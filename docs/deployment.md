# Deployment

> **Status:** Phase 14. Filled in when Azure resources are provisioned.

## Target architecture

```
                    Internet
                       |
        +--------------+--------------+
        |                             |
        v                             v
 Azure Static Web Apps         Azure App Service
   (React SPA)          -->      (ASP.NET Core API)
                                       |
                          +------------+------------+
                          |                         |
                          v                         v
             Azure Database for PostgreSQL   Application Insights
                          ^
                          |
                    Azure Key Vault
                    (secrets: DB password, JWT key)
```

## Cost note

Every resource below has a free or low-cost tier. **Azure Database for PostgreSQL has no
permanently free tier** — it is the one resource that will incur charges beyond any free
trial credit. The Burstable **B1ms** SKU is the cheapest workable option and can be stopped
when not in use. This will be flagged explicitly before anything is provisioned.

| Resource | Suggested tier | Cost |
|---|---|---|
| Static Web Apps | Free | Free |
| App Service | F1 Free, or B1 for always-on | Free / low |
| PostgreSQL Flexible Server | Burstable B1ms | **Charges apply** |
| Application Insights | Pay-as-you-go | Free under the monthly data cap |
| Key Vault | Standard | Negligible (per-operation) |

## Configuration

No secret is ever committed or hardcoded in a workflow file.

| Where it runs | Secrets come from |
|---|---|
| Local development | .NET user secrets, `.env.local` |
| GitHub Actions | GitHub Secrets |
| Azure | App Service configuration, backed by Key Vault references |
