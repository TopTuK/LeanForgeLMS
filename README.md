# LeanForgeLMS

A Learning Management System (enrollment, progress tracking, grading, course lifecycle) built with .NET 10, Clean Architecture, and a Vue 3 SPA frontend.

## Tech Stack

- **.NET 10** / C# 14, **ASP.NET Core** (Minimal APIs for new endpoints; existing auth stays MVC controllers)
- **.NET Aspire** — local orchestration (Postgres in Docker, service discovery, OpenTelemetry, dashboard)
- **Entity Framework Core + PostgreSQL** (Npgsql)
- **gRPC** — `LF.WebApi` talks to `LF.IdentityService` over gRPC for user identity
- **Mapster** — DTO ↔ domain object mapping
- **JWT Bearer + Cookie + OpenID Connect** (Duende.IdentityModel) authentication
- **Serilog** — structured logging
- **Frontend**: Vue 3 + Vite, Pinia, vue-router, vue-i18n, Vuestic UI, Tailwind CSS v4

## Architecture

Clean Architecture with dependencies pointing inward:

```
LF.AppDomain          Domain layer — entities, enums. Zero project/framework references.
    ↑
LF.Application        Use cases, DTOs, service interfaces. References AppDomain only.
    ↑
LF.Infrastructure     EF Core persistence, gRPC clients, external services.
    ↑
LF.WebApi / LF.IdentityService   Hosts. Reference all three, depend on abstractions.
```

```
LeanForgeLMS.slnx
LeanForgeLMS.AppHost/            .NET Aspire orchestration (Postgres, WebApi, IdentityService, SPA dev server)
LeanForgeLMS.ServiceDefaults/    Aspire service defaults — OpenTelemetry, resilience, service discovery
LF.AppDomain/                    Domain layer — entities, enums
LF.Application/                  Application layer — use cases, DTOs, Mapster config, IAppDbContext
LF.Infrastructure/                Infrastructure layer — EF Core (AppDbContext), gRPC client
LF.WebApi/                       Host — MVC auth controllers + Minimal API endpoints, serves the SPA
LF.IdentityService/              gRPC service — owns the Postgres-backed user store
lf.webapp/                       Vue 3 + Vite SPA
```

See [CLAUDE.md](CLAUDE.md) for detailed conventions and target structure (this repo is set up for AI-assisted development with Claude Code).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) `^22.18.0 || >=24.12.0`
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — Aspire provisions Postgres as a container

## Getting Started

1. Clone the repo.
2. Configure secrets for `LF.WebApi` (see [Configuration & Secrets](#configuration--secrets) below).
3. Run everything via Aspire:

   ```bash
   dotnet run --project LeanForgeLMS.AppHost
   ```

   This starts Postgres (Docker), `LF.IdentityService`, `LF.WebApi`, and the Vite dev server for `lf.webapp`, and prints an Aspire dashboard URL where you can see all resources, logs, and traces.

## Configuration & Secrets

`LF.WebApi/appsettings.json` contains placeholder values for `PmiAuth:ClientSecret` — **never commit real secrets to this file.** For local development, use [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets):

```bash
cd LF.WebApi
dotnet user-secrets set "PmiAuth:ClientSecret" "<real-secret>"
```

In deployed environments, set secrets via environment variables (`PmiAuth__ClientSecret`) or a secret manager — never bake them into `appsettings.json`. The same applies to `DefaultAuth:JwtKey` before any production deployment.

The Postgres connection string (`ConnectionStrings:leanforge`) is injected automatically when running via the AppHost. `LF.IdentityService/appsettings.Development.json` has a local-only fallback (`leanforge`/`leanforge`) for running `LF.IdentityService` standalone without Aspire.

## Other Commands

```bash
# Build entire solution
dotnet build LeanForgeLMS.slnx

# Run the API directly (without Aspire — gRPC calls to IdentityService won't resolve)
dotnet run --project LF.WebApi

# Run the frontend dev server standalone
cd lf.webapp && npm run dev

# Add an EF Core migration
dotnet ef migrations add <Name> \
  --project LF.Infrastructure \
  --startup-project LF.IdentityService \
  --context AppDbContext

# Format check
dotnet format --verify-no-changes
```

## Testing

No test projects exist yet. The intended stack is xUnit v3 + Testcontainers (real PostgreSQL, not in-memory) + WebApplicationFactory for integration tests.
