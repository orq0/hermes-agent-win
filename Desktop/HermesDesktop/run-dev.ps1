param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64",
    [switch]$ShowLocalDetails
)

$dotnet = "C:\Program Files\dotnet\dotnet.exe"
$projectFile = Join-Path $PSScriptRoot "HermesDesktop.csproj"
[xml]$projectXml = Get-Content -Path $projectFile
$targetFramework = $projectXml.Project.PropertyGroup.TargetFramework | Select-Object -First 1

$rid = switch ($Platform) {
    "x86" { "win-x86" }
    "ARM64" { "win-arm64" }
    default { "win-x64" }
}
$outputDir = Join-Path $PSScriptRoot "bin\$Platform\$Configuration\$targetFramework\$rid"
$showLocalDetailsFlagPath = Join-Path $outputDir "show-local-details.flag"

Get-Process HermesDesktop -ErrorAction SilentlyContinue | Stop-Process -Force

$buildSucceeded = $false
for ($attempt = 1; $attempt -le 3; $attempt++) {
    & $dotnet build -c $Configuration -p:Platform=$Platform
    if ($LASTEXITCODE -eq 0) {
        $buildSucceeded = $true
        break
    }

    Start-Sleep -Seconds 2
}

if (-not $buildSucceeded) {
    exit $LASTEXITCODE
}

if ($ShowLocalDetails) {
    Set-Content -Path $showLocalDetailsFlagPath -Value "show-local-details" -Encoding ascii
} elseif (Test-Path $showLocalDetailsFlagPath) {
    Remove-Item -LiteralPath $showLocalDetailsFlagPath -Force
}

$manifestPath = Join-Path $outputDir "AppxManifest.xml"
Add-AppxPackage -Register $manifestPath -ForceApplicationShutdown

$package = Get-AppxPackage | Where-Object { $_.Name -eq "EDC29F63-281C-4D34-8723-155C8122DEA2" } | Select-Object -First 1
if (-not $package) {
    throw "Hermes Desktop package is not registered."
}

Start-Process explorer.exe "shell:AppsFolder\$($package.PackageFamilyName)!App"
