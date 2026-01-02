# Integrating Anthropics Skills into Cursor IDE

**Date**: 2025-01-27  
**Reference**: Anthropics Skills Repository - https://github.com/anthropics/skills

## Overview

Anthropics Skills are structured instruction files designed for Claude. While Cursor doesn't natively support the Agent Skills format, there are several practical approaches to integrate skill patterns and knowledge into your Cursor workflow.

## Integration Approaches

### Approach 1: Extract Patterns into .cursorrules (Recommended)

**Best For**: Core workflows, patterns, and frequently-used knowledge

**How It Works**:
1. Extract relevant patterns from skills
2. Integrate into existing `.cursorrules` files
3. Reference from root or service-specific `.cursorrules`

**Example**: Extract webapp-testing patterns into BE/.cursorrules

```markdown
## Testing Patterns

### Playwright Testing Workflow
When testing Angular frontend or API endpoints:
1. Use reconnaissance-then-action pattern
2. Wait for `networkidle` before DOM inspection
3. Use descriptive selectors: `text=`, `role=`, CSS selectors
4. Always close browser when done

### Decision Tree: Choosing Testing Approach
- Static HTML → Read HTML directly for selectors
- Dynamic webapp → Use Playwright with server lifecycle management
```

**Pros**:
- ✅ Always in context
- ✅ Integrated with existing workflow
- ✅ No additional setup required

**Cons**:
- ❌ Increases .cursorrules file size
- ❌ Can bloat context window if too extensive
- ❌ Less modular than skills format

### Approach 2: Create Skills Directory Structure

**Best For**: Comprehensive skills with scripts, references, and assets

**How It Works**:
1. Create `.cursor/skills/` directory structure
2. Store skill-like content in markdown files
3. Reference from .cursorrules or use @filename syntax

**Directory Structure**:
```
AmesaBase-Monorepo/
├── .cursor/
│   └── skills/
│       ├── webapp-testing.md
│       ├── dotnet-microservice-patterns.md
│       ├── aws-ecs-deployment.md
│       ├── signalr-hub-patterns.md
│       └── scripts/
│           └── (helper scripts)
├── .cursorrules (references skills)
└── BE/
    └── .cursorrules (references skills)
```

**Example .cursorrules Reference**:
```markdown
## Testing Workflows

For comprehensive testing patterns, see `.cursor/skills/webapp-testing.md`

Key points:
- Use Playwright for E2E testing
- Follow reconnaissance-then-action pattern
- Wait for networkidle before DOM inspection
```

**Using @filename Syntax**:
When in Cursor chat, you can reference skills directly:
```
@.cursor/skills/webapp-testing.md How do I test the Angular frontend?
```

**Pros**:
- ✅ Modular and organized
- ✅ Can include scripts and assets
- ✅ Reference only when needed
- ✅ Follows skill structure patterns

**Cons**:
- ❌ Requires explicit references
- ❌ Not automatically loaded
- ❌ Need to remember to reference

### Approach 3: Reference Documentation (Hybrid)

**Best For**: Comprehensive guides that complement .cursorrules

**How It Works**:
1. Store skill content in `MetaData/Documentation/Development/`
2. Create skill-like markdown files
3. Reference from .cursorrules with clear navigation

**Directory Structure**:
```
MetaData/
└── Documentation/
    └── Development/
        ├── SKILLS/
        │   ├── webapp-testing-guide.md
        │   ├── dotnet-patterns.md
        │   └── deployment-workflows.md
        └── ANTHROPICS_SKILLS_REVIEW.md
```

**Example Integration in .cursorrules**:
```markdown
## External Guides

For detailed workflows, see:
- Testing: `MetaData/Documentation/Development/SKILLS/webapp-testing-guide.md`
- .NET Patterns: `MetaData/Documentation/Development/SKILLS/dotnet-patterns.md`
- Deployment: `MetaData/Documentation/Development/SKILLS/deployment-workflows.md`
```

**Pros**:
- ✅ Integrated with existing documentation structure
- ✅ Version controlled with codebase
- ✅ Can be referenced from multiple places
- ✅ Team-accessible

**Cons**:
- ❌ Requires navigation to files
- ❌ Not as immediate as .cursorrules

### Approach 4: Create Custom MCP Server (Advanced)

**Best For**: Dynamic skill loading, skill discovery, advanced workflows

**How It Works**:
1. Create MCP server that exposes skill content
2. Configure in Cursor MCP settings
3. Query skills via MCP tools

**Note**: This requires significant development effort and may not provide benefits over simpler approaches for most use cases.

## Recommended Implementation Strategy

### Phase 1: Extract High-Value Patterns (Immediate)

**Extract these patterns into BE/.cursorrules**:

1. **Testing Patterns** (from webapp-testing):
   - Playwright workflow patterns
   - Server lifecycle management
   - DOM inspection strategies

2. **Documentation Patterns** (from docx/pdf skills):
   - Document generation workflows
   - Template usage patterns

3. **MCP Patterns** (from mcp-builder):
   - If expanding MCP usage

### Phase 2: Create Skills Directory (Short-term)

**Create `.cursor/skills/` with these skills**:

1. **webapp-testing.md** - Complete Playwright testing guide
2. **dotnet-microservice-patterns.md** - .NET 8.0 patterns specific to AmesaBase
3. **aws-ecs-deployment.md** - ECS deployment workflows
4. **signalr-hub-patterns.md** - SignalR hub patterns and best practices

### Phase 3: Enhance Documentation (Ongoing)

**Store comprehensive guides in MetaData/Documentation/Development/SKILLS/**:

- Reference from .cursorrules
- Link from main documentation
- Keep updated with codebase changes

## Practical Example: Creating a Skill

### Step 1: Choose a Skill to Adapt

Example: Adapt `webapp-testing` for AmesaBase Angular/.NET stack

### Step 2: Create Skill File

**`.cursor/skills/webapp-testing.md`**:

```markdown
# Angular Frontend Testing with Playwright

## Overview

Guide for testing Angular frontend (FE/) and .NET 8.0 backend APIs using Playwright.

## Quick Start

### Testing Angular Frontend

1. **Start Backend**: `cd BE && dotnet run`
2. **Start Frontend**: `cd FE && npm start`
3. **Write Playwright Test**: Use patterns below

### Testing Backend APIs

Use Playwright for API endpoint validation and integration testing.

## Workflow Patterns

### Reconnaissance-Then-Action Pattern

1. Navigate to page: `page.goto('http://localhost:4200')`
2. Wait for network idle: `page.wait_for_load_state('networkidle')`
3. Take screenshot: `page.screenshot(path='screenshot.png')`
4. Inspect DOM: `page.content()` or `page.locator('selector')`
5. Execute actions using discovered selectors

### Server Lifecycle Management

For tests requiring both backend and frontend:

```python
# Use with_server.py pattern
python scripts/with_server.py \
  --server "cd BE && dotnet run" --port 5000 \
  --server "cd FE && npm start" --port 4200 \
  -- python test_automation.py
```

## Common Patterns

### Angular Component Testing

```python
# Wait for Angular to bootstrap
page.goto('http://localhost:4200')
page.wait_for_load_state('networkidle')

# Angular-specific selectors
page.locator('[data-testid="component-name"]')
page.locator('button', has_text='Submit')
```

### API Endpoint Testing

```python
# Test API endpoints
response = page.request.get('http://localhost:5000/api/v1/health')
assert response.status == 200
```

## Best Practices

- ✅ Always wait for `networkidle` on dynamic apps
- ✅ Use descriptive selectors: `data-testid`, `role=`, `text=`
- ✅ Close browser when done: `browser.close()`
- ✅ Use headless mode for CI: `headless=True`
- ❌ Don't inspect DOM before waiting for networkidle
- ❌ Don't use brittle selectors (class names, complex CSS)

## References

- Playwright Docs: https://playwright.dev/python/
- Angular Testing: See FE/.cursorrules
- Backend Testing: See BE/.cursorrules
```

### Step 3: Reference from .cursorrules

**BE/.cursorrules** (add section):

```markdown
## Testing Workflows

For comprehensive testing patterns, see `.cursor/skills/webapp-testing.md`

Quick reference:
- Use Playwright for E2E testing
- Follow reconnaissance-then-action pattern
- Wait for networkidle before DOM inspection
```

## File Reference Syntax in Cursor

Cursor supports referencing files directly in chat:

```
@.cursor/skills/webapp-testing.md How do I test the login flow?
```

This loads the skill file into context for that conversation.

## Skills to Create for AmesaBase

### High Priority

1. **webapp-testing** - Angular/.NET testing patterns
2. **dotnet-microservice-patterns** - .NET 8.0 microservice architecture patterns
3. **aws-ecs-deployment** - ECS Fargate deployment workflows

### Medium Priority

4. **signalr-hub-patterns** - SignalR hub development and patterns
5. **aurora-postgresql-schema** - Database schema management patterns
6. **api-design-patterns** - RESTful API design for microservices

### Future

7. **document-generation** - Generate deployment docs, API references
8. **monitoring-observability** - CloudWatch, logging patterns
9. **security-patterns** - Auth, authorization, security best practices

## Integration Checklist

- [ ] Review Anthropics skills repository for relevant patterns
- [ ] Extract high-value patterns into BE/.cursorrules
- [ ] Create `.cursor/skills/` directory structure
- [ ] Create first skill: `webapp-testing.md`
- [ ] Reference skills from .cursorrules
- [ ] Test skill references in Cursor chat
- [ ] Document skill usage in team docs
- [ ] Create additional skills as needed
- [ ] Update skills as codebase evolves

## Best Practices

1. **Start Small**: Extract most valuable patterns first
2. **Progressive Disclosure**: Keep .cursorrules concise, reference detailed skills
3. **Regular Updates**: Keep skills in sync with codebase changes
4. **Team Collaboration**: Store skills in version control
5. **Clear Navigation**: Make it obvious where to find skill content
6. **Examples**: Include practical examples in skills
7. **Scripts**: Bundle helper scripts with skills when appropriate

## Troubleshooting

### Skills Not Loading
- Verify file path in .cursorrules references
- Check file exists in expected location
- Use absolute paths if relative paths don't work

### Context Window Issues
- Keep .cursorrules concise
- Move detailed content to skill files
- Reference skills only when needed

### Outdated Skills
- Regularly review and update skills
- Version skills with codebase changes
- Document skill update process

## References

- **Anthropics Skills Repo**: https://github.com/anthropics/skills
- **Agent Skills Spec**: See `C:\Users\dror0\Curser-Repos\anthropics-skills\spec\`
- **Skill Creator Guide**: `C:\Users\dror0\Curser-Repos\anthropics-skills\skills\skill-creator\SKILL.md`
- **Review Document**: `MetaData/Documentation/Development/ANTHROPICS_SKILLS_REVIEW.md`

## Conclusion

While Cursor doesn't natively support the Agent Skills format, you can effectively integrate skill patterns using:

1. **.cursorrules** - For core, frequently-used patterns
2. **.cursor/skills/** - For modular, comprehensive skills
3. **MetaData/Documentation/** - For team-accessible guides

The hybrid approach of extracting key patterns into .cursorrules while maintaining detailed skills in separate files provides the best balance of immediate accessibility and modular organization.
