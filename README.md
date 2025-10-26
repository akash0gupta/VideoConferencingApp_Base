# VideoConferencingApp

Lightweight, extensible .NET 9-based video conferencing backend + Blazor frontend.  
This repository contains the API, infrastructure, application and domain layers plus a Blazor/WebAssembly client for real-time video calls, signaling (SignalR), messaging, and email/notification support.

## Contents
- `VideoConferencingApp.Api` — ASP.NET Core API (SignalR hub, REST endpoints)
- `VideoConferencingApp.Client` — Blazor (Server or WebAssembly) frontend (may be named differently in the repo)
- `VideoConferencingApp.Infrastructure` — external integrations (SMTP, Redis, RabbitMQ, MimeKit, Serilog)
- `VideoConferencingApp.Application` — application services, CQRS/commands
- `VideoConferencingApp.Domain` — domain models and DTOs
- `*.Test` projects — unit/integration tests

## Key technologies
- .NET 9
- Blazor (priority for client)
- SignalR for real-time signaling
- RabbitMQ,Kafka (optional queueing)
- StackExchange.Redis and InMemory for caching/state
- MimeKit for e-mail composition
- Serilog for logging
- xUnit + Moq for tests

## Prerequisites
- .NET 9 SDK
- __Visual Studio 2022__ or newer (or `dotnet` CLI)
- Node.js (if front-end tooling is needed for WebAssembly builds)
- Optional: Docker, RabbitMQ, Redis, SMTP server (or a dev SMTP like MailHog)

## Quick start (local)

1. Clone the repo

2. Configure application settings
- Copy `appsettings.Development.json` (if present) or update `appsettings.json` in `VideoConferencingApp.Api`.
- Provide secrets via environment variables or `appsettings.Development.json`. Typical settings:
  - `Smtp:Host`, `Smtp:Port`, `Smtp:User`, `Smtp:Password`
  - `Redis:Configuration`
  - `RabbitMQ:HostName`, `RabbitMQ:UserName`, `RabbitMQ:Password`
  - `AllowedHosts`, `Logging` settings
- If the project supports `.env`, create a `.env` with the same variables for local `docker-compose` or local dev convenience.

3. Build and run (API)
- Using Visual Studio: open solution, set desired startup project(s) and run.
  - To run API and client together: set multiple startup projects via the solution properties (__Set Startup Projects__).
- Using dotnet CLI:
  ```bash
  dotnet build
  dotnet run --project VideoConferencingApp.Api
  ```
4. Run Blazor client (if separate)
- If WebAssembly, open the `wwwroot/index.html` served by the client or a development server.

5. Tests
- Ensure external services (Redis, RabbitMQ, SMTP) are available in the compose file or configured as services.

## Configuration notes
- Email attachments: DTOs such as `EmailAttachment` (in `VideoConferencingApp.Domain`) may include streams or byte arrays. When sending mail, ensure you dispose streams you open (see troubleshooting).
- Logging: Serilog sinks are configured in `VideoConferencingApp.Infrastructure`. Rotate/limit file sinks in production.

## Tests
- Unit tests are under `*.Test` projects and use `xUnit` and `Moq`. Use `dotnet test` to run.

## Troubleshooting & common gotchas
- Long-running background services: If you have a worker or hosted background service, derive from `BackgroundService` and properly respect cancellation tokens and dispose any `IDisposable` resources.
- Memory leaks from streams:
- If DTOs such as `EmailAttachment` expose `Stream` and are passed around, ensure ownership is clear. Prefer passing `byte[]` or creating streams transiently and disposing them after use. See suggested fix below.
- SMTP/Attachment best-practices:
- Convert `byte[]` to a `MemoryStream` within the mail-sending operation and wrap it in a `using` or use `await using` for async disposal.
- SignalR connection issues:
- Confirm CORS and transport configuration in `VideoConferencingApp.Api`.
- Tests failing due to missing config:
- Use an `appsettings.Test.json` or mock infrastructure dependencies in unit tests.

## Suggested fixes for common resource-management issues
- Avoid storing open `Stream` instances on DTOs that live beyond the sending operation. Example pattern:
- Keep `EmailAttachment` DTO with `byte[] FileBytes` and/or `string FileName` only.
- In mail-sender, create a `MemoryStream` from `FileBytes`, attach it, and dispose after sending.
- For background services implementing `IHostedService` or `BackgroundService`, always pass `CancellationToken` to asynchronous operations and call `Dispose` on `IDisposable` clients (e.g., RabbitMQ connections, Redis multiplexer) when stopping the host.

Example pattern when sending attachments (conceptual):
- Convert the DTO bytes to `using var ms = new MemoryStream(attachment.FileBytes);` then add to message and dispose `ms` after send.

## Contributing
- Fork, create a feature branch, add tests, and open a PR.
- Follow existing code style and prefer small, testable changes.

## License
- Check repository root for `LICENSE`. If none exists, add one (MIT recommended for open-source projects).

If you want, I can:
- Create a `README.md` file in the repository with this content.
- Add example `appsettings.Development.json` and a small code snippet showing safe disposal of `EmailAttachment` streams.