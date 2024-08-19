# Leeds Experiments

Experiments related to DLIP project.

## Projects

* `Dashboard` - UI that uses our API to interact with Fedora (via below APIs)
* `DLCS` - DLCS client 
* `Fedora` - Fedora client/wrapper
* `Storage` - Models/helpers/utils. Knows about Fedora and our concepts but higher level?
* `StorageApiClient` - API client for Storage.API
* `SamplesWorker`
* `Storage.API` - _was_ `Preservation.API` but renamed. Wrapper around Fedora API
* `Preservation.API` - higher level API. Sits infront of `Storage.API` and used by client applications for API interactions

## Building Images

```bash
cd .\LeedsExperiment\
docker build -t dlip-storage:local -f .\Storage.API\Dockerfile .
docker build -t dlip-preservation:local -f .\Preservation.API\Dockerfile .
docker build -t dlip-dash:local -f .\Dashboard\Dockerfile .
```

## Running Locally

### Database

Running `docker-compose.local.yml` will run any external dependencies required for local development (currently just a Postgres instance).

> The Preservation.API projects default appSettings will use connection string for this local postgres instance + migrations are auto-ran.

```bash
docker compose -f docker-compose.local.yml up
```

#### Migrations

Migrations can be added with:
```bash
cd .\LeedsExperiment\

dotnet ef migrations add "{migration-name}" -p Preservation.API -o Data/Migrations