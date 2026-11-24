# Borderland Dreams

This repository is rebuilding the Dreams platform for The Borderland festival. See [README.md](README.md) for domain terminology and development setup.

- [Legacy Dreams platform (Cobudget)](https://dreams.theborderland.se/)
- [New test deployment](http://bl-dreams.runasp.net/)
- [GitHub repository](https://github.com/bob-box/dreams-platform)

## Track issue work locally

- Before working on an issue, copy its full description verbatim into the gitignored `plans/` directory.
- Keep an implementation checklist in `plans/` and check off each task as you complete it.
- Always include a test plan in `plans/`. List automated and manual checks as applicable, check off each test after it passes, and record any blocked or skipped checks.

## Review legacy behavior before every new feature

1. Before implementing a feature, review the relevant workflow on the legacy platform. Check how users reach it, who can act, what rules and deadlines apply, and what happens after each action. Check the affected roles and edge cases where relevant.
2. Compare the legacy workflow with the current code and, when accessible, the new test deployment. Use observed legacy behavior as the default for equivalent functionality and follow explicitly requested changes. Use read-only checks on the live legacy platform.
3. If a workflow is inaccessible, review available legacy source or documentation and record what remains unverified. If there is no legacy equivalent, record what you checked and the assumptions you used.
4. In the change summary or PR, record the legacy workflow and evidence reviewed, the behavior carried over, and any intentional differences or gaps in verification.

## Coding standards

- Use the existing .NET 10 ASP.NET Core MVC structure. Keep request handling in controllers, business rules in services, and rendering in Razor views. Reuse view models, mappers, shared Razor components, and assets in `wwwroot`. Follow the existing Identity Razor Pages structure for account flows.
- Match neighboring C# conventions for naming, namespaces, formatting, and dependency injection. Use nullable annotations, await database and network I/O, and register new dependencies in `Program.cs` when needed. Keep diffs focused and reuse existing services before adding abstractions.
- Enforce Cloud scope, membership requirements, and role or ownership rules server-side using the existing authorization patterns. Check Main Dreamer, Co-dreamer, and admin permissions where relevant. Reuse HTML sanitization when rendering user-authored rich text.
- Use `IAppTime` for deadlines and scheduling. Preserve consistency between wallets, funding allocations, dream statuses, and funding rounds on retries or partial failures. Follow the existing Hangfire patterns for scheduled work.
- Use EF Core through `ApplicationDbContext`. For schema changes, include the migration and model snapshot under `Borderland.Dreams.Web/Data/Migrations` and check the upgrade against existing data. Keep credentials and local database files out of source control.

## Verify changes

From the repository root with the .NET 10 SDK installed:

```sh
dotnet build Borderland.Dreams.sln
dotnet test Borderland.Dreams.sln
dotnet run --project Borderland.Dreams.Web
```

Run the app for changes that affect a user workflow. Exercise the affected roles, Cloud boundaries, and deadline or funding transitions as applicable, using local test data. Add focused xUnit coverage in `Borderland.Dreams.Tests` when existing tests miss a regression risk introduced by the change. Report any checks you could not run.
