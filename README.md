<div align="center">
  <img src="./refs/icon.svg" width="80" alt="HookSentry" />
  <h1>HookSentry Cloud</h1>
  <p><strong>Managed SaaS layer for the HookSentry webhook delivery platform</strong></p>
  <p>
    Wraps the open-source HookSentry core with multi-tenant plans, quota enforcement,<br/>
    usage metering, tenant lifecycle, cloud RBAC, and data-retention automation.
  </p>
</div>

---

## What this is

The OSS [HookSentry core](https://github.com/RhaynerRS/hooksentry-core) is the delivery engine: it receives events over HTTP, queues them through RabbitMQ, and delivers to configured destinations with retries, circuit breaking and HMAC signing.

**HookSentry Cloud** takes that same engine and adds everything needed to run it as a multi-tenant hosted service:

| Capability | What it does |
|-----------|--------------|
| **Plans** | Per-tenant limits (users, destinations, events/month, retention days). A `free` plan is seeded on first boot (1,000 events/month, 7-day retention). |
| **Quota enforcement** | Ingest requests are counted per tenant per month in Redis. Over the limit → `429 Too Many Requests`. |
| **Usage metering** | Live counters in Redis, flushed nightly into `usage_snapshots` for reporting. |
| **Tenant blocking** | Block/unblock a tenant; blocked tenants get `403 tenant_blocked` on every request. |
| **Cloud RBAC** | Extra roles — `Owner`, `Admin`, `Developer`, `Viewer` — with policy-based authorization and last-owner protection. |
| **Data retention** | Old events are pruned per plan retention window; tenants blocked > 30 days are fully purged. |
| **Quota warnings** | Owners are notified when a tenant crosses 80% of its monthly event quota. |
| **Abuse protection** | Registration guards (fingerprint, per-IP rate limit, disposable-email block) driven by config. |

The core lives here as a git submodule (`hooksentry/`). The `HookSentry.Cloud.Host` project composes the OSS core and the SaaS extensions into a single API process.

## System Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker and Docker Compose
- Git (the OSS core is a submodule — clone recursively)

## Getting Started

Clone with the submodule:

```bash
git clone --recurse-submodules https://github.com/RhaynerRS/hooksentry-cloud.git
# already cloned?
git submodule update --init --recursive
```

### Full stack (with Grafana, Loki, Tempo)

```bash
cp .env.example .env          # fill in secrets
docker compose -f docker-compose.cloud.yml up -d
```

| Service | URL |
|---------|-----|
| API | http://localhost:5143 |
| Swagger UI | http://localhost:5143/swagger |
| Grafana | http://localhost:3001 |
| RabbitMQ | http://localhost:15672 |

The compose file builds the cloud API from `src/HookSentry.Cloud.Host/Dockerfile` and the delivery worker from the submodule (`hooksentry/src/HookSentry.Worker`), alongside PostgreSQL, RabbitMQ, Redis and the observability stack.

### Running locally (development)

```bash
# Start infrastructure only
docker compose -f docker-compose.cloud.yml up postgres rabbitmq redis -d

# Cloud API (terminal 1)
dotnet run --project src/HookSentry.Cloud.Host

# Delivery worker (terminal 2)
dotnet run --project hooksentry/src/HookSentry.Worker
```

The API listens on http://localhost:5108 with Swagger at http://localhost:5108/swagger.

## Building

```bash
dotnet build HookSentry.Cloud.slnx
```

Docker build (cloud API):

```bash
docker build -f src/HookSentry.Cloud.Host/Dockerfile -t hooksentry-cloud-api .
```

## Testing

```bash
dotnet test src/HookSentry.Billing.Tests
```

## Architecture

The host references the OSS core assemblies from the submodule and layers SaaS behavior on top. `Program.cs` wires OSS core services first, then the cloud extensions and middleware.

**Projects:**

| Project | Type | Responsibility |
|---------|------|----------------|
| `HookSentry.Cloud.Host` | ASP.NET Core Web API | Composition root — OSS core + SaaS extensions, migrations, endpoint mapping |
| `HookSentry.Billing` | Class Library | Plans, quota, usage, tenant state, cloud RBAC, retention/purge jobs, billing endpoints |
| `HookSentry.Subscriptions` | Class Library | Abuse-protection options and subscription wiring (in progress) |
| `HookSentry.Billing.Tests` | xUnit | Plan and tenant-state unit tests |
| `hooksentry/` *(submodule)* | — | OSS core: `Domain`, `Infrastructure`, `Api`, `Worker` |

### Request pipeline (cloud additions)

```
Authentication
  → TenantAccess        (403 if tenant blocked)
  → QuotaEnforcement    (429 if monthly event quota exceeded, ingest only)
  → LastOwnerProtection (blocks removing/demoting the last Owner)
  → Authorization
```

### Background jobs

| Job | Schedule (UTC) | What it does |
|-----|----------------|--------------|
| `EventRetentionJob` | daily 02:00 | Deletes events older than the plan's retention window (batched). |
| `UsageFlushJob` | daily 03:00 | Snapshots per-tenant Redis usage counters into `usage_snapshots`. |
| `QuotaWarningJob` | daily 09:00 | Notifies Owners once per period when usage ≥ 80% of quota. |
| `DataPurgeJob` | monthly, 1st 04:00 | Hard-deletes all data for tenants blocked more than 30 days. |
| `PlanSeeder` | on startup | Seeds the `free` plan if no plans exist. |

## API

The cloud host exposes **all OSS core endpoints** (auth, tenants, users, invites, API keys, destinations, senders, events, ingest, health — see the [core README](hooksentry/README.md)) plus the cloud-specific routes below.

### Cloud

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET` | `/cloud/plans` | — | List available plans with their limits (public) |
| `GET` | `/cloud/usage` | JWT | Current-month event usage for the authenticated tenant (`current`, `limit`, `percentage`, `warning`) |

### Tenant lifecycle (internal)

> These routes are for internal/service use. Service authentication is planned — see the TODOs on each endpoint.

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/internal/tenants/{tenantId}/block` | Block a tenant (`reason`: `abuse`, `manual`, `quota_abuse`). Upserts `TenantCloudState`, invalidates Redis cache. |
| `POST` | `/internal/tenants/{tenantId}/unblock` | Remove a tenant's block. |

## Configuration

The cloud API reuses the core's configuration keys and adds a few of its own. Minimum `.env` for the compose stack:

```env
POSTGRES_DB=hooksentry
POSTGRES_USER=hooksentry
POSTGRES_PASSWORD=change-me
REDIS_PASSWORD=change-me
RABBITMQ_USER=hooksentry
RABBITMQ_PASSWORD=change-me
JWT_KEY=CHANGE_ME_MIN_32_CHARACTERS_LONG_SECRET
API_KEY=change-this-api-key
CREDENTIAL_ENCRYPTION_KEY=CHANGE_ME_32_BYTES_BASE64_ENCODED=
STRIPE_SECRET_KEY=sk_test_placeholder
STRIPE_WEBHOOK_SECRET=whsec_placeholder
```

Cloud protection settings live under `CloudProtection` in `appsettings.json`:

```json
{
  "CloudProtection": {
    "EventRetention":    { "Enabled": true, "RetentionDays": 7 },
    "RegistrationAbuse": {
      "FingerprintEnabled": true,
      "MaxAccountsPerFingerprint": 3,
      "FingerprintBlockWindowDays": 30,
      "RegistrationRateLimitEnabled": true,
      "MaxRegistrationsPerIpPerHour": 3,
      "DisposableEmailBlockEnabled": true
    }
  }
}
```

> Stripe keys are wired into configuration but the subscription/billing integration is still in progress (`HookSentry.Subscriptions` is a stub).

## Database migrations

Billing tables are provisioned by `BillingDatabaseMigrator` on startup, from SQL under `src/HookSentry.Billing/Persistence/Migrations/` (`plans`, `tenant_cloud_states`, `usage_snapshots`, event index). Core tables are migrated by the OSS core.

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10 |
| ORM | NHibernate + Npgsql |
| Database | PostgreSQL |
| Message queue | RabbitMQ 3 (management) |
| Cache & quota counters | Redis 7 (StackExchange.Redis) |
| Auth | JWT Bearer + API Key, cloud RBAC policies |
| Observability | OpenTelemetry → Loki + Tempo + Grafana |
| API docs | Swagger / OpenAPI |
| Payments | Stripe *(in progress)* |

## License

Licensed under the [Apache License 2.0](hooksentry/LICENSE).
</content>
</invoke>
