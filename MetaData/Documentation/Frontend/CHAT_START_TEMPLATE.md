# Starting a New Chat Session

## Quick Context Setup
When starting a new chat, share these files in order:

1. **PROJECT_OVERVIEW.md** - High-level project information
2. **CURRENT_WORK.md** - What you're currently working on
3. **TECH_STACK.md** - Technology details
4. **REPO_STRUCTURE.md** - Repository architecture
5. **SECRETS_MANAGEMENT.md** - GitHub secrets and environment configs
6. **TROUBLESHOOTING.md** - Common issues and solutions

## Initial Message Template
```
Hi! I'm working on the AmesaBase project. Please read these context files to understand the project:

1. PROJECT_OVERVIEW.md
2. CURRENT_WORK.md  
3. TECH_STACK.md
4. REPO_STRUCTURE.md
5. SECRETS_MANAGEMENT.md
6. ENVIRONMENT_CONFIG.md
7. ACTUAL_IMPLEMENTATION_DETAILS.md
8. AWS_INFRASTRUCTURE_DETAILS.md
9. TROUBLESHOOTING.md

I'm currently working on: [brief description of current task]

The project is a lottery management system with multi-repo setup:
- AmesaFE: Angular 20.2.1 frontend (property lottery platform)
- AmesaBE: .NET 8.0 backend (Docker + ECS)
- AmesaDevOps: Infrastructure as Code
- Database: Aurora PostgreSQL (separate clusters for prod/test/dev)
- Secrets: Stored in GitHub repository secrets
- Configuration: Single codebase with external environment config
- CLI Tools: AWS CLI, Angular CLI, GitHub CLI
- Business Model: 4Wins Model (property lotteries with community support)

Current repo: AmesaFE
Current branch: Testing
Last commit: [include if relevant]

What I need help with: [describe your specific request]
```

## Context Maintenance
- Update `CURRENT_WORK.md` regularly with your progress
- Add new issues and solutions to `TROUBLESHOOTING.md`
- Keep `PROJECT_OVERVIEW.md` updated with major changes
- Document important decisions and configurations

## Quick Status Check
Before starting work, the AI can:
- Read the current work status
- Check recent git commits
- Understand the project structure
- Know the current technology stack

## Pro Tips
- Always mention which branch you're on
- Include any recent error messages
- Specify what environment you're working in (dev/stage/prod)
- Mention any recent changes or deployments
