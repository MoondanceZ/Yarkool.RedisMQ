# Repository Guidelines

## Project Structure & Module Organization

This repository is a .NET 8 Redis Stream message queue library with sample hosts and an embedded dashboard.

- `Yarkool.RedisMQ/`: core library, including publishers, consumers, Redis queue configuration, dashboard endpoints, serializers, and shared message models.
- `Yarkool.RedisMQ/Dashboard/wwwroot/`: Vue 3 + Vite dashboard source. The built `dist/` output is embedded as a library resource.
- `RedisMQ.Api/`: ASP.NET Core sample API for local testing and dashboard integration.
- `RedisMQ.Console/`: console sample consumer/publisher host.
- `Yarkool.RedisMQ.Test/`: NUnit test project.
- `README.md`: public usage examples and package-level documentation.

## Build, Test, and Development Commands

Run commands from the repository root unless noted.

- `dotnet restore Yarkool.RedisMQ.sln`: restore .NET dependencies.
- `dotnet build Yarkool.RedisMQ.sln`: build the library, samples, and tests.
- `dotnet test Yarkool.RedisMQ.sln`: run NUnit tests.
- `dotnet run --project RedisMQ.Api`: start the sample API. Tests and samples expect Redis at `127.0.0.1:6379`.
- `cd Yarkool.RedisMQ/Dashboard/wwwroot && pnpm install`: install dashboard dependencies.
- `pnpm dev`: run the dashboard Vite server.
- `pnpm build`: type-check and build dashboard assets into `dist/`.
- `pnpm lint`: run ESLint with auto-fix for dashboard code.

## Coding Style & Naming Conventions

C# style is governed by `.editorconfig`: 4-space indentation, CRLF line endings, braces on new lines, explicit types preferred over `var`, `System` usings first, interfaces prefixed with `I`, PascalCase for public types/members, camelCase for parameters/locals, and `_camelCase` for private/internal fields. Keep nullable annotations enabled and preserve implicit usings.

For dashboard code, use Vue single-file components, TypeScript, Pinia stores, Vuetify components, and the existing `src/apis`, `src/hooks`, `src/pages`, and `src/plugins` organization.

## Testing Guidelines

Tests use NUnit with `Microsoft.NET.Test.Sdk`, `NUnit3TestAdapter`, and `coverlet.collector`. Place new tests under `Yarkool.RedisMQ.Test/`; name test classes and methods after the behavior being verified rather than implementation details. Be explicit when a test requires a local Redis instance or a specific database.

## Commit & Pull Request Guidelines

Recent commits follow Conventional Commit prefixes such as `feat(...)`, `fix:`, `perf:`, and `refactor(...)`, often with concise Chinese descriptions. Keep commits focused, for example `fix: 修复延迟队列重发条件`.

Pull requests should explain the behavior change, list verification commands (`dotnet test`, `pnpm build`, etc.), mention Redis/configuration requirements, and include screenshots or short recordings for dashboard UI changes.

## Security & Configuration Tips

Do not commit real Redis passwords or production connection strings. Keep sample settings in `appsettings.Development.json` clearly non-production. When changing dashboard assets, rebuild `wwwroot/dist` before packaging the library so embedded resources match the source.
