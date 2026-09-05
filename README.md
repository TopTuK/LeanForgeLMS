# Lean Forge LMS

[![Backend Tests](https://github.com/TopTuK/LeanForgeLMS/actions/workflows/tests.yml/badge.svg)](https://github.com/TopTuK/LeanForgeLMS/actions/workflows/tests.yml)
[![Webapp Tests](https://github.com/TopTuK/LeanForgeLMS/actions/workflows/webapp-tests.yml/badge.svg)](https://github.com/TopTuK/LeanForgeLMS/actions/workflows/webapp-tests.yml)

## What it is

Lean Forge LMS is a **Learning Management System for an online school for developers**. It
lets instructors author courses — chapters and lessons built from ordered rich-text, image,
video, audio, quiz and file blocks, with cover art and a publish workflow — and lets students
browse a catalog, enroll (free or paid), work through lessons, pass quizzes, and track their
progress. An admin area manages users, categories, promo codes, payment reporting, and a
runtime switch that turns student self-enrollment on or off without a redeploy.

It's a **solo-developer project** built on **.NET 10** and a **Vue 3** SPA. The backend runs
as four independently deployable processes — one public API/BFF plus three internal gRPC
services (identity, courses, payments) — sharing one PostgreSQL database and one MinIO object
store. Paid enrollment goes through **Robokassa** hosted checkout. Local development is
orchestrated with **.NET Aspire**; production is plain **Docker Compose**.

## Author & deployment

Built by **[Sergey Sidorov](https://s-sidorov.ru)**.

Production: **<https://lms.s-sidorov.ru>**

## Architecture

Four ASP.NET Core processes (`LF.WebApi` public + `LF.IdentityService` / `LF.CourseService` /
`LF.PaymentService` internal-only gRPC), a Vue 3 SPA served by `LF.WebApi`, one PostgreSQL
database, and MinIO for blobs. Each service is the single owner of its slice of the domain
and is reached only through its gRPC contract; inside each, code follows Clean Architecture
with dependencies pointing inward.

**Full architecture — service topology, auth flow, domain model, payments, deployment:
[`Architecture.md`](./Architecture.md).**

## Quick start

### Prerequisites

- .NET 10 SDK
- Node.js 22.18+ (or 24.12+)
- Docker (for the Aspire-managed Postgres/MinIO containers, or for `docker-compose.yml`)

### Run everything via Aspire

```bash
dotnet run --project LeanForgeLMS.AppHost
```

This starts Postgres, MinIO, the Vite dev server, and all four .NET hosts, wires connection
strings and service discovery between them, and opens the Aspire dashboard.

> A known health-probe interaction can make `lf-webapi` hang on startup. Workarounds (the
> `payment-check` launch profile, or running services standalone) are in
> [`Architecture.md`](./Architecture.md#local-development).

### Build & test

```bash
dotnet build LeanForgeLMS.slnx
dotnet test
cd lf.webapp && npm run lint && npm test
```

## Production deployment

```bash
cp .env.example .env   # fill in POSTGRES_PASSWORD, MINIO_ROOT_USER/PASSWORD, DefaultAuth__JwtKey,
                       # PmiAuth__*, GoogleAuth__*, Robokassa__* — SENTRY_DSN is optional
docker compose up --build
```

The compose topology (six services, two networks, why only `lf-webapi` is exposed) and the
full list of environment keys are in
[`Architecture.md`](./Architecture.md#deployment-topology-docker-compose).

## Contributing

Architecture rules, anti-patterns, and detailed conventions for contributing (Clean
Architecture layering, Minimal API endpoint groups, gRPC contract-change discipline,
auth-wiring cautions) live in [`CLAUDE.md`](./CLAUDE.md).
