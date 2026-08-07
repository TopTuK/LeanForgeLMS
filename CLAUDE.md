# LeanForgeLMS — Clean Architecture

## Project Context

LeanForgeLMS is a Learning Management System built as a .NET 10 application. The domain has moderate business rules (enrollment, progress tracking, grading, course lifecycle) — not CRUD-only, but not a rich DDD domain either. It's a solo-developer project.

The application runs as two Aspire-managed services fronted by a Vue 3 SPA: `LF.WebApi` (MVC auth controllers + the growing Minimal API surface) and `LF.IdentityService`, a gRPC service that owns user identity data and is called from `LF.WebApi` as a gRPC client (shared `user_service.proto`) — see [IdentityService status](#identityservice-status) below.

**Migration note:** this project originally started as a Modular Monolith (`LF.Modules.[Module]` class libraries). That direction has been abandoned in favor of layered Clean Architecture — `LF.AppDomain` (Domain), `LF.Application` (Application), `LF.Infrastructure` (Infrastructure), `LF.WebApi` (Api). If you find references to `LF.Modules.*` anywhere, they're stale.

## Current Solution Structure

```
LeanForgeLMS.slnx
LeanForgeLMS.AppHost/            # .NET Aspire orchestration — postgres wired to lf-identityservice; minio (avatar storage) and the lf-webapp Vite dev server wired to lf-webapi; lf-webapi references lf-identityservice (gRPC client) and waits for it
LeanForgeLMS.ServiceDefaults/    # Aspire service defaults — OpenTelemetry, resilience, service discovery
LF.AppDomain/                    # Domain layer — has real content: Entities/User/DbUser.cs, Models/User/Enums/UserRole.cs; zero project references (by design)
LF.Application/                  # Application layer — Services/Authentication (AuthenticationService, TokenService), Services/User/UserService, Services/Profile/ProfileService, ModelDto/*, Common/Interfaces/IAppDbContext.cs + IFileStorageService.cs
LF.Infrastructure/                # Infrastructure layer — Persistence/AppDbContext.cs (Npgsql), DI registered via AddInfrastructureDatabase() — but only wired up in LF.IdentityService/Program.cs, not LF.WebApi; Services/Storage/MinioFileStorageService.cs (avatar uploads), DI via AddInfrastructureFileStorage(), wired up only in LF.WebApi/Program.cs
LF.WebApi/                       # Host project (Microsoft.NET.Sdk.Web) — MVC controllers + JWT/Cookie/OIDC auth + growing Minimal API surface in Endpoints/ (e.g. ProfileEndpoints); references ServiceDefaults, LF.Application, AND LF.Infrastructure directly; also talks directly to MinIO (via CommunityToolkit.Aspire.Minio.Client) for avatar upload/download — a second external dependency alongside the gRPC call to LF.IdentityService
LF.IdentityService/              # Separate Aspire project — real gRPC identity/user service (RpcUserService), owns AppDbContext registration; leftover unused Protos/greet.proto (see below)
LF.ApplicationTests/             # xUnit v3 unit tests for LF.Application services (Moq + MockQueryable.Moq for IAppDbContext/DbSet mocking) — see Tech Stack below for what this does and doesn't cover yet
lf.webapp/                       # Vue 3 + Vite SPA (esproj), proxied in dev via UseSpa
```

### Target Layer Structure (new work)

Dependencies point inward. `LF.AppDomain` has zero project references; `LF.Application` references only `LF.AppDomain`; `LF.Infrastructure` references `LF.Application` + `LF.AppDomain`; `LF.WebApi` references all three but depends on abstractions, not concretions.

```
LF.AppDomain/
  Entities/
    Course.cs                    # Entity with behavior, not a data bag
  Enums/
  Exceptions/
  Common/
    Entity.cs                    # Base entity with Id
    Result.cs                    # Result pattern type

LF.Application/
  Common/
    Interfaces/
      IAppDbContext.cs           # DbContext abstraction — prefer this over repositories
    Behaviors/
      ValidationBehavior.cs      # Mediator pipeline behavior
  Courses/
    Commands/
      CreateCourse/
        CreateCourseCommand.cs
        CreateCourseHandler.cs
        CreateCourseValidator.cs
    Queries/
      GetCourse/
        GetCourseQuery.cs
        GetCourseHandler.cs
        CourseDto.cs

LF.Infrastructure/
  Persistence/
    AppDbContext.cs               # Implements IAppDbContext (move out of Infrastructure/db/)
    Configurations/
    Migrations/
  DependencyInjection.cs          # AddInfrastructure extension

LF.WebApi/
  Endpoints/
    CourseEndpoints.cs            # Thin, maps HTTP <-> use cases via ISender
  Controllers/                    # Existing MVC auth controllers stay here — do not add new ones
  Program.cs
```

### IdentityService Status

`LF.IdentityService` has moved past the scaffold stage: it's a real gRPC service (`RpcUserService : UserServiceRpc.UserServiceRpcBase`, `GetOrCreateUser`) that owns the Postgres-backed `AppDbContext` and user identity data. `LF.WebApi` talks to it as a gRPC client over the shared `user_service.proto` contract. The original default-template `Protos/greet.proto` is still present but unused — cleanup candidate, not a pattern to extend.

- Treat it as a real service boundary now — module/contract conventions can be built around it.
- The gRPC contract (`user_service.proto`) is a cross-service boundary; changes to it affect both `LF.WebApi` and `LF.IdentityService` — check both call sites before editing messages/RPCs.
- `AppDbContext` DI registration lives only in `LF.IdentityService/Program.cs` (via `AddInfrastructureDatabase()`); `LF.WebApi` does not register it despite referencing `LF.Infrastructure` — don't assume `AppDbContext` is resolvable in `LF.WebApi` without checking first.

## API Style

New endpoints use Minimal APIs — `IEndpointGroup` per feature in `LF.WebApi/Endpoints/`, auto-discovered, using `TypedResults` for OpenAPI. Endpoints stay thin and delegate to Application-layer use cases via `ISender`.

The existing auth setup in `LF.WebApi/Program.cs` (`AddControllersWithViews`, `MapControllerRoute`) stays as MVC controllers — it is not being retrofitted. Don't add new controllers; add new endpoints as `IEndpointGroup`s instead.

```csharp
public sealed class CourseEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses").WithTags("Courses");

        group.MapPost("/", async (CreateCourseCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? TypedResults.Created($"/api/courses/{result.Value}", result.Value)
                : result.ToProblemDetails();
        });
    }
}
```

## Tech Stack

- **.NET 10** / C# 14
- **ASP.NET Core** — Minimal APIs for new endpoints (`IEndpointGroup` + auto-discovery); existing auth stays MVC controllers
- **.NET Aspire** — AppHost + ServiceDefaults already wired up; OpenTelemetry, resilience, service discovery come from ServiceDefaults
- **Authentication** — JWT Bearer (primary scheme) + Cookie + OpenID Connect via Duende.IdentityModel, already implemented in `LF.WebApi/Program.cs`. Do not change the auth scheme wiring without discussing it first — it has specific cookie/OIDC/JWT interplay (temp cookie sign-in scheme, authorization code redemption).
- **Entity Framework Core + PostgreSQL (Npgsql)** — `Npgsql.EntityFrameworkCore.PostgreSQL` referenced in `LF.Infrastructure`; `AppDbContext` (in `Persistence/`) is registered via `AddInfrastructureDatabase()`, but only in `LF.IdentityService/Program.cs` — `LF.WebApi` does not call it. Aspire AppHost provisions a Postgres resource (`postgres`, database `leanforge`) wired only to `lf-identityservice`.
- **Mediator** (source-generated, MIT) — command/query dispatch for Application-layer use cases; not yet added as a package reference.
- **FluentValidation** — request validation (referenced in `LF.WebApi` only; not yet in `LF.Application`)
- **Serilog** — structured logging (`Serilog.AspNetCore` already referenced in `LF.WebApi`, bootstrap logger configured in `Program.cs`)
- **MinIO** — object storage for user avatar uploads. AppHost provisions a `minio` container (`CommunityToolkit.Aspire.Hosting.Minio`) referenced by `lf-webapi`; `LF.WebApi/Program.cs` calls `builder.AddMinioClient("minio")` (`CommunityToolkit.Aspire.Minio.Client`) directly — `IMinioClient` is only resolvable in `LF.WebApi`, not `LF.IdentityService`. `LF.Infrastructure`'s `MinioFileStorageService` (behind `IFileStorageService`) does the actual upload/download/delete; the avatar *key* (not the file) is persisted in Postgres via the existing gRPC round-trip to `LF.IdentityService`. Avatar bytes are always proxied through `LF.WebApi` — MinIO itself is never exposed to the browser (internal-only network in `docker-compose.yml`, matching `postgres`).
- **xUnit v3** — `LF.ApplicationTests` unit-tests `LF.Application` services (`UserService`, `ProfileService`, `AuthenticationService`, `TokenService`, mapping configs, DI registration) using Moq + `MockQueryable.Moq` to mock `IAppDbContext`/`DbSet<T>`. Testcontainers + WebApplicationFactory-based integration testing (real PostgreSQL, real HTTP pipeline) is still aspirational — no integration test project exists yet.
- **Frontend: Vue 3 + Vite** (`lf.webapp`) — Pinia for state, vue-router, vue-i18n, Vuestic UI, Tailwind CSS v4, axios for HTTP. Proxied to `http://localhost:5173` in dev via `UseSpa`/`UseProxyToSpaDevelopmentServer`; served from `wwwroot`/`MapFallbackToFile("index.html")` in production.

**Not yet decided / explicitly deferred:** caching (no HybridCache/Redis yet) and inter-service messaging (no Wolverine/MassTransit yet — `LF.IdentityService` currently would only be reachable via gRPC direct call, not a bus).

## Coding Standards

- **C# 14 features** — primary constructors, collection expressions, `field` keyword, records, pattern matching
- **File-scoped namespaces** — always
- **`var` for obvious types** — explicit types when the type isn't clear from context
- **Naming** — PascalCase for public members, `_camelCase` for private fields, suffix async methods with `Async`
- **No regions** — ever
- **No comments for obvious code** — only comment "why", never "what"
- **Internal by default** — handlers should be `internal sealed`; expose only endpoint groups, DTOs/contracts, and the `AddInfrastructure`/`AddApplication` registration extensions as `public`

## Skills

Load these dotnet-claude-kit skills for context:

- `modern-csharp` — C# 14 language features and idioms
- `clean-architecture` — Layered project structure, dependency inversion, use case handlers
- `minimal-api` — Endpoint groups, TypedResults, OpenAPI metadata
- `ef-core` — DbContext patterns, query optimization, migrations (PostgreSQL/Npgsql)
- `authentication` — JWT/OIDC/Cookie patterns (reference before touching `LF.WebApi/Program.cs` auth config)
- `dependency-injection` — Service registration patterns
- `error-handling` — Result pattern, ProblemDetails
- `testing` — xUnit v3, WebApplicationFactory, Testcontainers
- `aspire` — AppHost/ServiceDefaults conventions
- `configuration` — Options pattern, connection strings, per-project configuration sections
- `logging` — Serilog, structured logging, OpenTelemetry
- `messaging` — Wolverine/MassTransit (when/if inter-service messaging is introduced)
- `caching` — HybridCache/Redis (when caching is introduced)
- `arch-check` — Verify Domain -> Application -> Infrastructure -> Api dependency direction stays clean
- `workflow-mastery` — Parallel worktrees, verification loops, subagent patterns, context discipline
- `instinct-system` — Capture corrections, instincts, and discoveries as persistent learning

## MCP Tools

> **Setup:** Install once globally with `dotnet tool install -g CWM.RoslynNavigator` and register with `claude mcp add --scope user cwm-roslyn-navigator -- cwm-roslyn-navigator --solution ${workspaceFolder}`.

Use `cwm-roslyn-navigator` tools to minimize token consumption:

- **Before modifying a type** — `find_symbol` to locate it, `get_public_api` to understand its surface
- **Before adding a reference** — `find_references` to understand existing usage
- **To understand architecture** — `get_project_graph` to see project dependencies and confirm layers only depend inward
- **To find implementations** — `find_implementations` instead of grep
- **To check for errors** — `get_diagnostics` after changes

## Commands

```bash
# Build entire solution
dotnet build LeanForgeLMS.slnx

# Run via Aspire AppHost (recommended — wires up dashboard, service discovery, OTEL)
dotnet run --project LeanForgeLMS.AppHost

# Run the API directly (without Aspire)
dotnet run --project LF.WebApi

# Run the frontend dev server (proxied by LF.WebApi in Development)
cd lf.webapp && npm run dev

# Run all tests
dotnet test

# Add EF migration (AppDbContext is only registered in LF.IdentityService's DI container — use it as the startup project, not LF.WebApi)
dotnet ef migrations add [Name] \
  --project LF.Infrastructure \
  --startup-project LF.IdentityService \
  --context AppDbContext

# Format check
dotnet format --verify-no-changes
```

## Workflow

- **Plan first** — Enter plan mode for any non-trivial task (3+ steps or architecture decisions). Iterate until the plan is solid before writing code.
- **Verify before done** — Run `dotnet build` and `dotnet test` after changes. Use `get_diagnostics` via MCP to catch warnings.
- **Fix bugs autonomously** — Investigate and fix without hand-holding; check logs, errors, failing tests.
- **Stop and re-plan** — If implementation goes sideways, stop and re-plan rather than pushing through a broken approach.
- **Use subagents** — Offload research, exploration, and parallel analysis. One task per subagent for focused execution.
- **Learn from corrections** — Capture the pattern in memory after any correction.

## Anti-patterns

Do NOT generate code that:

- Adds new MVC controllers — use `IEndpointGroup` Minimal APIs for new features (existing auth controllers are the one exception, left as-is)
- Defines new endpoints directly in `Program.cs` — use `IEndpointGroup` per feature with auto-discovery
- Puts business logic in endpoints — endpoints map HTTP to use cases and back, nothing more
- Puts business logic in Application handlers instead of Domain entities — avoid an anemic domain model; entities own their own invariants
- References EF Core, ASP.NET Core, or any framework type from `LF.AppDomain` — the Domain layer has zero project/framework dependencies
- Creates a repository interface per entity — use `IAppDbContext` with `DbSet<T>` directly; only add a repository for genuinely complex, reusable query logic
- Uses `DateTime.Now` — use `TimeProvider` injection instead
- Creates `new HttpClient()` — use `IHttpClientFactory`
- Uses `async void` — always return `Task`
- Blocks with `.Result` or `.Wait()` — await instead
- Uses `Results.Ok()` — use `TypedResults.Ok()` for OpenAPI
- Returns domain entities from endpoints — always map to response DTOs
- Uses in-memory database for tests — use Testcontainers with real PostgreSQL
- Catches bare `Exception` — catch specific types, let the global handler catch the rest
- Uses string interpolation in log messages — use structured logging templates
- Modifies the existing JWT/Cookie/OIDC scheme wiring in `LF.WebApi/Program.cs` without explicit discussion — it has specific interplay between the temp cookie sign-in scheme and OIDC code redemption
- Changes `user_service.proto` or the gRPC contract between `LF.WebApi` and `LF.IdentityService` without checking both call sites — it's a real cross-service boundary now
- Assumes `AppDbContext` is DI-resolvable in `LF.WebApi` — it's only registered in `LF.IdentityService/Program.cs` today
