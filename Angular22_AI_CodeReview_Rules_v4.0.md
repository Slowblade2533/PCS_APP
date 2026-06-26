# Angular22_AI_CodeReview_Rules_v4.0

> Enterprise AI Review Specification Target: Angular v22+, Standalone,
> Zoneless, Signals, Tailwind CSS v4, DaisyUI v5

## Goals

This specification is intended to become the **single source of truth**
for AI code reviewers.

## Review Principles

-   Prefer correctness over cleverness.
-   Prefer official Angular guidance.
-   Prefer immutable state.
-   Prefer Signals over imperative state.
-   Prefer type safety over convenience.
-   Explain every finding with evidence.

------------------------------------------------------------------------

# Phase 1 --- Discovery

Collect and document:

-   Angular version
-   TypeScript version
-   Builder
-   Rendering mode (CSR / SSR / Hybrid)
-   Zoneless status
-   Signal adoption
-   Build configuration
-   Testing framework
-   Lint configuration
-   Formatting configuration
-   Folder architecture

------------------------------------------------------------------------

# Phase 2 --- Architecture Audit

Review:

-   Feature-first organization
-   Layer separation
-   Dependency direction
-   SOLID
-   DRY
-   KISS
-   YAGNI
-   Clean Architecture
-   CQRS suitability
-   Circular dependencies
-   Shared kernel
-   Domain boundaries
-   Technical debt

Scoring: 0--10

------------------------------------------------------------------------

# Phase 3 --- Angular Audit

## Modern APIs

Require evaluation of:

-   standalone
-   inject()
-   signal()
-   computed()
-   effect()
-   resource()
-   linkedSignal()
-   input()
-   output()
-   model()
-   loadComponent()
-   Functional Guards
-   Functional Interceptors
-   provideAppInitializer()

Flag legacy APIs and recommend modern replacements where appropriate.

------------------------------------------------------------------------

## Signals Deep Audit

Evaluate:

-   Writable vs readonly signals
-   Signal dependency graph
-   Computed purity
-   Effect lifecycle
-   Circular updates
-   Derived state
-   Signal granularity
-   State ownership
-   Resource lifecycle
-   Async cancellation

------------------------------------------------------------------------

## Zoneless Audit

Reject unnecessary reliance on:

-   ChangeDetectorRef
-   NgZone
-   ApplicationRef.tick()

Verify UI updates are signal-driven.

------------------------------------------------------------------------

# Phase 4 --- Component Audit

Evaluate:

-   SRP
-   File size
-   Template complexity
-   Business logic placement
-   Inputs / outputs
-   Host listeners
-   Accessibility
-   Performance
-   Reusability

------------------------------------------------------------------------

# Phase 5 --- Template Audit

Review:

-   @if
-   @for
-   @switch
-   @defer
-   track expressions
-   Template typing
-   Semantic HTML
-   Keyboard support
-   ARIA
-   Focus order

------------------------------------------------------------------------

# Phase 6 --- Service Audit

Evaluate:

-   Cohesion
-   Statelessness
-   API consistency
-   Error handling
-   Cache strategy
-   DTO mapping
-   Resource suitability

------------------------------------------------------------------------

# Phase 7 --- Performance Audit

Measure:

-   Bundle size
-   Lazy loading
-   Code splitting
-   Memoization
-   Cache growth
-   Rendering cost
-   Signal recomputation
-   Image loading
-   Core Web Vitals readiness

------------------------------------------------------------------------

# Phase 8 --- TypeScript Audit

Require:

-   strict
-   strictTemplates
-   strictInjectionParameters
-   noUncheckedIndexedAccess
-   exactOptionalPropertyTypes

Review:

-   any usage
-   unknown usage
-   non-null assertions
-   type narrowing
-   generics

------------------------------------------------------------------------

# Phase 9 --- Security Audit

OWASP-oriented review:

-   XSS
-   CSRF
-   Injection
-   Hardcoded secrets
-   Token storage
-   Dangerous HTML
-   Upload validation
-   Sensitive logging
-   CSP readiness

------------------------------------------------------------------------

# Phase 10 --- Accessibility Audit

Review against WCAG 2.2 AA:

-   Contrast
-   Keyboard navigation
-   Screen reader support
-   Labels
-   ARIA
-   Focus management
-   Semantic elements

------------------------------------------------------------------------

# Phase 11 --- Tailwind CSS v4 & DaisyUI v5 Audit

Review:

-   Utility consistency
-   Theme usage
-   Responsive design
-   Design tokens
-   Component consistency
-   CSS duplication
-   Avoid !important
-   Avoid inline styles

------------------------------------------------------------------------

# Phase 12 --- Code Smell Audit

Detect:

-   Long Method
-   Long Component
-   Long Service
-   Duplicate Code
-   Dead Code
-   Primitive Obsession
-   Feature Envy
-   Data Clumps
-   Shotgun Surgery
-   Temporary Field
-   Lazy Class
-   Middle Man
-   Speculative Generality

------------------------------------------------------------------------

# Phase 13 --- Complexity Audit

Measure:

-   Cyclomatic Complexity
-   Cognitive Complexity
-   Nesting depth
-   File length
-   Function length
-   Class length

------------------------------------------------------------------------

# Phase 14 --- Dependency Audit

Review:

-   Deprecated packages
-   Duplicate packages
-   Bundle impact
-   Unused imports
-   Unused dependencies
-   Version compatibility

------------------------------------------------------------------------

# Phase 15 --- Testing Audit

Review:

-   Vitest
-   Component tests
-   Service tests
-   Signal tests
-   Mockability
-   Isolation
-   Coverage

------------------------------------------------------------------------

# Phase 16 --- Production Readiness

Evaluate:

-   angular.json
-   package.json
-   tsconfig
-   ESLint
-   Prettier
-   Environment strategy
-   Feature flags
-   SSR readiness
-   Hydration readiness
-   Monitoring hooks
-   Logging
-   Build budgets

------------------------------------------------------------------------

# AI Reviewer Constraints

Always:

-   Explain WHY.
-   Explain IMPACT.
-   Explain ROOT CAUSE.
-   Recommend the modern Angular solution.
-   Include a minimal example.
-   Prioritize findings.

Never:

-   Guess.
-   Recommend deprecated APIs.
-   Recommend NgModule for new Angular applications.
-   Recommend Zone.js where Signals are appropriate.
-   Recommend any without justification.

------------------------------------------------------------------------

# Required Finding Schema

Every finding must contain:

-   Severity
-   Category
-   Rule
-   File
-   Line
-   Problem
-   Root Cause
-   Impact
-   Recommendation
-   Example
-   Estimated Fix Effort
-   Confidence (High / Medium / Low)

------------------------------------------------------------------------

# Severity

-   Critical
-   High
-   Medium
-   Low
-   Suggestion

------------------------------------------------------------------------

# Final Report

Include:

1.  Executive Summary
2.  Architecture Score
3.  Angular Modern Score
4.  Performance Score
5.  Security Score
6.  Accessibility Score
7.  Maintainability Score
8.  Type Safety Score
9.  Production Readiness Score
10. Overall Score

Then summarize:

-   Critical Issues
-   High Priority Issues
-   Quick Wins
-   Technical Debt
-   Refactoring Roadmap
-   Estimated Engineering Effort

------------------------------------------------------------------------

# Weighted Score

  Category                 Weight
  ---------------------- --------
  Correctness                 20%
  Architecture                15%
  Performance                 15%
  Maintainability             15%
  Angular Modern              10%
  Security                    10%
  Type Safety                  5%
  Accessibility                5%
  Testing                      3%
  Production Readiness         2%

Every score must include written justification.

------------------------------------------------------------------------

# Future Extensions

The specification is designed to support:

-   JSON review output
-   SARIF output
-   GitHub Pull Request annotations
-   CI/CD quality gates
-   Automated scoring dashboards
-   Multi-agent review pipelines
-   Rule versioning
