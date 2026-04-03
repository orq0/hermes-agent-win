from __future__ import annotations

import argparse
import sys
import json
import os
import subprocess
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


def _hermes_home() -> Path:
    configured = os.environ.get("HERMES_HOME")
    if configured:
        return Path(configured)

    local_app_data = os.environ.get("LOCALAPPDATA") or str(Path.home() / "AppData" / "Local")
    return Path(local_app_data) / "hermes"


def _read_model_setting(key: str) -> str | None:
    config_path = _hermes_home() / "config.yaml"
    if not config_path.exists():
        return None

    in_model_section = False
    for raw_line in config_path.read_text(encoding="utf-8").splitlines():
        line = raw_line.rstrip()
        stripped = line.strip()

        if not stripped or stripped.startswith("#"):
            continue

        if not raw_line[:1].isspace() and line.endswith(":"):
            in_model_section = line.lower() == "model:"
            continue

        if not in_model_section:
            continue

        prefix = f"{key}:"
        if stripped.lower().startswith(prefix.lower()):
            return stripped[len(prefix):].strip().strip("\"'")

    return None


def _parse_hermes_output(stdout: str) -> tuple[str, str]:
    lines = stdout.splitlines()
    session_id = ""

    for index in range(len(lines) - 1, -1, -1):
        line = lines[index].strip()
        if line.startswith("session_id:"):
            session_id = line.split(":", 1)[1].strip()
            response = "\n".join(lines[:index]).strip()
            return response, session_id

    return stdout.strip(), session_id


def _run_hermes_chat(message: str, session_id: str | None, cwd: str | None) -> dict[str, str]:
    hermes_cmd = _hermes_home() / "bin" / "hermes.cmd"
    if not hermes_cmd.exists():
        raise FileNotFoundError(f"Hermes command not found: {hermes_cmd}")

    command = [
        "cmd.exe",
        "/c",
        str(hermes_cmd),
        "chat",
        "-Q",
        "-q",
        message,
        "--source",
        "tool",
    ]

    if session_id:
        command.extend(["--resume", session_id])

    env = os.environ.copy()`n    env["CREATE_NEW_CONSOLE"] = "1"  # Force Windows console creation
    env["PYTHONUTF8"] = "1"
    env["HERMES_HOME"] = str(_hermes_home())

    completed = subprocess.run(`n        creationflags=subprocess.CREATE_NEW_CONSOLE if sys.platform == "win32" else 0,  # Give Hermes its own console`n        
        command,
        cwd=cwd or os.getcwd(),
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        timeout=900,
        env=env,
        shell=False,
    )

    stdout = completed.stdout or ""
    stderr = completed.stderr or ""

    if completed.returncode != 0:
        detail = stderr.strip() or stdout.strip() or "Hermes returned a non-zero exit code."
        raise RuntimeError(detail)

    response, next_session_id = _parse_hermes_output(stdout)
    if not response:
        raise RuntimeError("Hermes did not return a response.")

    return {
        "response": response,
        "session_id": next_session_id or (session_id or ""),
    }


class HermesSidecarHandler(BaseHTTPRequestHandler):
    server_version = "HermesSidecar/0.1"

    def do_GET(self) -> None:
        if self.path != "/health":
            self._write_json(HTTPStatus.NOT_FOUND, {"error": "Not found"})
            return

        self._write_json(
            HTTPStatus.OK,
            {
                "status": "ok",
                "provider": _read_model_setting("provider") or "custom",
                "model": _read_model_setting("default") or "minimax-m2.7:cloud",
                "base_url": _read_model_setting("base_url") or "http://127.0.0.1:11434/v1",
            },
        )

    def do_POST(self) -> None:
        if self.path != "/chat":
            self._write_json(HTTPStatus.NOT_FOUND, {"error": "Not found"})
            return

        try:
            length = int(self.headers.get("Content-Length", "0"))
            payload = json.loads(self.rfile.read(length).decode("utf-8"))
            message = str(payload.get("message", "")).strip()
            session_id = str(payload.get("session_id", "")).strip() or None
            cwd = str(payload.get("cwd", "")).strip() or None

            if not message:
                self._write_json(HTTPStatus.BAD_REQUEST, {"error": "Message is required."})
                return

            result = _run_hermes_chat(message, session_id, cwd)
            self._write_json(HTTPStatus.OK, result)
        except FileNotFoundError as exc:
            self._write_json(HTTPStatus.INTERNAL_SERVER_ERROR, {"error": str(exc)})
        except subprocess.TimeoutExpired:
            self._write_json(HTTPStatus.GATEWAY_TIMEOUT, {"error": "Hermes timed out while processing the request."})
        except Exception as exc:  # noqa: BLE001
            self._write_json(HTTPStatus.INTERNAL_SERVER_ERROR, {"error": str(exc)})

    def log_message(self, format: str, *args: object) -> None:  # noqa: A003
        return

    def _write_json(self, status: HTTPStatus, payload: dict[str, object]) -> None:
        body = json.dumps(payload).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, default=8765)
    args = parser.parse_args()

    server = ThreadingHTTPServer(("127.0.0.1", args.port), HermesSidecarHandler)
    server.daemon_threads = True
    server.serve_forever()


if __name__ == "__main__":
    main()
