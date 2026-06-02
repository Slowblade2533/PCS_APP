# PCS_APP Hardening Tasks

Started: 2026-06-01
Status key: `[ ]` pending, `[~]` in progress, `[x]` done

- [x] P0: Fix login SQL query bug in `api/Repositories/UserRepository.cs`
- [x] P1: Require authorization for product APIs and protected auth endpoints
- [x] P2: Add CSRF protection for cookie-authenticated cross-origin requests
- [x] P3: Ensure exception handler is wired into ASP.NET middleware pipeline
- [x] P4: Unify frontend API base URL usage and CORS-compatible credentials behavior
- [x] P5: Make product create flow functional (submit enabled + POST to API)
- [x] P6: Remove unsafe `any` usage in core frontend data/service flow
- [x] P7: Move sensitive connection string out of committed appsettings defaults
- [x] P8: Prevent build artifacts from being tracked (`bin/`, `obj/`, `.vs/`)

## Progress Log

- 2026-06-01: Created task list and started P0.
- 2026-06-01: P0 done (`WHERE Email = @Email`).
- 2026-06-01: P1 done (added `[Authorize]` to product endpoints and protected auth endpoints).
- 2026-06-01: P2 done (added antiforgery token issuance + server validation + frontend XSRF interceptor).
- 2026-06-01: P3 done (enabled `app.UseExceptionHandler()`).
- 2026-06-01: P4 done (centralized API base URL; aligned product/auth service origins).
- 2026-06-01: P5 done (implemented product create POST flow, button state, error/loading UI).
- 2026-06-01: P6 done (removed `any` from edited frontend data flow files).
- 2026-06-01: P7 done (removed machine-specific connection string from default appsettings).
- 2026-06-01: P8 done (added `api/.gitignore` for build artifacts and local IDE files).
- 2026-06-01: Verification done (`dotnet build PCS_API.csproj` and `npm run build` both pass).
