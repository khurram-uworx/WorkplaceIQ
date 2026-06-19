# Agent Notes

This repo is still early and in flux. Keep changes small, sequential, and easy to verify.

## Local App

- Use the deterministic demo URL: `http://localhost:4792`.
- Launch the web app with:

```powershell
dotnet run --project src\WorkplaceIQ.Web\WorkplaceIQ.Web.csproj
```

- Do not invent or rotate ports unless `4792` is genuinely unavailable.
- For smoke checks, give the app enough time to finish seeding and binding before requesting the page.
- In this environment, `dotnet run --no-restore --project src\WorkplaceIQ.Web\WorkplaceIQ.Web.csproj` was the reliable way to keep the app up after restore/build had already succeeded.
- If you need a background local run for smoke testing, redirect stdout and stderr to files so the process stays alive long enough to serve requests. Reliable pattern:

  ```powershell
  # Start in background (does NOT block)
  $p = Start-Process -FilePath "dotnet" -ArgumentList "run --no-build --project src\WorkplaceIQ.Web\WorkplaceIQ.Web.csproj" -WindowStyle Hidden -RedirectStandardOutput "$env:TEMP\wiq-smoke.log" -RedirectStandardError "$env:TEMP\wiq-smoke-err.log" -PassThru
  Write-Host "PID: $($p.Id)"

  # Wait for startup, then test
  Start-Sleep -Seconds 10
  Invoke-WebRequest -Uri "http://localhost:4792/" -UseBasicParsing

  # Clean up
  Stop-Process -Id $p.Id -Force
  ```

## Build And Test

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

The repo may use the latest installed .NET SDK, including preview SDKs, while projects still target `net10.0`.

## Shell Tools

- Linux-style command-line tools are available directly from PowerShell and can be used when they make debugging faster, including `rg`, `grep`, `awk`, `sed`, and related coreutils.
- Do not wrap commands in `bash`; WSL may not have a distro installed. Call the available tools directly.

## Current Shape

- `src\WorkplaceIQ` owns core entities and service contracts.
- `src\WorkplaceIQ.AspNet` owns Tag Helpers, DbContext, and ASP.NET integration.
- `src\WorkplaceIQ.Web` is the SQLite-backed demo/reference app.
- Public Tag Helper prefix is `iq-`, for example `<iq-feed id="CompanyNews" title="News Feed" />`.
- Keep tests split by behavior instead of adding everything to one large file.
