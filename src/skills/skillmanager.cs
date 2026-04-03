namespace Hermes.Agent.Skills;

using Hermes.Agent.LLM;
using Hermes.Agent.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

/// <summary>
/// Skills System - Markdown-based custom capabilities.
/// Skills are .md files with YAML frontmatter that define agent behaviors.
/// </summary>

public sealed class SkillManager
{
    private readonly string _skillsDir;
    private readonly ILogger<SkillManager> _logger;
    private readonly ConcurrentDictionary<string, Skill> _skills = new();
    
    public SkillManager(string skillsDir, ILogger<SkillManager> logger)
    {
        _skillsDir = skillsDir;
        _logger = logger;
        
        Directory.CreateDirectory(skillsDir);
        LoadSkills();
    }
    
    /// <summary>
    /// Load all skills from disk.
    /// </summary>
    private void LoadSkills()
    {
        if (!Directory.Exists(_skillsDir))
            return;
        
        var skillFiles = Directory.EnumerateFiles(_skillsDir, "*.md", SearchOption.AllDirectories);
        
        foreach (var file in skillFiles)
        {
            try
            {
                var skill = ParseSkillFile(file);
                if (skill != null)
                {
                    _skills[skill.Name] = skill;
                    _logger.LogInformation("Loaded skill: {Name} from {Path}", skill.Name, file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load skill from {File}", file);
            }
        }
    }
    
    /// <summary>
    /// Parse skill from markdown file with YAML frontmatter.
    /// </summary>
    private Skill? ParseSkillFile(string path)
    {
        var content = File.ReadAllText(path);
        
        // Parse YAML frontmatter
        if (!content.StartsWith("---"))
            return null;
        
        var endIndex = content.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIndex == -1)
            return null;
        
        var yamlContent = content.Substring(3, endIndex - 3).Trim();
        var body = content.Substring(endIndex + 3).Trim();
        
        var frontmatter = ParseYamlFrontmatter(yamlContent);
        
        return new Skill
        {
            Name = frontmatter.Name,
            Description = frontmatter.Description,
            FilePath = path,
            Tools = frontmatter.Tools?.Split(',').Select(t => t.Trim()).ToList() ?? new List<string>(),
            Model = frontmatter.Model,
            SystemPrompt = body
        };
    }
    
    private SkillFrontmatter ParseYamlFrontmatter(string yaml)
    {
        var frontmatter = new SkillFrontmatter();
        
        foreach (var line in yaml.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                continue;
            
            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex == -1)
                continue;
            
            var key = trimmed.Substring(0, colonIndex).Trim();
            var value = trimmed.Substring(colonIndex + 1).Trim().Trim('"', '\'');
            
            switch (key.ToLower())
            {
                case "name":
                    frontmatter.Name = value;
                    break;
                case "description":
                    frontmatter.Description = value;
                    break;
                case "tools":
                    frontmatter.Tools = value;
                    break;
                case "model":
                    frontmatter.Model = value;
                    break;
            }
        }
        
        return frontmatter;
    }
    
    /// <summary>
    /// Get skill by name.
    /// </summary>
    public Skill? GetSkill(string name)
    {
        return _skills.TryGetValue(name, out var skill) ? skill : null;
    }
    
    /// <summary>
    /// List all skills.
    /// </summary>
    public List<Skill> ListSkills()
    {
        return _skills.Values.ToList();
    }
    
    /// <summary>
    /// Invoke a skill.
    /// Returns the system prompt to inject.
    /// </summary>
    public async Task<string> InvokeSkillAsync(string skillName, string userQuery, CancellationToken ct)
    {
        if (!_skills.TryGetValue(skillName, out var skill))
        {
            throw new SkillNotFoundException(skillName);
        }
        
        _logger.LogInformation("Invoking skill: {Name}", skillName);
        
        // Build skill context
        var context = $@"
## Active Skill: {skill.Name}
{skill.Description}

### Tools Available
{string.Join(", ", skill.Tools)}

### Instructions
{skill.SystemPrompt}

### User Query
{userQuery}
";
        
        return context;
    }
    
    /// <summary>
    /// Create a new skill.
    /// </summary>
    public async Task<Skill> CreateSkillAsync(
        string name,
        string description,
        string systemPrompt,
        List<string> tools,
        string? model,
        CancellationToken ct)
    {
        var filename = $"{name.ToLower().Replace(" ", "-")}.md";
        var path = Path.Combine(_skillsDir, filename);
        
        var frontmatter = $@"---
name: {name}
description: {description}
tools: {string.Join(", ", tools)}
{(model != null ? $"model: {model}" : "")}
---
";
        
        var content = frontmatter + systemPrompt;
        
        await File.WriteAllTextAsync(path, content, ct);
        
        var skill = new Skill
        {
            Name = name,
            Description = description,
            FilePath = path,
            Tools = tools,
            Model = model,
            SystemPrompt = systemPrompt
        };
        
        _skills[name] = skill;
        
        _logger.LogInformation("Created skill: {Name}", name);
        
        return skill;
    }
    
    /// <summary>
    /// Delete a skill.
    /// </summary>
    public async Task DeleteSkillAsync(string name, CancellationToken ct)
    {
        if (!_skills.TryGetValue(name, out var skill))
        {
            throw new SkillNotFoundException(name);
        }
        
        if (File.Exists(skill.FilePath))
        {
            File.Delete(skill.FilePath);
        }
        
        _skills.TryRemove(name, out _);
        
        _logger.LogInformation("Deleted skill: {Name}", name);
    }
}

// =============================================
// Skill Types
// =============================================

public sealed class Skill
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string FilePath { get; init; }
    public required List<string> Tools { get; init; }
    public string? Model { get; init; }
    public required string SystemPrompt { get; init; }
}

public sealed class SkillFrontmatter
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Tools { get; set; } = "";
    public string Model { get; set; } = "";
}

// =============================================
// Exceptions
// =============================================

public sealed class SkillNotFoundException : Exception
{
    public SkillNotFoundException(string skillName) 
        : base($"Skill '{skillName}' not found")
    {
    }
}

// =============================================
// Skill Invoker
// =============================================

public sealed class SkillInvoker
{
    private readonly SkillManager _skillManager;
    private readonly IChatClient _chatClient;
    private readonly ILogger<SkillInvoker> _logger;
    
    public SkillInvoker(
        SkillManager skillManager,
        IChatClient chatClient,
        ILogger<SkillInvoker> logger)
    {
        _skillManager = skillManager;
        _chatClient = chatClient;
        _logger = logger;
    }
    
    /// <summary>
    /// Invoke skill and get response.
    /// </summary>
    public async Task<string> InvokeAsync(
        string skillName,
        string userQuery,
        CancellationToken ct)
    {
        var skillContext = await _skillManager.InvokeSkillAsync(skillName, userQuery, ct);
        
        var messages = new List<Message>
        {
            new Message { Role = "system", Content = skillContext }
        };
        
        var response = await _chatClient.CompleteAsync(messages, ct);
        
        _logger.LogInformation("Skill {Name} completed", skillName);
        
        return response;
    }
}

// =============================================
// Built-in Skills
// =============================================

public static class BuiltInSkills
{
    public static readonly Skill ApiExpert = new()
    {
        Name = "api-expert",
        Description = "Expert in REST API patterns and best practices",
        FilePath = "",
        Tools = new List<string> { "read_file", "write_file", "edit_file", "bash" },
        Model = null,
        SystemPrompt = @"
You are an expert in REST API design and implementation.

Always:
1. Check existing endpoints before creating new ones
2. Follow RESTful conventions (proper HTTP methods, status codes)
3. Use consistent error handling patterns
4. Document all endpoints with OpenAPI/Swagger
5. Implement proper authentication and authorization
6. Add rate limiting and input validation
7. Log all API requests for debugging

When creating new endpoints:
- Use proper HTTP methods (GET, POST, PUT, DELETE, PATCH)
- Return appropriate status codes (200, 201, 400, 404, 500)
- Include pagination for list endpoints
- Version your API (/api/v1/...)
- Validate all input parameters
"
    };
    
    public static readonly Skill TestWriter = new()
    {
        Name = "test-writer",
        Description = "Expert in writing comprehensive tests",
        FilePath = "",
        Tools = new List<string> { "read_file", "write_file", "bash" },
        Model = null,
        SystemPrompt = @"
You are an expert in writing comprehensive tests.

Always:
1. Read the source code before writing tests
2. Cover edge cases and error conditions
3. Use descriptive test names (Given_When_Then format)
4. Mock external dependencies
5. Test both happy path and failure scenarios
6. Aim for high code coverage but prioritize meaningful tests
7. Use Arrange-Act-Assert pattern

When writing tests:
- Name tests clearly: `MethodName_Scenario_ExpectedResult`
- Test one thing per test
- Keep tests independent and idempotent
- Use fixtures for common setup
- Assert specific values, not just 'no exception'
"
    };
    
    public static readonly Skill SecurityReviewer = new()
    {
        Name = "security-reviewer",
        Description = "Security expert who reviews code for vulnerabilities",
        FilePath = "",
        Tools = new List<string> { "read_file", "grep", "glob" },
        Model = null,
        SystemPrompt = @"
You are a security expert reviewing code for vulnerabilities.

Always check for:
1. SQL injection (parameterized queries only)
2. XSS (sanitize all user input)
3. CSRF (tokens on state-changing operations)
4. Authentication/authorization flaws
5. Sensitive data exposure (logs, errors)
6. Insecure dependencies (outdated packages)
7. Hardcoded secrets
8. Path traversal vulnerabilities
9. Command injection
10. Race conditions

When reviewing:
- Flag any hardcoded credentials immediately
- Check all user input is validated/sanitized
- Verify authentication on all protected routes
- Ensure sensitive data is encrypted at rest and in transit
- Review error messages don't leak implementation details
"
    };
}
