# Cursor Skills Directory

This directory contains skill-like documentation files adapted from the Anthropics Skills repository for use in Cursor IDE.

## Available Skills

### Development & Architecture
- **dotnet-microservice-patterns.md** - .NET 8.0 microservice architecture patterns, service structure, configuration, dependency injection
- **api-design-patterns.md** - RESTful API design, endpoint patterns, error handling, versioning, authentication

### Infrastructure & Database
- **aws-ecs-deployment.md** - AWS ECS Fargate deployment workflows, Docker builds, ECR management, ALB routing
- **aurora-postgresql-schema.md** - Aurora PostgreSQL schema management, EF Core migrations, connection patterns

### Real-time Communication
- **signalr-hub-patterns.md** - SignalR hub development patterns, group management, broadcasting, client integration

### Testing
- **webapp-testing.md** - Angular frontend and .NET backend testing with Playwright

## Usage

### In Cursor Chat

Reference skills directly using @filename syntax:

```
@.cursor/skills/webapp-testing.md How do I test the login flow?
```

### In .cursorrules

Reference skills in your .cursorrules files:

```markdown
## Testing Workflows

For comprehensive testing patterns, see `.cursor/skills/webapp-testing.md`
```

## Skill Format

Skills follow this structure (adapted from Anthropics Skills format):

1. **Overview** - What the skill covers
2. **Quick Start** - Fast getting started guide
3. **Workflow Patterns** - Common patterns and workflows
4. **Examples** - Practical code examples
5. **Best Practices** - DOs and DON'Ts
6. **References** - Links to related documentation

## Source

Skills are adapted from: https://github.com/anthropics/skills

See `MetaData/Documentation/Development/ANTHROPICS_SKILLS_REVIEW.md` for the full review.

## Integration Guide

See `MetaData/Documentation/Development/CURSOR_SKILLS_INTEGRATION_GUIDE.md` for complete integration instructions.
