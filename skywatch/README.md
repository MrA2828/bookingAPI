# SkyWatch API

ASP.NET Core API that aggregates aviation data: reference data (airports, airlines) from open datasets,
and live aircraft from open ADS-B feeds. All endpoints except `/auth/*` require a bearer token.

| Service  | API port | Swagger                       | Postgres (host port) | Database   |
|----------|----------|-------------------------------|----------------------|------------|
| skywatch | 8080     | http://localhost:8080/swagger | 5543                 | `skywatch` |

Credentials are `skywatch` / `skywatch_dev_pw`. Dev only.

## Docker

```bash
docker compose up -d --build     # build and start the API + database
docker compose logs -f skywatch  # follow logs (dev emails are logged here as [DEV EMAIL])
docker compose down              # stop (data kept)
docker compose down -v           # also delete the database
```

## Run locally (database in Docker)

```bash
docker compose up -d skywatch-db
dotnet run                       # http://localhost:8080
```

## Reference data

Airports (OurAirports) and airlines (OpenFlights) are imported into Postgres by a background job:
on startup if the data is missing or stale, then every 24 hours (`ReferenceData` section in `appsettings.json`).
Records that disappear from a source are marked inactive, never deleted. Check the last run with
`GET /reference-data/status`.

## Tests

Run from this folder (`skywatch`). The test projects are siblings, hence the `../`.

```bash
dotnet test ../skywatch.Tests                                       # unit tests, no dependencies
docker compose up -d skywatch-db                                    # integration tests need Postgres on :5543
dotnet test ../skywatch.IntegrationTests --filter "Category!=Network"   # offline, uses a throw-away database
dotnet test ../skywatch.IntegrationTests --filter "Category=Network"    # hits the real ADS-B feeds
```

## Debugging

```bash
docker compose ps -a
docker compose exec skywatch-db psql -U skywatch -d skywatch -c '\dt'
docker compose exec skywatch-db psql -U skywatch -d skywatch -c 'select * from sync_states'
docker compose up -d --build skywatch      # rebuild after code changes
netstat -ano | findstr :8080               # what holds the port (Windows)
```
