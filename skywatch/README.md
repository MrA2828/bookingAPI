# SkyWatch API

ASP.NET Core API that will aggregate aviation data (airports, airlines, live aircraft). All endpoints
except `/auth/*` require a bearer token.

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

## Debugging

```bash
docker compose ps -a
docker compose exec skywatch-db psql -U skywatch -d skywatch -c '\dt'
docker compose up -d --build skywatch      # rebuild after code changes
netstat -ano | findstr :8080               # what holds the port (Windows)
```
