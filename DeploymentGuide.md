# Lean Forge LMS — Deployment Guide

How to deploy Lean Forge LMS to a production server. For *what the system is*, see
[`README.md`](./README.md); for *how it is designed*, see [`Architecture.md`](./Architecture.md).

Production runs as **plain Docker Compose** on a single host. There is no Kubernetes, no
Aspire, and no orchestration beyond `docker compose` — Aspire is a local-development tool only.

---

## 1. What gets deployed

Six containers defined by [`docker-compose.production.yml`](./docker-compose.production.yml):

| Service | Image | Networks | Exposed? |
|---|---|---|---|
| `postgres` | `postgres:18-alpine` | `leanforge-internal` | No |
| `minio` | `minio/minio:latest` | `leanforge-internal` | No |
| `lf-identityservice` | `ghcr.io/toptuk/leanforgelms/identityservice` | `leanforge-internal` | No |
| `lf-courseservice` | `ghcr.io/toptuk/leanforgelms/courseservice` | `leanforge-internal` | No |
| `lf-paymentservice` | `ghcr.io/toptuk/leanforgelms/paymentservice` | `leanforge-internal` | No |
| `lf-webapi` | `ghcr.io/toptuk/leanforgelms/webapi` | `leanforge-public` + `leanforge-internal` + `pmi_network` | Loopback only |

`leanforge-internal` is declared `internal: true` — **the three gRPC services and the two data
stores have no outbound internet access at all**. Only `lf-webapi` reaches the internet (OIDC /
OAuth handshakes, Unleash) and only it receives inbound traffic.

**There is no SPA container.** `LF.WebApi/Dockerfile` is 3-stage: a `node:22` stage builds the
Vue app into `LF.WebApi/wwwroot`, then the .NET SDK stage publishes it into the image. In
production `LF.WebApi` serves the SPA itself via `MapFallbackToFile("index.html")`.

### Inbound traffic

`lf-webapi` is **not** published to the internet directly. A shared **nginx-proxy** container
(already running on the PROD host, on the external `pmi_network`) terminates TLS and routes by
hostname. `lf-webapi` advertises itself with `VIRTUAL_HOST` / `VIRTUAL_PORT` / `VIRTUAL_PROTO`.

The only host binding is `127.0.0.1:${WEBAPI_HOST_PORT:-8085}:8080` — loopback-only, for
`curl` debugging on the box, unreachable from outside.

Because TLS terminates at the proxy, `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` is set so
ASP.NET Core trusts `X-Forwarded-Proto`. **Without it the OIDC handler builds an `http://`
redirect URI that PMI rejects** — do not remove it.

---

## 2. Prerequisites

**On the PROD server**

- Docker Engine + Docker Compose v2
- The shared `nginx-proxy` stack running on an external Docker network named `pmi_network`,
  with TLS/ACME already configured for `*.s-sidorov.ru`
- SSH access on port **45654** for the deploy user
- Disk for two named volumes: `leanforge-postgres-data`, `leanforge-minio-data`

**DNS** — an A record for the public hostname (default `lms.s-sidorov.ru`) pointing at the server.

**External services to have ready before the first deploy**

| Service | Needed for | What you need |
|---|---|---|
| PMI Club (OIDC) | Primary login | Client ID + secret + discovery URL |
| Google OAuth | Social login | Client ID + secret |
| Yandex OAuth | Social login | Client ID + secret |
| Robokassa | Paid enrollment | Merchant login + Password1 + Password2 |
| **Unleash** | Feature flags | Server URL + **client** API token (see [§5](#5-feature-flags-unleash)) |
| Sentry | Error monitoring | DSN — **optional**, blank disables it |

Postgres and MinIO are containers in the compose file — nothing to provision separately.

---

## 3. Environment variables

All secrets live in a single `.env` file **next to `docker-compose.production.yml` on the
server**. It is gitignored and has two maintenance paths:

- **Preferred — the [`Manual sync production .env`](./.github/workflows/sync-production-env.yml)
  workflow.** GitHub Actions secrets/variables are the source of truth; the workflow renders
  `.env` from them and ships it to the server (previous file backed up). See
  [§3.1](#31-syncing-env-from-github-actions).
- **Fallback — hand-edit `.env` on the server** for a one-off tweak. The next workflow run
  overwrites it, so fold any manual change back into the GitHub secrets.

[`.env.example`](./.env.example) is the field reference either way.

Values reach the apps through ASP.NET Core's double-underscore convention
(`Robokassa__Password1` → `Robokassa:Password1`).

| Variable | Required | Notes |
|---|---|---|
| `POSTGRES_USER` / `POSTGRES_DB` | No | Default `leanforge` |
| `POSTGRES_PASSWORD` | **Yes** | Compose fails fast if unset |
| `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` | **Yes** | Compose fails fast if unset |
| `DefaultAuth__JwtKey` | **Yes** | ≥32 chars. Signs session JWTs — rotating it logs everyone out |
| `PmiAuth__ClientId` / `__ClientSecret` / `__OpenIdConfigurationUrl` | **Yes** | PMI Club OIDC |
| `GoogleAuth__ClientId` / `__ClientSecret` | **Yes** | |
| `YandexAuth__ClientId` / `__ClientSecret` | **Yes** | |
| `Robokassa__MerchantLogin` / `__Password1` / `__Password2` | **Yes** | From the merchant cabinet |
| `Robokassa__HashAlgorithm` | **Yes** | Must match the cabinet setting (`MD5`/`SHA256`/`SHA512`) |
| `Robokassa__IsTest` | **Yes** | `false` for real payments |
| `Robokassa__SuccessUrl` / `__FailUrl` | **Yes** | Public SPA routes, e.g. `https://lms.s-sidorov.ru/payments/success` |
| `Unleash__ApiKey` | Recommended | Blank ⇒ all flags off ⇒ **self-enrollment disabled** |
| `UNLEASH_API_URL` | No | Defaults to `https://features.s-sidorov.ru/api/` |
| `SENTRY_DSN` | No | Blank disables Sentry |
| `WEBAPI_VIRTUAL_HOST` | No | Defaults to `lms.s-sidorov.ru` |
| `WEBAPI_HOST_PORT` | No | Loopback debug port, default `8085` |

### Not in `.env`

**`DefaultAdmins`** — the seed admin list lives in
[`LF.IdentityService/appsettings.json`](./LF.IdentityService/appsettings.json), not `.env`. It
grants `Admin` role on first login by email. To change it on a deployed box without rebuilding,
add indexed env vars to the `lf-identityservice` service:

```yaml
DefaultAdmins__0__Email: "someone@example.com"
DefaultAdmins__0__FirstName: "Some"
DefaultAdmins__0__LastName: "One"
```

### 3.1 Syncing `.env` from GitHub Actions

[`.github/workflows/sync-production-env.yml`](./.github/workflows/sync-production-env.yml)
(**`Manual sync production .env`**, `workflow_dispatch` only) rebuilds the server's `.env`
from repository secrets/variables. One run:

1. renders `KEY=value` lines from the secrets/variables below (fails fast if a **required**
   one is empty),
2. SSHes to the PROD host (reusing `PRODUCTION_SSH_HOST` / `PRODUCTION_SSH_USERNAME` /
   `PRODUCTION_SSH_KEY` — the same secrets the deploy workflow uses),
3. copies the current `.env` to `.env.bak.<UTC timestamp>` in the deploy directory,
4. uploads the new `.env` (`chmod 600`),
5. runs `docker compose -f docker-compose.production.yml config -q` on the server to confirm
   every `${VAR:?…}` guard resolves.

**It does not restart anything.** The new values take effect on the next
[`Manual deploy production`](#6-deploying) run, or immediately if you SSH in and run
`docker compose -f docker-compose.production.yml up -d`.

**Run it:** GitHub → Actions → *Manual sync production .env* → *Run workflow*.

**Required secrets** (repository → Settings → Secrets and variables → Actions → *Secrets*).
Name each secret **exactly** as the `.env` key (names are case-insensitive):

| Secret | `.env` key it fills |
|---|---|
| `POSTGRES_PASSWORD` | `POSTGRES_PASSWORD` |
| `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` | same |
| `DefaultAuth__JwtKey` | `DefaultAuth__JwtKey` |
| `PmiAuth__ClientId` / `PmiAuth__ClientSecret` / `PmiAuth__OpenIdConfigurationUrl` | same |
| `GoogleAuth__ClientId` / `GoogleAuth__ClientSecret` | same |
| `YandexAuth__ClientId` / `YandexAuth__ClientSecret` | same |
| `Robokassa__MerchantLogin` / `Robokassa__Password1` / `Robokassa__Password2` | same |

**Optional secrets** — may be unset/blank: `Unleash__ApiKey` (blank ⇒ all flags off ⇒
self-enrollment disabled), `SENTRY_DSN` (blank ⇒ Sentry off).

**Variables** (same screen → *Variables* tab) — non-secret knobs, each with a built-in
default, so you only set the ones you want to override:

| Variable | Default |
|---|---|
| `POSTGRES_USER` / `POSTGRES_DB` | `leanforge` |
| `WEBAPI_VIRTUAL_HOST` | `lms.s-sidorov.ru` |
| `WEBAPI_HOST_PORT` | `8085` |
| `UNLEASH_API_URL` | `https://features.s-sidorov.ru/api/` |
| `Robokassa__HashAlgorithm` | `SHA256` |
| `Robokassa__IsTest` | `false` |
| `Robokassa__SuccessUrl` / `Robokassa__FailUrl` | `https://lms.s-sidorov.ru/payments/{success,fail}` |

Backups (`.env.bak.*`) accumulate in the deploy directory — prune them by hand
occasionally. To roll back a bad sync: `cp .env.bak.<ts> .env` on the server, then
`docker compose … up -d`.

---

## 4. One-time server preparation

```bash
# 1. Deploy directory (must match DEPLOY_DIR in the workflow)
mkdir -p /home/toptuk/leanforgelms && cd /home/toptuk/leanforgelms

# 2. Create .env. Preferred: set the repository secrets/variables (§3.1) and run the
#    "Manual sync production .env" workflow — it writes /home/toptuk/leanforgelms/.env
#    for you. It will warn that docker-compose.production.yml is not on the server yet;
#    that is expected on a first bring-up and the .env is still written.
#    Fallback: copy .env.example over by hand and fill in every required value.
vi .env
chmod 600 .env

# 3. The shared proxy network must exist and be external
docker network inspect pmi_network >/dev/null || docker network create pmi_network
```

Nothing else is needed: the compose file creates its own networks and volumes, database
migrations run automatically ([§7](#7-database-migrations)), and MinIO buckets are created on
startup by `MinioBucketInitializer` (`avatars` and `storage`).

---

## 5. Feature flags (Unleash)

Self-enrollment is gated by the Unleash flag **`lf.self_enrollment`**. There is no admin screen
and no database switch — the flag lives entirely in Unleash.

1. In Unleash, create a flag named exactly `lf.self_enrollment` in the project/environment your
   token targets.
2. Create a **client-side SDK token** (Settings → API access). It looks like
   `default:development.<secret>`. A *client* token is required — admin/frontend tokens will not
   authenticate the SDK.
3. Put it in `.env` as `Unleash__ApiKey`.

**`UNLEASH_API_URL` must include the `/api/` suffix** — the SDK appends `client/features` to it.
`https://features.s-sidorov.ru/api/` is correct; the bare host is not.

**Fail-closed behaviour.** If the key is missing, still the `CHANGE_ME` placeholder, rejected, or
the server is unreachable, every flag reads **false** and self-enrollment is blocked
(`POST /api/enrollments` → `409`). This is deliberate. It also means a wrong key looks exactly
like "enrollment is switched off" — check the logs before assuming the flag is off:

```bash
docker compose -f docker-compose.production.yml logs lf-webapi | grep -i unleash
```

A healthy start logs `UnleashInitializer::StartAsync: feature flag client ready with N known
toggles`. A failure logs `initial toggle fetch failed; starting in background-polling mode with
all flags off` — the app still boots and self-heals once Unleash is reachable again.

Only `lf-webapi` talks to Unleash. Do **not** add the flag client to the gRPC services: they sit
on the egress-less internal network and cannot reach it.

---

## 6. Deploying

### Automated (normal path)

GitHub Actions → **Manual deploy production** → *Run workflow*
([`.github/workflows/deploy-production.yml`](./.github/workflows/deploy-production.yml)).

The workflow is `workflow_dispatch` only — nothing deploys on push. It runs four jobs in order:

1. **Backend tests** — `dotnet test` on the whole solution (Release)
2. **Webapp tests** — `npm ci`, `lint:ci`, `test:coverage`, `build`
3. **Build and push images** — four images to GHCR, each tagged `latest` **and** the commit SHA
4. **Deploy to PROD** — `scp` the compose file to `/home/toptuk/leanforgelms/`, then
   `docker compose pull && up -d --no-build --remove-orphans && docker image prune -af`

A failing test blocks the image build, so a red test suite cannot reach production.

**Required repository secrets**

| Secret | Purpose |
|---|---|
| `TOKEN` | GHCR PAT with `write:packages` (push + server-side pull) |
| `PRODUCTION_SSH_HOST` | PROD hostname/IP |
| `PRODUCTION_SSH_USERNAME` | SSH user (must be in the `docker` group) |
| `PRODUCTION_SSH_KEY` | Private key, PEM format |

The deploy job validates all three SSH secrets are non-empty before connecting — an empty
username otherwise surfaces as a confusing `runner@host: Permission denied`.

The `Manual sync production .env` workflow reuses the three `PRODUCTION_SSH_*` secrets and
needs its own set for the `.env` contents — see [§3.1](#31-syncing-env-from-github-actions).

### Manual (fallback / first bring-up)

From the deploy directory on the server, using published images:

```bash
cd /home/toptuk/leanforgelms
docker login ghcr.io -u <github-user> -p <PAT>
docker compose -f docker-compose.production.yml pull
docker compose -f docker-compose.production.yml up -d --remove-orphans
```

Or build from source on the server (uses `docker-compose.yml`, which publishes `lf-webapi` on
`WEBAPI_HOST_PORT` instead of going through nginx-proxy):

```bash
git clone https://github.com/TopTuK/LeanForgeLMS.git && cd LeanForgeLMS
cp .env.example .env && vi .env
docker compose up --build -d
```

Building the four images on the server is slow and memory-hungry — the `lf-webapi` image runs a
full Node SPA build plus a .NET publish. Prefer the registry path when you can.

---

## 7. Database migrations

**`LF.IdentityService` is the sole schema owner.** On startup it runs
`DatabaseInitializer.InitializeDatabaseAsync` → `Database.MigrateAsync()`, then seeds
`DefaultAdmins` and the starter categories and backfills the `CoursePayments` ledger. The other
three services just connect and assume the schema is current — which is why
`lf-courseservice`, `lf-paymentservice` and `lf-webapi` all `depends_on: lf-identityservice`.

Consequences worth knowing:

- **You never run `dotnet ef database update` against production.** Deploying a new
  `identityservice` image applies its migrations automatically.
- `depends_on: service_started` waits for the *container*, not for migrations to finish. On a
  migration that takes a while, the other services may briefly fail queries and restart. They
  recover; `restart: unless-stopped` handles it.
- **Take a database backup before deploying a release that contains a destructive migration**
  (a dropped table or column). `Down()` methods exist but are not exercised in production.

```bash
# Backup before a risky deploy
docker compose -f docker-compose.production.yml exec -T postgres \
  pg_dump -U leanforge leanforge | gzip > backup-$(date +%F).sql.gz
```

To create a migration during development, `LF.IdentityService` is the startup project —
`AppDbContext` is not registered anywhere else:

```bash
dotnet ef migrations add <Name> \
  --project LF.Infrastructure --startup-project LF.IdentityService --context AppDbContext
```

---

## 8. Post-deploy verification

```bash
cd /home/toptuk/leanforgelms

# 1. All six containers up, none restarting
docker compose -f docker-compose.production.yml ps

# 2. No fatal startup errors
docker compose -f docker-compose.production.yml logs --tail=100 lf-webapi

# 3. Migrations applied
docker compose -f docker-compose.production.yml logs lf-identityservice | grep -i migrat

# 4. Feature flags connected
docker compose -f docker-compose.production.yml logs lf-webapi | grep -i "known toggles"

# 5. App answers on the loopback debug port
curl -si http://127.0.0.1:8085/ | head -n 1          # 200, SPA index
curl -si http://127.0.0.1:8085/api/platform/config   # 401 when anonymous — that is correct
```

> **There is no `/health` endpoint in production.** `MapDefaultEndpoints` maps `/health` and
> `/alive` **only in Development**, deliberately. Use container status and logs instead — a
> `404` on `/health` in prod is expected, not a fault.

Then from a browser: load `https://lms.s-sidorov.ru`, sign in through each configured provider,
and confirm the enroll CTA reflects the `lf.self_enrollment` flag.

For a payment smoke test, keep `Robokassa__IsTest=true` for the first run and confirm the
ResultURL webhook lands. Robokassa must be configured with:

```
https://<your-host>/api/payments/robokassa/result
```

---

## 9. Rollback

Every deploy pushes a commit-SHA tag alongside `latest`, so rolling back is a tag change.

```bash
cd /home/toptuk/leanforgelms

# Pin the failing service(s) to a known-good SHA
sed -i 's|webapi:latest|webapi:<good-sha>|' docker-compose.production.yml
docker compose -f docker-compose.production.yml up -d --no-build

# Full rollback: pin all four services to the same SHA
```

Caveats:

- The deploy job ends with `docker image prune -af`, which removes every local image not used by
  a running container — including the previous SHA-tagged ones. A rollback therefore **re-pulls
  from GHCR**; confirm the tag still exists there before relying on it.
- Rolling back `identityservice` does **not** roll back a migration that already ran. If the bad
  release included a destructive migration, restore the database backup instead.
- The workflow overwrites `docker-compose.production.yml` on every deploy, so a pinned tag is
  reverted by the next run. Fix forward once the cause is understood.

---

## 10. Troubleshooting

| Symptom | Likely cause |
|---|---|
| Enrollment always returns `409` | Unleash key missing/rejected, or the flag is off. Grep the `lf-webapi` logs for `unleash` |
| OIDC login redirects to `http://` and PMI rejects it | `ASPNETCORE_FORWARDEDHEADERS_ENABLED` missing on `lf-webapi` |
| `lf-webapi` cannot reach the gRPC services | `Services__lf-*__http__0` entries wrong, or the container missing from `leanforge-internal` |
| gRPC calls fail with a protocol error | `DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2UNENCRYPTEDSUPPORT=1` missing (the gRPC hosts are h2c-only) |
| Site unreachable but containers healthy | `pmi_network` not joined, or `VIRTUAL_HOST` does not match DNS |
| Course/enrollment queries fail right after deploy | `lf-identityservice` still migrating; wait and re-check |
| Avatars/media 404 | MinIO volume lost, or bucket init failed — check `lf-webapi` logs for `MinioBucketInitializer` |
| Compose exits with `Set X in .env` | A required variable is unset; compose fails fast by design. If `.env` is managed by the sync workflow ([§3.1](#31-syncing-env-from-github-actions)), add the missing secret/variable and re-run it |
| `.env` on the server looks stale after rotating a secret | Re-run `Manual sync production .env`, then `docker compose -f docker-compose.production.yml up -d`. Previous files are kept as `.env.bak.<timestamp>` in the deploy directory |
| Sync workflow fails at *Render .env* with `required secret … is not set` | That repository secret is missing/empty; the server `.env` is left untouched |

Useful commands:

```bash
docker compose -f docker-compose.production.yml logs -f --tail=200 <service>
docker compose -f docker-compose.production.yml restart <service>
docker compose -f docker-compose.production.yml exec postgres psql -U leanforge -d leanforge
```

---

## 11. Backups

Two named volumes hold all persistent state:

- `leanforge-postgres-data` — every table (users, courses, enrollments, payments)
- `leanforge-minio-data` — avatars, course covers, lesson media

Neither is backed up automatically. At minimum, schedule the `pg_dump` from
[§7](#7-database-migrations) plus a periodic copy of the MinIO volume. Nothing in the
application layer stores state outside these two volumes and `.env`.
