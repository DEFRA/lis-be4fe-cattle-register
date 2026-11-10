# lis-be4fe-cattle-register

Part of the Livestock Information Service (LIS), which lets livestock keepers, owners, agents and
delegates record and manage their animals.

This is the backend-for-frontend (BE4FE) API for the cattle register microsite. It gives the
`lis-apps-cattle-register` spoke one API for building up a birth submission and sending it to
`lis-api-cattle`. It is a .NET 10 minimal API that keeps submissions in MongoDB.

Deployed on the Core Delivery Platform (CDP). See the
[CDP portal service page](https://portal.cdp-int.defra.cloud/services/lis-be4fe-cattle-register) for
environments, deployments, logs and metrics.

## Architecture

```text
hub (front-office or back-office) → lis-apps-cattle-register → this API → lis-api-cattle → KRDS and CADS
```

- Callers: intended for `lis-apps-cattle-register` only, which does not call it yet. This is an internal CDP
  service with no sign-in of its own.
- Authentication: every `/submission` and `/data` endpoint needs an `x-api-key` header that matches one of the keys in
  `ApiKeyAuthentication__Keys__N`, or it returns `401`. The spoke is expected to send its key as `CATTLE_REGISTER_API_KEY`.
  `/health`, the OpenAPI document and Scalar are anonymous. The endpoints use the `ServiceToService`
  authorisation policy, so another scheme can be added to that policy later. Every `/submission` and `/data` request also
  needs an `x-cdp-request-id` header (see [Correlation ID](#correlation-id)).
- Downstream calls: `lis-api-cattle`, through the client in `src/Integrations/CattleApi`, for the dam and sire lists.
  The transfer of submissions and the unallocated eartags are not built yet. This API does not send or check an AWS STS
  service token.
- Data store: MongoDB (database `lis-be4fe-cattle-register`) holds submissions in the `submissions` collection. Each one has a GUID as its
  document id, which stays internal, and a hash id (`XXX-XXX-XXX`, Crockford base32) that is the only id the API uses.

See [HUB-ARCHITECTURE.md](https://github.com/DEFRA/lis-dev/blob/main/HUB-ARCHITECTURE.md) for the hub and spoke design.

### Structure

- `src/Api` is the application: endpoints, authentication, middleware and services
- `src/Integrations/CattleApi` is the client for `lis-api-cattle`
- `src/Models` holds the shared response models
- `tests/Api.Tests` holds the unit and endpoint tests
- `tests/Endpoints` holds `.http` requests for every `/submission` endpoint (see [Running tests](#running-tests))
- `compose.yml` starts MongoDB and this API on their own
- `build.cake` and `base.cake` are the Cake build scripts

### Endpoints

Paths under `/submission` and `/data` need the `x-api-key` and `x-cdp-request-id` headers.

| Method and path | Purpose |
|---|---|
| `GET /health` | Health check |
| `GET /openapi/v1.json` | OpenAPI v1 document (`/openapi` redirects to it) |
| `GET /scalar/v1` | Interactive Scalar API reference |
| `GET /submission/start?cph=` | The CPH's draft submission id and summary, creating a draft if it has none |
| `GET /submission/{submissionId}/{animalId}/{section}` | One section of an animal's data, with an empty `data` array if none is stored. `404` if the submission is unknown |
| `POST /submission/{submissionId}:update` | Saves one section of an animal's data and, if `status` is given, sets the animal's status (`DRAFT` or `COMPLETE`). `400` if the payload is invalid, `404` if the submission is unknown, `409` if it was already submitted or changed by another request |
| `DELETE /submission/{submissionId}/{animalId}` | Removes an animal and all of its sections from a draft. `404` if the submission or animal is unknown, `409` if it was already submitted or changed by another request |
| `POST /submission/{submissionId}:submit` | Transfers a draft to the cattle API and marks it `SUBMITTED`. `400` if it has no animals, `404` if unknown, `409` if already submitted, `503` if the transfer fails |
| `GET /submission?cph=` | The CPH's submissions, newest first, each with the `animal-id` and `status` of its animals |
| `GET /data/eartags/unallocated?cph=&submission-id=` | Eartags issued to the holding that are not yet on an animal. With `submission-id`, tags its animals already use are left out. `404` if the holding or submission is unknown, `400` if the submission is for another CPH. Always `503` until the cattle API endpoint exists |
| `GET /data/eartags/dams?cph=` | Female animals on the holding, with breed and date of birth, from the cattle API |
| `GET /data/eartags/sires?cph=` | Male animals on the holding, with breed and date of birth, from the cattle API |

Errors use problem details.

The `/submission` endpoints follow the BFF framework. Submissions are stored in the MongoDB `submissions` collection. The
transfer to the cattle API is not built yet, so `POST /submission/{submissionId}:submit` currently always returns `503` and the
submission stays a draft.

### Correlation ID

Every `/submission` endpoint needs a single `x-cdp-request-id` header, as set out in the
[Correlation ID standard](https://eaflood.atlassian.net/wiki/spaces/LDD/pages/6438093586/Correlation+ID+standard).
Without it the request is rejected with `400` and `"code": "missing_header"` before authentication runs.
The value is returned on the response and as `correlationId` in error problem details, written as `CorrelationId`
on every log line, and sent on to `lis-api-cattle`. When calling the API by hand, send your own, for example
`-H 'x-cdp-request-id: local-test-1'`.

## Prerequisites

- The .NET SDK named in `global.json` (10.0.0 or later; check with `dotnet --version`)
- Docker with Docker Compose
- A local clone of [lis-dev](https://github.com/DEFRA/lis-dev) (recommended, see [Running with lis-dev](#running-with-lis-dev-recommended))

## Setup

### Development

```bash
dotnet restore be4fe-cattle-register.slnx
dotnet build be4fe-cattle-register.slnx
```

There is no `.env` file. Configuration comes from `appsettings.json`, `appsettings.<Environment>.json` and
environment variables, in that order (see [Configuration](#configuration)). The `Development` launch profile in
`src/Api/Properties/launchSettings.json` sets the values needed to run on the host.

### Test

There are no tools to install beyond the .NET SDK. Tests use xUnit and need no Docker or database.

## Running with lis-dev (recommended)

[lis-dev](https://github.com/DEFRA/lis-dev) runs the wider stack locally with Docker Compose and serves it behind
local HTTPS hostnames. Follow its README for the one-time setup.

Start the stack:

```bash
cd lis-dev
docker compose up -d
```

In compose this API is the `lis-be4fe-cattle-register` service, built from this repo and published on port 8085. That
service does not yet set `CattleApi__BaseUrl`, `CattleApi__ApiKey` or `ApiKeyAuthentication__Keys__0`, so until it does
only `/health` works there: `/submission` requests are rejected with `401`.

To work on this API with the rest of the stack still in Docker, stop its container and run it on the host on the same
port:

```bash
cd lis-dev
docker compose stop lis-be4fe-cattle-register

cd be4fe/cattle-register
ASPNETCORE_URLS=http://localhost:8085 \
Mongo__DatabaseUri=mongodb://127.0.0.1:27017/ \
CattleApi__BaseUrl=http://localhost:8091 \
CattleApi__ApiKey=local-dev-cattle-api-key \
ApiKeyAuthentication__Keys__0=local-dev-cattle-register-api-key \
dotnet run --project src/Api --no-launch-profile
```

MongoDB and `lis-api-cattle` publish ports 27017 and 8091 on the host. Use `dotnet watch` in place of `dotnet run` to
restart on changes. When done, press Ctrl+C and run `docker compose start lis-be4fe-cattle-register`.

## Running standalone

You can also run the API on its own with the `Development` launch profile:

```bash
docker compose up -d mongodb
dotnet run --project src/Api --launch-profile Development   # http://localhost:8085
```

`/health` and `/scalar/v1` work without any other service, because the cattle API options are only checked when
used. The `/submission` endpoints need MongoDB on `127.0.0.1:27017`.

To run this API and MongoDB together in containers, without lis-dev:

```bash
docker compose up --build
```

This publishes the API on <http://localhost:3226> and expects `lis-api-cattle` on `host.docker.internal:5019`.

### Configuration

Settings are in `src/Api/appsettings.json`, with `appsettings.Development.json` for local use. Each setting can be
overridden by an environment variable, with `__` in place of `:`. Values marked secret must be set as secrets in
the CDP portal for every deployed environment.

| Variable | Description | Required | Default |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Selects `appsettings.<Environment>.json` | no | `Production` |
| `ASPNETCORE_URLS` | Address and port to bind. The container sets `http://+:8085` | no | .NET default |
| `Mongo__DatabaseUri` | MongoDB connection string. CDP replaces the placeholder with the platform's AWS-authenticated URI | yes | `mongodb://set-automatically-when-deployed/admin?authSource=$external&authMechanism=MONGODB-AWS` (`mongodb://127.0.0.1:27017` in Development) |
| `Mongo__DatabaseName` | MongoDB database name | yes | `lis-be4fe-cattle-register` |
| `CattleApi__BaseUrl` | Base URL of `lis-api-cattle`. CDP sets it per environment | for the dam and sire lists | empty (`http://localhost:5019` in Development) |
| `CattleApi__ApiKey` | Secret. Key sent to `lis-api-cattle` as `x-api-key`. Must match one of that API's `ApiKeyAuthentication__Keys__N` | for the dam and sire lists | empty |
| `ApiKeyAuthentication__Keys__0` | Secret. Key callers must send as `x-api-key`. Must equal `CATTLE_REGISTER_API_KEY` in `lis-apps-cattle-register` | yes | none |
| `ApiKeyAuthentication__Keys__1` | Secret. Optional second key, so a key can be rotated without downtime | no | none |
| `ApiKeyAuthentication__HeaderName` | Header that carries the key | no | `x-api-key` |
| `TraceHeader` | Header copied from the request to downstream calls | no | `x-cdp-request-id` |
| `AllowedHosts` | ASP.NET allowed hosts | no | `*` |
| `Serilog__MinimumLevel__Default` | Log level | no | `Information` |
| `HTTP_PROXY`, `HTTPS_PROXY`, `NO_PROXY` | Outbound proxy settings, read by the default .NET handler | no | none |
| `TRUSTSTORE_*` | CDP-supplied certificates, loaded into the trust store at start-up | no | none |

With no inbound key configured, the API starts and answers `/health` but rejects every `/submission` request. With no
`CattleApi__BaseUrl` or `CattleApi__ApiKey`, it also starts, as those are only checked when the dam or sire list is requested.
On CDP, logs go to the console in Elastic Common Schema format. Request paths are logged as the route template
(for example `/submission/{submissionId}/{animalId}/{section}`), never the raw path, because the path can carry identifiers. The
sources that log raw URLs (ASP.NET Core hosting, `HttpClient` and the SDK REST client) are held at `Warning`
whatever the configuration says.

## Running tests

```bash
dotnet test be4fe-cattle-register.slnx      # unit and endpoint tests
dotnet format be4fe-cattle-register.slnx    # format the code; the build also runs StyleCop and Sonar analysers
```

`tests/Api.Tests` covers authentication, the correlation ID check, error handling, logging, the cattle API client,
caching and every endpoint. The endpoint tests run in memory with the submission store and transfer replaced, so they
need no Docker or database. The MongoDB submission repository itself is not covered by them.

### Endpoint requests (`tests/Endpoints`)

`tests/Endpoints` holds JetBrains HTTP client requests, with asserts, for every `/submission` endpoint, grouped by area
(`birth`, `health`). `security/Security.http` checks that a missing or wrong `x-api-key` gives
`401` and that a missing `x-cdp-request-id` gives `400 missing_header`. Run them from the IDE or with `ijhttp`:

```bash
cd tests/Endpoints
ijhttp --env-file http-client.env.json --env local health/Health.http birth/Birth.http security/Security.http
```

- `local` expects this API on 8085, the port of the `Development` launch profile, and MongoDB on 27017.

End-to-end journeys are covered by [lis-journey-tests](https://github.com/DEFRA/lis-journey-tests).

## Monitoring

- Health check: `GET /health` returns `{"status":"Healthy"}`. It shows the API is running and does not call MongoDB or `lis-api-cattle`.
- Logs: OpenSearch dashboard per environment, linked from the [CDP portal service page](https://portal.cdp-int.defra.cloud/services/lis-be4fe-cattle-register) under Logs
- Metrics: Grafana dashboard per environment, linked from the [CDP portal service page](https://portal.cdp-int.defra.cloud/services/lis-be4fe-cattle-register) under Metrics

## Pipelines

Build, versioning and publishing run in GitHub Actions (`.github/workflows`) using Cake scripts (`*.cake`):

- **Pull request to `main`**: format, build and test, Sonar analysis, version check
- **Push to a `feature/` or `hotfix/` branch**: prerelease build and tag
- **Merge to `main`**: release tag, then publish the container image to CDP

Versions come from git tags (SemVer). The `Dockerfile` runs the tests and publishes the API.

## Branching policy

The repo uses mainline development. Branch from `main` and name the branch:

- `feature/LREG-<ticket>-<short-kebab-description>`
- `hotfix/LREG-<ticket>-<short-kebab-description>`

`LREG` must be uppercase, because the GitHub ruleset rejects other branch names. Rebase onto `main`
rather than merging `main` in. Incomplete work goes behind a feature flag.

## Contributing to this project

- Follow the [Defra software development standards](https://github.com/DEFRA/software-development-standards).
- Before you push, run `dotnet format be4fe-cattle-register.slnx` and `dotnet test be4fe-cattle-register.slnx`. Fix StyleCop and Sonar warnings rather than suppress them.
- Open a pull request against `main` using the PR template. CI must pass and the Sonar quality gate must be green before merge.

## Licence

THIS INFORMATION IS LICENSED UNDER THE CONDITIONS OF THE OPEN GOVERNMENT LICENCE found at:

<http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3>

The following attribution statement MUST be cited in your products and applications when using this information.

> Contains public sector information licensed under the Open Government licence v3

### About the licence

The Open Government Licence (OGL) was developed by the Controller of His Majesty's Stationery Office (HMSO) to enable information providers in the public sector to license the use and re-use of their information under a common open licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.
