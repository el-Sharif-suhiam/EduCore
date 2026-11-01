# EduCore — Agent Instructions

This file establishes mandatory constraints for any AI agent or developer working on the
EduCore repository. The architecture rules below are binding, not recommendations.

## Layer Separation and Communication Rule

EduCore uses a strict, one-directional layered architecture:

```text
EduCoreAPI
    |
    v
EduCore_BusinessLayer
    |
    v
EduCore_DataAccess
    |
    v
Database
```

`Common` contains shared contracts and types (DTOs, enums, exceptions, ViewModels,
validators) used by the layers where necessary. It does not provide a way to bypass
the intended communication flow.

### Allowed project references

| Project | Allowed project references |
|---|---|
| `EduCoreAPI` | `EduCore_BusinessLayer`, `Common` |
| `EduCore_BusinessLayer` | `EduCore_DataAccess`, `Common` |
| `EduCore_DataAccess` | `Common` |
| `Common` | *(none)* |

### Mandatory rules

1. **API → Business Layer only.** The API (`EduCoreAPI`) may communicate only with the
   Business Layer (`EduCore_BusinessLayer`) for application operations.
2. **API never touches Data Access.** The API must never directly reference, import,
   instantiate, or call Data Access (`EduCore_DataAccess`) classes or methods. This
   includes removing unnecessary `using EduCore_DataAccess;` directives from API source
   files.
3. **Business Layer is the only Data Access caller.** `EduCore_BusinessLayer` is the only
   layer permitted to communicate with `EduCore_DataAccess`.
4. **Data Access never reaches upward.** `EduCore_DataAccess` must never communicate with
   or depend on `EduCoreAPI` or `EduCore_BusinessLayer`.
5. **All database operations flow through the Business Layer.** Any database-related
   operation exposed to the API must be surfaced through a Business Layer class (for
   example, certificate lookup is exposed via `clsProgress.GetCertificateByCode`, not
   `clsCertificatesData.GetByCode`).
6. **Dependency direction is strictly one-way:** `API → Business Layer → Data Access`.
   Never skip a layer, reverse the direction, or establish direct communication between
   non-adjacent layers.
7. **Common is not a bypass.** `Common` may be referenced by the appropriate layers for
   shared types and contracts, but must never be used to circumvent the communication
   boundaries above.
8. **Preserve the rules in all future work.** Any future feature, bug fix, or
   modification must preserve these rules.
9. **Verify before referencing.** Before introducing a new project reference or calling a
   class from another layer, the agent must verify that the communication is permitted
   under this rule.
10. **Correct violations before implementing.** If a proposed implementation violates
    these rules, the agent must correct the design before implementing it.

### Layer responsibilities

- **API (`EduCoreAPI`):** presentation/HTTP entry point. Controllers, authorization
  handlers, middleware, request/response shaping. It communicates with the Business Layer
  and must never call Data Access classes or methods directly.
- **Business Layer (`EduCore_BusinessLayer`):** intermediary between the API and Data
  Access. Domain entities (`cls*`), business rules, orchestration, and audit logging. Any
  operation requiring database access must be exposed through this layer.
- **Data Access (`EduCore_DataAccess`):** database access. `cls*Data` classes, raw
  ADO.NET, stored procedures. It must not depend on or communicate directly with the API
  or Business Layer.
- **Common:** shared types and contracts. It must not be used to circumvent the layer
  boundaries. Note: the shared-contracts project is named `Common` (`Common/Common.csproj`,
  root namespace `Common`); some older task descriptions call it `EduCore_Common` — that
  name does not exist in this repository and always refers to this `Common` project.

**Key principle:** `API → Business Layer → Data Access`. Never skip a layer, reverse the
dependency direction, or establish direct communication between non-adjacent layers.
