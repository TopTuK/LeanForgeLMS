# LeanForgeLMS — Clean Architecture

## Project Context

LeanForgeLMS is a Learning Management System built as a .NET 10 application. The domain has moderate business rules (enrollment, progress tracking, grading, course lifecycle) — not CRUD-only, but not a rich DDD domain either. It's a solo-developer project.

The application runs as a single deployable unit (`LF.WebApi`) orchestrated by .NET Aspire, fronted by a Vue 3 SPA. A second Aspire-managed project, `LF.IdentityService`, exists alongside it but is still an unfinished scaffold — see [IdentityService status](#identityservice-status) below before building conventions around it.

**Migration note:** this project originally started as a Modular Monolith (`LF.Modules.[Module]` class libraries). That direction has been abandoned in favor of layered Clean Architecture — `LF.AppDomain` (Domain), `LF.Application` (Application), `LF.Infrastructure` (Infrastructure), `LF.WebApi` (Api). If you find references to `LF.Modules.*` anywhere, they're stale.

## Current Solution Structure

```
LeanForgeLMS.slnx
LeanForgeLMS.AppHost/            # .NET Aspire orchestration — registers lf-identityservice; lf-webapi registration is commented out (see AppHost.cs)
LeanForgeLMS.ServiceDefaults/    # Aspire service defaults — OpenTelemetry, resilience, service discovery
LF.AppDomain/                    # Domain layer — currently empty, no project references (by design)
LF.Application/                  # Application layer — currently empty
LF.Infrastructure/                # Infrastructure layer — has AppDbContext (Npgsql referenced, not yet registered in DI, no connection string configured)
LF.WebApi/                       # Host project (Microsoft.NET.Sdk.Web) — MVC controllers + JWT/Cookie/OIDC auth, references ServiceDefaults only
LF.IdentityService/              # Separate Aspire project — still the default gRPC "Greeter" template, purpose undecided (see below)
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

`LF.IdentityService` is scaffolded (Aspire + gRPC `Greeter` template) but its role hasn't been decided — it may become a real separate identity/auth service, or get folded back into `LF.WebApi`. Until that's settled:

- Don't build architectural conventions (module boundaries, event contracts, etc.) around it.
- Don't wire real business logic into it without checking first.
- Note `AppHost.cs` currently has `lf-webapi` commented out and `lf-identityservice` registered twice — worth cleaning up whenever this project is picked back up, but don't touch it as a drive-by edit.

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
- **Entity Framework Core + PostgreSQL (Npgsql)** — `Npgsql.EntityFrameworkCore.PostgreSQL` already referenced in `LF.Infrastructure`; `AppDbContext` exists but isn't registered in DI yet and has no connection string configured. Aspire AppHost already provisions a Postgres resource (`postgres`, database `aiquill`).
- **Mediator** (source-generated, MIT) — command/query dispatch for Application-layer use cases; not yet added as a package reference.
- **FluentValidation** — request validation
- **Serilog** — structured logging (`Serilog.AspNetCore` already referenced in `LF.WebApi`, bootstrap logger configured in `Program.cs`)
- **xUnit v3 + Testcontainers + WebApplicationFactory** — testing (Testcontainers for real PostgreSQL in integration tests); no test projects exist yet
- **Frontend: Vue 3 + Vite** (`lf.webapp`) — Pinia for state, vue-router, vue-i18n, Vuestic UI, Tailwind CSS v4, axios for HTTP. Proxied to `http://localhost:5173` in dev via `UseSpa`/`UseProxyToSpaDevelopmentServer`; served from `wwwroot`/`MapFallbackToFile("index.html")` in production.

**Not yet decided / explicitly deferred:** caching (no HybridCache/Redis yet) and inter-service messaging (no Wolverine/MassTransit yet — `LF.IdentityService` currently would only be reachable via gRPC direct call, not a bus).

## Coding Standards

- **C# 14 features** — primary constructors, collection expressions, `field` keyword, records, pattern matching
- **File-scoped namespaces** — always (note: `LF.Infrastructure/db/AppDbContext.cs` still uses block-scoped namespace — fix opportunistically, don't leave as a pattern to copy)
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

# Add EF migration (once AppDbContext is registered in DI)
dotnet ef migrations add [Name] \
  --project LF.Infrastructure \
  --startup-project LF.WebApi \
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
- Builds real functionality into `LF.IdentityService` or wires it into `AppHost.cs` beyond cleanup, without checking first — its role isn't decided yet
