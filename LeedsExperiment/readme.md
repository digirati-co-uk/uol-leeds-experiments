# Leeds Experiments

Experiments related to DLIP project.

## Projects

* `Dashboard` - UI that uses our API to interact with Fedora (via below APIs)
* `DLCS` - DLCS client 
* `Fedora` - Fedora client/wrapper
* `Preservation` - Models/helpers/utils. Knows about Fedora and our concepts but higher level?
* `PreservationApiClient` - API client for Storage.API (*lkely renamed in future*)
* SamplesWorker
* `Storage.API` - _was_ `Preservation.API` but renamed. Wrapper around Fedora API
* `Preservation.API` - higher level API. Sits infront of `Storage.API` and used by client applications for API interactions

## Building Image

```bash
cd .\LeedsExperiment\
docker build -t dlip-storage:local -f .\Storage.API\Dockerfile .
docker build -t dlip-preservation:local -f .\Preservation.API\Dockerfile .
docker build -t dlip-dash:local -f .\Dashboard\Dockerfile .
```