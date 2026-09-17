# AGENTS.md

Guidance for AI coding agents working in this repository. Humans are welcome to read it too.

## What this repository is

`PTrampert.ApiProxy` is a **published NuGet library**, not an application. It adds a proxy route to
an ASP.NET Core app: `AddApiProxy(...)` registers the services, `UseApiProxy("basePath")` maps the
route, and requests to `{basePath}/{api}/{*path}` are forwarded to the `BaseUrl` configured for that
`api` name. WebSocket requests are forwarded to `WsBaseUrl` instead.

Because it is a library, **the public API surface is the product**. Anything `public` in the
`PTrampert.ApiProxy` project is a consumer-facing contract, and changing or removing it is a
breaking change (see *Versioning* below). Prefer adding to the surface over reshaping it.

## Layout

| Path | What it is |
| --- | --- |
| `PTrampert.ApiProxy/` | The library. The only project that ships. |
| `PTrampert.ApiProxy.Test/` | NUnit tests for the library. |
| `PTrampert.ApiProxy.SampleApp/` | Demo ASP.NET Core host + Create React App front end that exercises the proxy. |
| `PTrampert.ApiProxy.SampleApp.Api/` | Demo downstream API that the sample app proxies to. |
| `.github/workflows/` | Thin wrappers that call reusable workflows in `PaulTrampert/github-workflows`. |

Key types in the library:

- `ApiProxyController` — the proxy itself. `public` only so ASP.NET Core's controller discovery can
  find it; consumers should never call it directly.
- `ApiProxyConfig` / `ApiConfig` — the configuration model bound from `IConfiguration`.
  `ApiProxyConfig` is a `Dictionary<string, ApiConfig>` with `OrdinalIgnoreCase` keys.
- `IAuthentication` / `IAuthenticationFactory` — pluggable upstream authentication.
  `DefaultAuthenticationFactory` resolves `ApiConfig.AuthType` by name via `Type.GetType`, constructs
  it through `ActivatorUtilities` (so constructor injection works), then assigns public settable
  properties from `ApiConfig.AuthProps`.
- `BasicAuthentication`, `UserBearerAuthentication` — the two built-in `IAuthentication` implementations.
- `IWebSocketProxy` / `WebSocketProxy` — bidirectional WebSocket pumping.
- `ApiProxyConfigValidator` — an `IValidateOptions<ApiProxyConfig>`, validated on start, that rejects
  reserved headers (content headers, and `Authorization` on requests) in an api's `RequestHeaders` /
  `ResponseHeaders`. The reserved names are documented in `README.md`.

## Build and test

```bash
dotnet build PTrampert.ApiProxy.sln
dotnet test PTrampert.ApiProxy.Test/PTrampert.ApiProxy.Test.csproj -f net10.0
```

Notes:

- The library and the test project **multi-target `net8.0` and `net10.0`**. `global.json` pins SDK
  `10.0.100` with `rollForward: latestFeature`.
- Plain `dotnet test` runs both target frameworks and will abort the `net8.0` run on a machine that
  has only the .NET 10 runtime installed. Pass `-f net10.0` locally; CI installs both and runs the
  whole matrix, so do not remove `net8.0` from `TargetFrameworks` to make a local run quieter.
- Tests collect coverage via coverlet and write `coverage.net*.cobertura.xml` into the test project.
  That file is gitignored — never commit it.
- `dotnet restore` emits an `NU1902` warning for a transitive `Microsoft.Build.Tasks.Git` advisory.
  It is pre-existing and not caused by your change.

To exercise the proxy end to end, `docker compose up` builds and runs the sample app (on
`localhost:8080`) together with the downstream sample API.

## Code conventions

Match the file you are editing rather than imposing a global style.

- Four-space indent. `var` for locals wherever the type is evident.
- Private fields are `camelCase` with **no** leading underscore, and are assigned through `this.x = x`
  in constructors when the parameter shares the name.
- Namespaces: newer files (`WebSocketProxy.cs`, `IWebSocketProxy.cs`) use file-scoped namespaces;
  older ones use block-scoped. Leave existing files as they are.
- Extension methods for `IServiceCollection` / `IApplicationBuilder` deliberately live in the
  `Microsoft.Extensions.DependencyInjection` and `Microsoft.AspNetCore.Builder` namespaces so they
  surface without an extra `using`. The `// ReSharper disable once CheckNamespace` comments above
  them are intentional — keep them.
- **XML doc comments are mandatory on public members.** `GenerateDocumentationFile` is on, so a
  missing `<summary>` is a build warning, and the docs ship in the package.
- Errors the client should see are `ProxyException(message, status)`. Note that nothing in the
  library installs an exception handler for it — the consuming app is expected to map it — so any
  other exception type escaping the controller becomes an unhandled 500.

### Header forwarding

Only headers named in `ApiConfig.RequestHeaders` / `ResponseHeaders` are forwarded. Matching against
the configured list is case-insensitive, but the header is forwarded **using the spelling the client
sent**, because an upstream API may not treat header names case-insensitively whatever the spec says.
Preserve that behaviour when touching `ApiProxyController.MakeRequest`.

Headers the proxy handles itself cannot be forwarded at all, and `ApiProxyConfigValidator` rejects
them at startup rather than letting a request fail. Adding a name to that reserved list is a
behaviour change for consumers whose configuration already lists it, so it needs a `README.md` update
too.

## Test conventions

NUnit 4 with Moq. Follow the shape already in `PTrampert.ApiProxy.Test`:

- The class under test is a field named `subject`, built in a `[SetUp]` method.
- Test method names read as sentences starting with `It`, e.g. `ItThrowsProxyExceptionWhenApiNotConfigured`.
- Constraint-model assertions only: `Assert.That(actual, Is.EqualTo(expected))`.
- HTTP is faked with `TestHttpHandler` (records the last request, returns a canned `NextResponse`)
  rather than by mocking `HttpClient`.
- `InternalsVisibleTo` is granted to the test assembly and to `DynamicProxyGenAssembly2` (for Moq),
  so internal types such as `WebSocketProxy` can be tested and mocked directly.

Any behaviour change to the library needs a test. Coverage currently sits around 83% of lines.

## Branching, PRs, and versioning

- Work in a **git worktree** on a dedicated branch off `main`; never commit to `main` directly.
  Existing branches use descriptive kebab-case names, often prefixed (`bugfix/header-passthrough`).
- **One small, self-contained PR per issue.** Prefer independent PRs branched from `main` over
  stacked ones. If a change needs shared groundwork, give the groundwork its own issue and PR that
  both consumers depend on.
- Findings outside the narrow scope of the PR you are working on get **filed as GitHub issues**, not
  folded into the PR. Reference the PR that surfaced them.
- **PR titles must start with `(MAJOR)`, `(MINOR)`, or `(PATCH)`.** The `check-pr-title` workflow
  enforces this and the release pipeline reads the prefix from the squashed commit message to compute
  the next semantic version, so the prefix is the version bump. Use `(MAJOR)` only for a genuine
  break in the public API, `(MINOR)` for additive surface, `(PATCH)` for fixes and dependency bumps.
- Merging to `main` runs the reusable `dotnet-library` workflow: calculate version, build, test, pack,
  and — if any non-test project changed since the last tag — publish to NuGet.org and tag the release.
  There is nothing to run by hand to cut a release.
