# PCS_APP Memory Notes

Last updated: 2026-06-01

## Review Baseline

- Backend: ASP.NET Core (`api/`)
- Frontend: Angular (`webapp/`)
- Primary risks identified in prior review were authentication, CSRF, endpoint protection, and broken product create flow.

## Implementation Decisions

- Keep existing cookie-auth architecture.
- Add explicit authorization attributes on protected endpoints.
- Add antiforgery token flow that works with cross-origin frontend:
  - Server issues `XSRF-TOKEN` cookie.
  - Frontend sends `X-XSRF-TOKEN` header for mutating API requests.
- Keep changes scoped; avoid unrelated refactors.

## Progress Journal

- 2026-06-01: Created `task.md` and began implementing fixes from P0 upward.
- 2026-06-01: Fixed login SQL (`WHERE Email = @Email`) and removed unused hash-generation API surface.
- 2026-06-01: Added authorization hardening:
  - `[Authorize]` on `ProductsController`
  - `[Authorize]` on `AuthController` `me/logout`
  - explicit `AddAuthorization()` + `UseAuthentication()`
- 2026-06-01: Added CSRF protection for cookie auth:
  - Backend issues token at `GET /api/auth/csrf-token`
  - Backend validates antiforgery token on unsafe methods when auth cookie exists
  - Frontend adds `X-XSRF-TOKEN` via interceptor for API origin
- 2026-06-01: Wired global exception middleware with `app.UseExceptionHandler()`.
- 2026-06-01: Unified API base URL in frontend (`shared/config/api.config.ts`) and updated services.
- 2026-06-01: Implemented product create submit flow (actual POST, loading state, error message, redirect).
- 2026-06-01: Reduced frontend type looseness (removed `any` in edited core flow files).
- 2026-06-01: Removed machine-specific connection string from `appsettings.json`; moved generic dev value to `appsettings.Development.json`.
- 2026-06-01: Added `api/.gitignore` for `bin/`, `obj/`, `.vs/`, and user-local files.
- 2026-06-01: Full build verification passed:
  - `dotnet build PCS_API.csproj`
  - `npm run build`
