# Nlist.Results — Specification

This document states the contracts and invariants that hold across the `nlistid-results`
package family. It is the behavioral source of truth for cross-package guarantees; it does not
duplicate the per-package usage docs (`src/*/README.md`), which show *how* to call the API, not
*what it promises*.

## 1. Package map

| Project | Role | Depends on |
|---|---|---|
| `Nlist.Results.Abstractions` | The contract surface: `IResult<T>`. No production dependencies. | nothing |
| `Nlist.Results` | The concrete `Result<T>` hierarchy, `ResultFactory`, `Bind`/`Map`. | `Nlist.Results.Abstractions` |
| `Nlist.Results.AspNetCore` | Maps `Result<T>` to ASP.NET Core HTTP responses. | `Nlist.Results`, `Nlist.Results.Abstractions` (`ProjectReference` — see [AGENTS.md](../docs/AGENTS.md)) |
| `Nlist.Results.Test` | MSTest + Moq tests for `Nlist.Results` only. | `Nlist.Results`, `Nlist.Results.Abstractions` (`ProjectReference`) |

Dependency direction is one-way down this table: a project may depend only on projects above it,
never below. In particular, `Nlist.Results.Abstractions` must never take a runtime
`PackageReference` (the private, build-only StyleCop analyzer is the only exception), so
consumers can depend on the contract without pulling in anything else.

## 2. The `Result<T>` type contract

`Result<T>` is a closed hierarchy. The complete set of concrete types is:

```
Result<T>
├── SuccessResult<T>
└── FailureResult<T>
    ├── NotFoundFailureResult<T>
    ├── ValidationFailureResult<T>      (adds Errors)
    ├── ConflictFailureResult<T>
    ├── RateLimitedFailureResult<T>     (adds RetryAfter)
    ├── StatusCodeFailureResult<T>      (adds Code, caller-supplied)
    ├── UnexpectedFailureResult<T>
    └── DependencyFailureResult<T>
```

Invariants:

- Every `Result<T>` is exactly one of `SuccessResult<T>` or a `FailureResult<T>` subtype. There
  is no partial-success or partial-failure state.
- `IsSuccess` is `true` if and only if the instance is a `SuccessResult<T>`.
- `Value` is non-null only on `SuccessResult<T>`; every failure type returns `default`.
- `Detail` is `null` on `SuccessResult<T>`. On failure types it is optional free text *except*
  `FailureResult<T>` itself, which requires a non-null `detail` — a bare 400 with nothing to say
  would be indistinguishable from any other failure, so the plain case is not allowed to be silent.
- Adding a new failure type is a breaking change to this spec: every `switch` over `Result<T>`
  in this repo (`Bind`, `Map`, and `HttpResultFactory`) enumerates the set by name, with the
  catch-all arm last — `_` in `Bind`/`Map`, `FailureResult<T> r` in `HttpResultFactory`. A new
  failure subtype that is not added to all three switches silently inherits the plain
  `FailureResult<T>` behavior of whichever switch wasn't updated (in `Bind`/`Map` that means it
  is turned into a plain `FailureResult<TOut>` and its own `Code` becomes 400).
- Three failure subtypes carry data beyond `Detail`: `ValidationFailureResult<T>` (`Errors`),
  `RateLimitedFailureResult<T>` (`RetryAfter`, nullable), and `StatusCodeFailureResult<T>` (its
  caller-supplied `Code`). `Bind`/`Map` must propagate that data, not just `Detail`, when
  preserving the type across a chain (§3).
- Every concrete type exposes a fixed `Code` (§4) matching its HTTP status: `SuccessResult<T>`
  200, `FailureResult<T>` 400, `NotFoundFailureResult<T>` 404, `ConflictFailureResult<T>` 409,
  `ValidationFailureResult<T>` 422, `RateLimitedFailureResult<T>` 429, `UnexpectedFailureResult<T>` 500,
  `DependencyFailureResult<T>` 502. `Code` is intrinsic to the type (like `IsSuccess`), not a
  constructor parameter like `Detail` — it cannot vary between two instances of the same type —
  **except** `StatusCodeFailureResult<T>`, whose entire purpose is to carry a caller-supplied status
  code for a failure with no dedicated subtype.
- Concrete failure types are organized in a `Failures/` subdirectory for clarity, but all declare
  `namespace Nlist.Results;` for flat API surface exposure. Core types (`Result<T>`, `SuccessResult<T>`,
  `FailureResult<T>`) remain at the project root.

## 3. Composition contract (`Bind` / `Map`)

- `Bind` and `Map` are the only two ways to compose a `Result<T>`. Both are total functions —
  they never throw for a well-formed input — and both switch exhaustively over the closed type
  set in §2.
- On any failure type, both `Bind` and `Map` short-circuit: the supplied function is never
  invoked, and a new instance of the *same* failure type is returned, carrying the original
  `Detail` forward unchanged — plus `Errors` for `ValidationFailureResult<T>`, `RetryAfter` for
  `RateLimitedFailureResult<T>`, and `Code` for `StatusCodeFailureResult<T>`.
- `Map` additionally transforms the success value (`T` → `TOut`); `Bind` delegates that decision
  to the supplied function, which itself returns a `Result<TOut>`.
- Chaining `Bind`/`Map` any number of times preserves the *first* failure encountered — once a
  chain is on a failure path, no later step can override it or reintroduce success.

## 4. `Nlist.Results.Abstractions` contract

- `IResult<T>` exposes only the read side of a result (`IsSuccess`, `Value`, `Detail`, `Code`) —
  deliberately no `Bind`/`Map`. Composition is a `Nlist.Results` concern; the abstraction exists
  so a consumer can accept or return "a result" without depending on the concrete hierarchy or
  its composition API.
- `Code` on the interface means any alternate `IResult<T>` implementation (§1) must supply its
  own fixed status code per outcome, not just `IsSuccess`/`Value`/`Detail` — it is as much a part
  of the contract as the others, not an `Nlist.Results`-specific add-on.
- `Result<T>` implements `IResult<T>`; this is the only implementation in this repo. Consumers
  coded against the interface would accept a second implementation unchanged, but
  `Nlist.Results.AspNetCore` would not: `HttpResultFactory` takes an `IResult<T>` yet matches on
  the concrete `Nlist.Results` types, and throws `InvalidOperationException` for anything else
  (§6).

## 5. Problem details alignment contract

`Nlist.Results` takes **no dependency** on ASP.NET Core and defines no `ProblemDetails` type of
its own. Instead, its constructor and factory parameters are named after the fields of
[RFC 9457 — Problem Details for HTTP APIs](https://www.rfc-editor.org/info/rfc9457/) by
convention, so that `Nlist.Results.AspNetCore`'s mapping (§6) is a rename-free pass-through
rather than a translation layer:

| `Nlist.Results` | Problem details field |
|---|---|
| `detail` parameter, surfaced as `Detail` | `detail` |
| `errors` parameter, surfaced as `Errors` | `errors` |
| `Code` | `status` |
| the failure type itself (`NotFoundFailureResult`, `ConflictFailureResult`, …) | `title`, `type` |

Invariants:

- This naming alignment is itself a contract: renaming `Detail`, `Errors`, or a factory
  parameter in `Nlist.Results` without checking it against RFC 9457's field names breaks the
  "rename-free pass-through" property §6 depends on, even though nothing in `Nlist.Results`
  itself references ASP.NET Core or would fail to compile.
- A `null` `Detail` means "nothing to add beyond the failure kind" and must be **omitted** from
  the response — not defaulted to echoing `title`, and not serialized as `"detail": null`.
- `Errors` is `IDictionary<string, string[]>` — deliberately the same shape as
  `ValidationProblemDetails.Errors`, so it can be handed to `Results.ValidationProblem(...)`
  with no mapping or copying. An empty key (`""`) denotes an error not tied to a specific
  member, matching the convention ASP.NET Core's `ModelStateDictionary` already uses.
- `FailureResult<T>.Detail` is required (non-null) specifically because it has no dedicated
  `title`/`type` of its own to fall back on (§2) — the problem details response would otherwise
  carry no information distinguishing it from any other plain failure. All other failure subtypes
  have a dedicated semantic meaning via their type name.

## 6. HTTP mapping contract (`Nlist.Results.AspNetCore`)

`HttpResultFactory.ToHttpResult<T>` is a total function over the same closed type set as §2 (any
other `IResult<T>` implementation throws `InvalidOperationException`), and the mapping is fixed:

| Result type | Status | 
|---|---|
| `SuccessResult<T>` | 200 OK |
| `ValidationFailureResult<T>` | 422 Unprocessable Content |
| `NotFoundFailureResult<T>` | 404 Not Found |
| `ConflictFailureResult<T>` | 409 Conflict |
| `RateLimitedFailureResult<T>` | 429 Too Many Requests (`retryAfter` extension, seconds, when `RetryAfter` is set) |
| `DependencyFailureResult<T>` | 502 Bad Gateway |
| `UnexpectedFailureResult<T>` | 500 Internal Server Error |
| `StatusCodeFailureResult<T>` | caller-supplied, via `code` |
| `FailureResult<T>` (the plain case) | 400 Bad Request |

Invariants:

- This table is a contract, not an implementation detail: each status code encodes who — or
  what — is responsible, so a client or retry policy can act on it instead of treating every
  failure the same way. `FailureResult<T>`/`ValidationFailureResult<T>`/`ConflictFailureResult<T>`/
  `NotFoundFailureResult<T>` mean *the request won't succeed by retrying unchanged*; `RateLimitedFailureResult<T>`
  and `DependencyFailureResult<T>` mean *retrying later may well succeed*; `UnexpectedFailureResult<T>` means
  *nobody can say which, this was unhandled*. Collapsing any of these to the same status code is
  always a bug.
- `detail` is forwarded into the problem-details body verbatim and is therefore
  **client-visible** — `Nlist.Results` and any caller constructing a result must never put
  exception text, stack traces, or connection strings in `Detail`; log those separately. This
  applies to `DependencyFailureResult<T>` in particular, since its `Detail` often originates from an
  upstream service's own error message.
- A new failure subtype added to `Nlist.Results` is a breaking change to this table until
  `HttpResultFactory` is updated with a matching arm — see §2's note on catch-all ordering. It
  must also define its own `Code` (§2, §4) — `HttpResultFactory` reads the status from `r.Code`
  rather than hardcoding a literal per arm, so this table and the type's `Code` are one source of
  truth, not two that can silently drift apart.

## 7. Versioning and publish contract

- All three packages (`Nlist.Results`, `Nlist.Results.Abstractions`, `Nlist.Results.AspNetCore`)
  are versioned and published together at the same `env.VERSION` from `nlistid-results.yml`.
- `Nlist.Results.AspNetCore` references `Nlist.Results` and `Nlist.Results.Abstractions` via
  `ProjectReference` both locally and when packing; `dotnet pack` turns each into a NuGet
  dependency at the version being packed, so the three packages always depend on each other at
  matching versions — no hand edits needed when bumping. A behavioral change to `Nlist.Results`
  (such as a §2 type-set change) is invisible to package consumers until all three are
  republished.
- There is no "already published" skip check in the publish workflow — pushing without bumping
  `VERSION` fails at `dotnet nuget push`, it does not silently no-op.
- `Nlist.Results.Abstractions` must stay dependency-free indefinitely; this is a standing
  constraint, not a current-state observation.

## 8. Testing contract (`Nlist.Results.Test`)

- Scope is `Nlist.Results` only — not `Nlist.Results.AspNetCore` (which has no test project of
  its own) and not `Nlist.Results.Abstractions` (contract-only, nothing to unit test).
  `Nlist.Results.Test` references `Nlist.Results` via `ProjectReference`, so it always tests
  local source. It currently holds only the template's empty `Test1` placeholder.
- Moq is available for mocking collaborators in tests that exercise `Bind`/`Map` chains against
  fakes; `Result<T>` itself has no collaborators to mock — tests of the type hierarchy exercise
  it directly.

## Non-goals

This spec does not cover: usage examples (see each project's README), the full set of example
`ProblemDetails` JSON responses (see `Nlist.Results.AspNetCore/README.md`), or CI/workflow
mechanics beyond the versioning invariants in §7 (see [AGENTS.md](../docs/AGENTS.md)).
