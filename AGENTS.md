# Agent Notes

This repo is still early and in flux. Keep changes small, sequential, and easy to verify.

## Local App

- Use the deterministic demo URL: `http://localhost:4792`.
- Launch the web app with:

```powershell
dotnet run --project src\WorkplaceIQ.Web\WorkplaceIQ.Web.csproj
```

- Do not invent or rotate ports unless `4792` is genuinely unavailable.

## Build And Test

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

The repo may use the latest installed .NET SDK, including preview SDKs, while projects still target `net10.0`.

## Current Shape

- `src\WorkplaceIQ` owns core entities and service contracts.
- `src\WorkplaceIQ.AspNet` owns Tag Helpers, DbContext, and ASP.NET integration.
- `src\WorkplaceIQ.Web` is the SQLite-backed demo/reference app.
- Public Tag Helper prefix is `iq-`, for example `<iq-feed id="CompanyNews" title="News Feed" />`.
- Keep tests split by behavior instead of adding everything to one large file.
