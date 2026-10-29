# Booking API stack

Three ASP.NET Core APIs, each with its own Postgres database, started together from this folder.

| Service      | Folder            | API port | Swagger                          | Postgres (host port) | Database    |
|--------------|-------------------|----------|----------------------------------|----------------------|-------------|
| booking-api  | `booking-api/`    | 8080     | http://localhost:8080/swagger    | 5543                 | `bookingapi`|
| aerohub-mock | `AeroHub_Mock/`   | 8081     | http://localhost:8081/swagger    | 5544                 | `aerohub`   |
| skylink-mock | `skylink-mock/`   | 8082     | http://localhost:8082/swagger    | 5545                 | `skylink`   |

Credentials are `<db>` / `<db>_dev_pw` (for example `bookingapi` / `bookingapi_dev_pw`). Dev only.

## Docker (all 3 projects at once)

Run from this folder (`booking-api/`):

```bash
docker compose up -d --build     # build images and start everything
docker compose ps                # status
docker compose logs -f           # follow all logs
docker compose logs -f booking-api   # follow one service
docker compose restart skylink-mock  # restart one service
docker compose up -d --build booking-api   # rebuild and restart one service
docker compose down              # stop and remove containers (data kept)
docker compose down -v           # also delete all three databases
```

The first start takes a few extra seconds while each API creates its schema. Dev emails
(confirmation links, reset codes) are written to the container logs, so use `docker compose logs`.

## Debugging

```bash
# Logs and status
docker compose logs --tail 100 booking-api      # last 100 lines of one service
docker compose logs -f --since 5m               # all services, last 5 minutes, follow
docker compose ps -a                            # includes crashed/exited containers
docker compose top booking-api                  # processes inside a container
docker stats                                    # live CPU/memory per container

# Inspect a container
docker compose exec booking-api sh              # shell inside the API container
docker compose exec booking-api env | grep -i connection   # effective connection string
docker compose config                           # compose file with all values resolved
docker inspect booking-api-booking-api-1 --format '{{json .State.Health}}'

# Check the APIs
curl -i http://localhost:8080/openapi/v1.json   # 8081 for aerohub-mock, 8082 for skylink-mock

# Database access (psql inside the db container)
docker compose exec booking-db psql -U bookingapi -d bookingapi
docker compose exec booking-db psql -U bookingapi -d bookingapi -c '\dt'
docker compose exec booking-db psql -U bookingapi -d bookingapi -c 'select "UserName","Email","EmailConfirmed" from "AspNetUsers"'
# same for aerohub-db (-U aerohub -d aerohub) and skylink-db (-U skylink -d skylink)
# or from the host with any client: localhost:5543 / 5544 / 5545

# Find what is using a port (Windows PowerShell)
netstat -ano | findstr :8080

# Reset a single service from scratch
docker compose rm -sf booking-api booking-db && docker volume rm booking-api_booking-data
docker compose up -d --build booking-api

# Attach the debugger (Visual Studio / VS Code)
# Start only the database in Docker, then debug the project locally (F5 / "dotnet watch run"):
docker compose up -d booking-db
cd booking-api && dotnet watch run
```

Common problems:
- **Port already allocated**: another process holds the port. Find it with `netstat` as above and stop it.
- **API returns connection refused right after `up`**: the first start creates the schema; wait a few seconds and check `docker compose logs <service>`.
- **Confirmation email / reset code**: written to the logs as `[DEV EMAIL] ...`, so run `docker compose logs booking-api | grep "DEV EMAIL"`.
- **Changed code but nothing changed**: rebuild with `docker compose up -d --build <service>`.

## Running one API locally (without Docker for the API)

Start only its database, then run the project:

```bash
docker compose up -d booking-db          # or aerohub-db / skylink-db
cd ../booking-api && dotnet run          # http://localhost:8080
cd ../AeroHub_Mock && dotnet run         # http://localhost:8081
cd ../skylink-mock && dotnet run         # http://localhost:8082
```

Connection strings for local runs are in each project's `appsettings.json`
(`localhost` + the host ports above). Inside Docker they are overridden with the
`ConnectionStrings__Default` environment variable in `docker-compose.yml`.

## Build

```bash
dotnet build booking-api
dotnet build ../AeroHub_Mock
dotnet build ../skylink-mock
```
