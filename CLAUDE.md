# LeanForgeLMS — Clean Architecture

## Project Context

LeanForgeLMS is a Learning Management System built as a .NET 10 application. The domain has moderate business rules (enrollment, progress tracking, grading, course lifecycle) — not CRUD-only, but not a rich DDD domain either. It's a solo-developer project.

The application runs as two Aspire-managed services fronted by a Vue 3 SPA: `LF.WebApi` (MVC auth controllers + the growing Minimal API surface) and `LF.IdentityService`, a gRPC service that owns user identity data and is called from `LF.WebApi` as a gRPC client (shared `user_service.proto`) — see [IdentityService status](#identityservice-status) below.

**Migration note:** this project originally started as a Modular Monolith (`LF.Modules.[Module]` class libraries). That direction has been abandoned in favor of layered Clean Architecture — `LF.AppDomain` (Domain), `LF.Application` (Application), `LF.Infrastructure` (Infrastructure), `LF.WebApi` (Api). If you find references to `LF.Modules.*` anywhere, they're stale.

## Current Solution Structure

```
LeanForgeLMS.slnx
LeanForgeLMS.AppHost/            # .NET Aspire orchestration — postgres wired to lf-identityservice; minio (avatar storage) and the lf-webapp Vite dev server wired to lf-webapi; lf-webapi references lf-identityservice (gRPC client) and waits for it
LeanForgeLMS.ServiceDefaults/    # Aspire service defaults — OpenTelemetry, resilience, service discovery, and centralized Serilog console logging (two-stage bootstrap + request logging) shared by all 3 host projects
LF.AppDomain/                    # Domain layer — has real content: Entities/User/DbUser.cs, Models/User/Enums/UserRole.cs; zero project references (by design)
LF.Application/                  # Application layer — Services/Authentication (AuthenticationService, TokenService), Services/User/UserService, Services/Profile/ProfileService, Services/Admin/AdminUserService, ModelDto/*, Common/Interfaces/IAppDbContext.cs + IFileStorageService.cs
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

LF.Application/
  Common/
    Interfaces/
      IAppDbContext.cs           # DbContext abstraction — prefer this over repositories
    Exceptions/                  # Cross-cutting Application-layer exceptions (e.g. SelfAdministrationException)
  Services/
    Course/
      ICourseService.cs          # Use-case interface, implementation is internal sealed
      CourseService.cs

LF.Infrastructure/
  Persistence/
    AppDbContext.cs               # Implements IAppDbContext (move out of Infrastructure/db/)
    Configurations/
    Migrations/
  DependencyInjection.cs          # AddInfrastructure extension

LF.WebApi/
  Endpoints/
    CourseEndpoints.cs            # Thin, maps HTTP <-> use-case service calls
    CourseModels.cs               # Request/response DTOs + FluentValidation validators, co-located
  Controllers/                    # Existing MVC auth controllers stay here — do not add new ones
  Program.cs
```

### IdentityService Status

`LF.IdentityService` has moved past the scaffold stage: it's a real gRPC service (`RpcUserService : UserServiceRpc.UserServiceRpcBase`, `GetOrCreateUser`) that owns the Postgres-backed `AppDbContext` and user identity data. `LF.WebApi` talks to it as a gRPC client over the shared `user_service.proto` contract. The original default-template `Protos/greet.proto` is still present but unused — cleanup candidate, not a pattern to extend.

- Treat it as a real service boundary now — module/contract conventions can be built around it.
- The gRPC contract (`user_service.proto`) is a cross-service boundary; changes to it affect both `LF.WebApi` and `LF.IdentityService` — check both call sites before editing messages/RPCs.
- `AppDbContext` DI registration lives only in `LF.IdentityService/Program.cs` (via `AddInfrastructureDatabase()`); `LF.WebApi` does not register it despite referencing `LF.Infrastructure` — don't assume `AppDbContext` is resolvable in `LF.WebApi` without checking first.
- **Known issue**: `LF.IdentityService/appsettings.json` sets `Kestrel:EndpointDefaults:Protocols` to `Http2` (standard gRPC-service scaffolding), which also makes its `/health` endpoint HTTP/2-only. `LeanForgeLMS.AppHost`'s `WaitFor(identityService)` health probe uses HTTP/1.1 and gets rejected, so `lf-webapi` can hang indefinitely waiting to start under `dotnet run --project LeanForgeLMS.AppHost` (confirmed by direct observation — Postgres/Minio/IdentityService come up fine, `lf-webapi` just never launches). Workaround for local testing: run `LF.IdentityService` and `LF.WebApi` standalone instead (`dotnet run --project LF.WebApi`, `--no-launch-profile`), pointing each at the already-running Postgres/MinIO with explicit env vars — `ConnectionStrings__leanforge` (IdentityService), and for WebApi: `Services__lf-identityservice__http__0=<host>:<port>`, `ConnectionStrings__minio=Endpoint=http://<host>:<port>/;AccessKey=...;SecretKey=...` (same format as `docker-compose.yml`'s `lf-webapi` service), plus `DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2UNENCRYPTEDSUPPORT=1` so the gRPC client can reach IdentityService's h2c-only Kestrel. Root-fixing the AppHost hang (e.g. adjusting the health check or Kestrel protocol config) hasn't been attempted — flag it if it comes up again.

## API Style

New endpoints use Minimal APIs — `IEndpointGroup` per feature in `LF.WebApi/Endpoints/`, auto-discovered, using `TypedResults` for OpenAPI. Endpoints stay thin and delegate to Application-layer use-case services injected directly as minimal-API delegate parameters — there is no mediator/dispatcher layer in this codebase (Mediator/`ISender` was evaluated and deliberately not adopted; see Tech Stack).

The existing auth setup in `LF.WebApi/Program.cs` (`AddControllersWithViews`, `MapControllerRoute`) stays as MVC controllers — it is not being retrofitted. Don't add new controllers; add new endpoints as `IEndpointGroup`s instead.

```csharp
public sealed class CourseEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses").WithTags("Courses").RequireAuthorization();

        group.MapPost("/", async Task<Results<Created<CourseResponse>, ValidationProblem>>
            (CreateCourseRequest request, ICourseService courseService, CancellationToken ct) =>
        {
            var validation = new CreateCourseRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var course = await courseService.CreateCourseAsync(request.ToDto());
            return TypedResults.Created($"/api/courses/{course.Id}", ToResponse(course));
        });
    }
}
```

## Tech Stack

- **.NET 10** / C# 14
- **ASP.NET Core** — Minimal APIs for new endpoints (`IEndpointGroup` + auto-discovery); existing auth stays MVC controllers
- **.NET Aspire** — AppHost + ServiceDefaults already wired up; OpenTelemetry, resilience, service discovery come from ServiceDefaults
- **Authentication** — JWT Bearer (primary scheme) + Cookie + OpenID Connect via Duende.IdentityModel, already implemented in `LF.WebApi/Program.cs`. Do not change the auth scheme wiring without discussing it first — it has specific cookie/OIDC/JWT interplay (temp cookie sign-in scheme, authorization code redemption).
  - **Session token delivery**: the app JWT is issued into an **HttpOnly + `SameSite=Lax`** cookie (`AuthController.IssueSessionCookieAsync`, `DevAuthEndpoints`; `Secure` in non-Development). The SPA never reads the token — `AddJwtBearer`'s `OnMessageReceived` pulls it from that cookie (the `Authorization: Bearer` header still works as a fallback for API clients and `LF.WebApiTests`). `lf.webapp` has no `js-cookie` dependency; `src/services/api.js` just sets `withCredentials: true` and the SPA's auth state comes from a one-time `authStore.ensureInitialized()` probe of `/api/Profile` (gated in the router `beforeEach`). Don't reintroduce a JS-readable session cookie.
  - **Role-based authorization policies**: the app JWT is issued with a literal `"role"` claim (`AuthController`/`DevAuthEndpoints`), but the JWT Bearer handler's default `MapInboundClaims = true` (left at its default for that scheme) remaps `"role"` → `ClaimTypes.Role` when building the `ClaimsPrincipal` for each request. Any `RequireClaim(...)`/role-checking policy must check `ClaimTypes.Role`, not the literal `"role"` string, or it will silently 403 everyone including the intended role — confirmed by running the app (`AddPolicy("AdminOnly", p => p.RequireClaim(ClaimTypes.Role, ...))` in `Program.cs` is the working pattern; this is additive and doesn't touch the scheme wiring itself).
  - Dev-only login shortcuts live in `LF.WebApi/Endpoints/DevAuthEndpoints.cs` (`GET /api/dev-auth/{role}`, Development-only) — personas for Student/Instructor/CourseCreator/Admin configured in `DevAuthOptions`/`appsettings.Development.json`. Useful for exercising role-gated endpoints without a real PMI/Google login.
- **Entity Framework Core + PostgreSQL (Npgsql)** — `Npgsql.EntityFrameworkCore.PostgreSQL` referenced in `LF.Infrastructure`; `AppDbContext` (in `Persistence/`) is registered via `AddInfrastructureDatabase()`, but only in `LF.IdentityService/Program.cs` — `LF.WebApi` does not call it. Aspire AppHost provisions a Postgres resource (`postgres`, database `leanforge`) wired only to `lf-identityservice`.
- **No mediator/dispatcher library** — Mediator (`Mediator.SourceGenerator`, source-generated, MIT, by martinothamar) was tried for the admin-user-management feature (`LF.Application/Users/` command/query handlers dispatched via `ISender`) and explicitly rolled back by decision — this codebase uses direct service injection instead (see `AdminUserService`/`IAdminUserService` in `LF.Application/Services/Admin/` as the reference pattern, mirroring `ProfileService`/`IProfileService`). Do not reintroduce Mediator/`ISender` without discussing it first. Two things worth remembering from that experiment if a dispatcher library is ever reconsidered: (1) if handlers depend on scoped services, the dispatcher must be registered `Scoped`, not the library-recommended `Singleton`, or it's a captive-dependency crash that `dotnet build`/`dotnet test` won't catch unless a DI test uses `ValidateOnBuild: true` with real (non-singleton-mocked) service lifetimes; (2) FluentValidation's `AddValidatorsFromAssembly` needs `includeInternalTypes: true` since validators are `internal sealed` by convention here.
- **FluentValidation** — request validation, referenced in `LF.WebApi` only. Validators are manually instantiated inline in endpoints — `new XValidator().Validate(request)`, mapped to `TypedResults.ValidationProblem(validation.ToDictionary())` on failure — co-located with their request DTO in the feature's `*Models.cs` file (e.g. `ProfileModels.cs`, `AdminUserModels.cs`). Not referenced from `LF.Application`.
- **Serilog** — structured logging, centralized in `LeanForgeLMS.ServiceDefaults/Extensions.cs` (`Serilog.AspNetCore` referenced there only) and applied transitively to all 3 host projects (`LF.WebApi`, `LF.IdentityService`, `LF.CourseService`) via the shared `AddServiceDefaults()`. Console output uses `AnsiConsoleTheme.Code` with a `[{Timestamp} {Level}] ({Application}) {Message}` template. Each `Program.cs` follows the two-stage bootstrap pattern (https://github.com/serilog/serilog-aspnetcore): call `Extensions.CreateBootstrapLogger()` before `WebApplication.CreateBuilder(args)`, wrap the whole body in `try { ... } catch (Exception ex) { Log.Fatal(ex, "..."); } finally { Log.CloseAndFlush(); }`, and call `app.UseDefaultRequestLogging()` right after `builder.Build()` for one summary line per request/RPC (health/liveness polling is demoted to `Verbose` so it doesn't spam the console). `AddServiceDefaults()` calls `builder.Logging.ClearProviders()` *before* wiring Serilog and OpenTelemetry logging — ordering matters: it must run before `ConfigureOpenTelemetry()` adds the OTel logging provider, or that provider gets wiped too and the Aspire dashboard's log view silently breaks. `AddSerilog()` also bypasses `appsettings.json`'s `Logging:LogLevel` filtering for its own provider, so noise suppression (e.g. `Microsoft.AspNetCore` → Warning) is set via `MinimumLevel.Override(...)` in code instead. gRPC caveat: `UseSerilogRequestLogging()` reads the HTTP status code, which doesn't reflect gRPC status (carried in HTTP/2 trailers) — a failed RPC still logs as 200.
- **MinIO** — object storage for user avatar uploads. AppHost provisions a `minio` container (`CommunityToolkit.Aspire.Hosting.Minio`) referenced by `lf-webapi`; `LF.WebApi/Program.cs` calls `builder.AddMinioClient("minio")` (`CommunityToolkit.Aspire.Minio.Client`) directly — `IMinioClient` is only resolvable in `LF.WebApi`, not `LF.IdentityService`. `LF.Infrastructure`'s `MinioFileStorageService` (behind `IFileStorageService`) does the actual upload/download/delete; the avatar *key* (not the file) is persisted in Postgres via the existing gRPC round-trip to `LF.IdentityService`. Avatar bytes are always proxied through `LF.WebApi` — MinIO itself is never exposed to the browser (internal-only network in `docker-compose.yml`, matching `postgres`).
- **xUnit v3** — `LF.ApplicationTests` unit-tests `LF.Application` services (`UserService`, `ProfileService`, `AuthenticationService`, `TokenService`, mapping configs, DI registration) using Moq + `MockQueryable.Moq` to mock `IAppDbContext`/`DbSet<T>`. Testcontainers + WebApplicationFactory-based integration testing (real PostgreSQL, real HTTP pipeline) is still aspirational — no integration test project exists yet.
- **Frontend: Vue 3 + Vite** (`lf.webapp`) — Pinia for state, vue-router, vue-i18n, Vuestic UI, Tailwind CSS v4, axios for HTTP. Proxied to `http://localhost:5173` in dev via `UseSpa`/`UseProxyToSpaDevelopmentServer`; served from `wwwroot`/`MapFallbackToFile("index.html")` in production. Most hand-built pages (Profile, Login, Courses, marketing Home) use Tailwind + custom CSS-variable design tokens directly, with Vuestic UI registered (`createVuestic()`) but otherwise unused; the `admin/` views (`src/views/admin/`, `src/layout/AdminLayout.vue`) are the first to actually use Vuestic components (`va-data-table`, `va-sidebar`, `va-modal`, `va-select`, etc.) — a deliberate per-feature choice given CRUD-heavy admin screens, not a project-wide switch.

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
- Introduces Mediator/MediatR/`ISender` or any other mediator/dispatcher library — endpoints call Application-layer use-case services directly (see Tech Stack); this was tried for the admin feature and explicitly rolled back
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
