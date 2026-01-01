# AGENTS.md

This file describes how coding agents should work in this repository (structure, workflows, and safety rules).

## Repository layout (high level)

- **Microservices (root)**: `AmesaBackend.Auth/`, `AmesaBackend.Lottery/`, `AmesaBackend.Payment/`, `AmesaBackend.Notification/`, `AmesaBackend.Content/`, `AmesaBackend.LotteryResults/`, `AmesaBackend.Analytics/`, `AmesaBackend.Admin/`
- **Shared library**: `AmesaBackend.Shared/`
- **Tests**: `AmesaBackend.Tests/`
- **Docs and scripts**: `MetaData/`, `Infrastructure/`, `scripts/`
- **CI workflows**: `.github/workflows/`

> Note: There is also a `BE/` directory in this monorepo. The active CI workflows reference the **root** `AmesaBackend.*` projects and Dockerfiles; treat `BE/` as legacy/auxiliary unless the task explicitly targets it.

## First steps for agents

Before making changes, skim these files for current conventions and “source of truth”:

- `.cursorrules`
- `README.md`
- `MetaData/Documentation/README.md`
- `.github/workflows/README.md`

## Build & test (local/CI-aligned)

CI runs restore/build from repo root and executes tests from `AmesaBackend.Tests/`.

Recommended commands:

```bash
dotnet restore AmesaBackend.Tests/AmesaBackend.Tests.csproj
dotnet build AmesaBackend.Tests/AmesaBackend.Tests.csproj --no-restore
dotnet test AmesaBackend.Tests/AmesaBackend.Tests.csproj --no-build
```

When changing a specific service, also build that service project:

```bash
dotnet build AmesaBackend.<Service>/AmesaBackend.<Service>.csproj
```

## Docker / deployment expectations

Docker images are built using the service Dockerfiles in each `AmesaBackend.<Service>/Dockerfile`, with the **repo root** as the build context:

```bash
docker build -f AmesaBackend.Auth/Dockerfile .
```

## Safety rules (must follow)

- **Do not commit or push** (leave git operations to the human / automation unless explicitly requested).
- **Never add secrets** (keys, passwords, tokens, connection strings) to source control.
  - Avoid changing/tracking `.env*`, `secrets*.json`, `appsettings.*.json` (except the repo’s tracked `appsettings.json` / `appsettings.Development.json` files).
- **Respect `.gitignore`** (don’t stage ignored files).
- **Avoid long-running processes** (`dotnet watch`, `npm run dev`, `tail -f`, etc.) unless explicitly required.
- **Prefer minimal, targeted changes**; keep controllers thin and push logic into services; follow existing naming and folder conventions.

## When you change APIs / schema / infra

If a task touches any of the following, update the relevant documentation and/or context references in `MetaData/Documentation/` and keep Swagger/API docs consistent:

- API routes/DTOs/contracts
- EF Core models/migrations
- Auth/security behavior
- Deployment/CI scripts or infrastructure configuration

