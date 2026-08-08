# Architecture Review: Cornerstone Sync Framework

Review based on `Cornerstone/Sync/*`, EF adapters, and how the pieces actually compose. This is an engineering review, not a product pitch.

---

## Executive take

This is a **serious, production-shaped sync engine**, not a toy. The layering is coherent, the hard problems (identity, paging, soft delete, relationship order, batch→individual fallback, server sanitization) are real and mostly in the right places.

It is also clearly **evolved systems code**: strong core model, a few incomplete features, a few inheritance/API surprises, and some correctness risks that will bite under concurrency, partial failure, or multi-device conflict.

**Overall grade:** solid foundation for client↔server entity sync; **not** yet a multi-master CRDT/vector-clock system, and a few paths look unfinished or inverted.

---

## What is genuinely well designed

### 1. Three-layer data model (Entity / Model / Object)

Separating **storage entity**, **wire model**, and **transport envelope** is the right split:

| Layer | Job |
|-------|-----|
| `SyncEntity` | Local PK + global `SyncId` + domain |
| `SyncModel` | Packable DTO |
| `SyncObject` | Typed envelope + status |

That gives you:

- Local IDs that never leak as identity
- Wire format control (`[Packable]`)
- Status independent of full deserialize

This is better than “serialize the EF entity and hope.”

### 2. Global identity via `SyncId`

Using a non-reusable `Guid` as the sync key is the correct baseline for offline-capable clients. Coupling that to optional `DatabaseKeyCache` for FK resolution is pragmatic and performance-aware.

### 3. Session orchestration is understandable

`SyncManager` (queue + settings + UI hooks) vs `SyncSession` (run) vs `SyncClient` (protocol) is a clean separation of concerns. The pull-then-push loop with an exclude map so reverse direction doesn’t echo the same change is good operational thinking.

### 4. Apply pipeline is battle-aware

`SyncClientForDatabase` shows real scars:

- Batch apply → on failure, individual apply
- Status normalization (Added/Updated/Deleted reconciliation)
- Soft-delete that can “add then delete” so peers learn about deletes
- Parent-before-child apply order; deletes reverse when permanent
- Server maintains `ModifiedOn`; clients don’t during apply

That is not accidental architecture. That is experience.

### 5. Server distrust of client settings

`ServerSyncClient.BeginSync` clamping page size and forcing `PermanentDeletions = false` is the right security posture. Client settings are suggestions; server owns policy.

### 6. Property-level sync control via `UpdateableAction`

Integrating sync include/exclude into the same update pipeline used elsewhere is consistent with Cornerstone’s design language. Once you understand the flags, you get fine-grained control without a second metadata system.

---

## Where the architecture is weak or wrong

### 1. `GetCorrections` is effectively a stub

In `SyncClientForDatabase`:

```csharp
public override ServiceResult<SyncObject> GetCorrections(...)
{
    ValidateSession(sessionId);
    return new ServiceResult<SyncObject>(); // always empty
}
```

But `SyncSession.Process` still has a full corrections round-trip after issues.

**Honest reading:** the *protocol* has a recovery stage; the *default implementation does nothing*. That means relationship failures, constraint failures, etc. are reported, not repaired, unless a custom client overrides this.

That is fine if intentional (“issues are for the user/admin”), but the code *looks* like automatic repair. Right now it is aspirational architecture with a dead branch.

**Recommendation:** either implement corrections (reload authoritative objects by issue SyncId) or make the empty path explicit in naming/docs and stop implying recovery.

### 2. Inheritance inversion: `WebSyncClient : ServerSyncClient`

A remote HTTP client is not a server. Inheriting `ServerSyncClient` pulls in:

- Server semantics (`IsServerClient` via `this is ServerSyncClient`)
- Server `BeginSync` sanitization path (overridden by web posts, but type identity still lies)
- Database provider in a type that primarily posts over the wire

This will confuse every new reader and may cause subtle bugs if any base behavior keys off `is ServerSyncClient`.

**Recommendation:**  
`SyncClient` ← `SyncClientForDatabase` ← `ServerSyncClient`  
`SyncClient` ← `WebSyncClient` (composition of `IWebClient`, not server inheritance)

### 3. `WaitForSyncsToComplete` looks inverted / broken

```csharp
if (_syncQueue.IsEmpty && SyncSession.SyncCompleted)
    return true;

while (_syncQueue.IsEmpty && SyncSession.SyncCompleted)
{
    // timeout...
}
return true;
```

If work is **still running** (`!SyncCompleted` or queue non-empty), the method never enters a wait loop that waits for completion. The `while` condition only continues while already complete and empty—which is the opposite of waiting.

Unless a threading trick is missing, **`Sync()` after `SyncAsync()` is not reliably waiting**. That is a correctness bug, not a style nit.

### 4. Success model is all-or-nothing, but time advancement is easy to get wrong

`Successful` requires no issues. Manager only **persists** last-synced on success—good.

But inside the session, `Settings.LastSyncedOn*` are still written from session start times before success is decided. Any caller reading settings off the live session mid-flight or off a failed response copy can think the window advanced when it didn’t persist—or the inverse if someone copies settings incorrectly.

More importantly: **partial success is not modeled**. If 99/100 objects apply and one fails, the whole session is unsuccessful and you re-sync the whole window next time (minus exclude map within the same run only). That is simple and safe, but can be expensive and re-apply-heavy. Fine for small datasets; painful for large ones.

### 5. Conflict strategy is LWW on `ModifiedOn` only

Update is skipped when:

```text
found.ModifiedOn >= incoming.ModifiedOn && !correction
```

That is last-write-wins by wall clock (or by whoever last set ModifiedOn). There is:

- No vector clock / version counter
- No field-level merge
- No “server always wins” policy switch beyond direction + sanitization
- Clock skew / offline edits can silently drop changes

For many apps (settings, account metadata, admin-owned data) LWW is acceptable. For multi-device concurrent edits of the same entity, **this will lose data without anyone noticing** (no issue raised—just “not newer”).

Be honest about the product domain: if two clients can edit the same row offline, this is not enough.

### 6. Filter API: opt-in by existence, not by intent

```csharp
ShouldSyncRepository => _filters.ContainsKey(typeAssemblyName)
```

So `AddFilter<T>()` with all null predicates means “include everything of type T.” That is powerful and compact, but:

- Naming says “filter,” behavior says “include list + optional predicates”
- Easy to omit a type and get silent non-sync
- Incoming filter polarity (`ShouldFilterIncomingEntity` = true means reject) is easy to reverse mentally

Architecturally fine; **API affordance is footgun-shaped**. A rename or dual API (`IncludeRepository` + `Where`) would reduce mistakes.

### 7. Type-name coupling and AOT fragility

Wire identity is assembly-qualified type names. Converter matching is string equality on model vs entity names. `SyncObject.ToSyncModel()` uses `Type.GetType` + `Activator.CreateInstance`.

That works in full reflection desktop worlds. It is fragile under:

- Trimming / AOT
- Type renames / namespace moves
- Multiple assemblies with similar names

Source generators and `[SourceReflection]` exist elsewhere—sync still leans on classic reflection in hot paths (relationships, converter defaults, model materialization).

### 8. Relationship discovery by reflection convention

`Name` + `NameId` + `NameSyncId` is a strong convention. It avoids mapping tables. Cost:

- Silent failure if naming drifts
- Lookup filters that change identity keys break the mental model (comments already admit this)
- Hierarchy/`ParentSyncId` is a second pattern alongside the first

Convention-over-config is fine if enforced (analyzer/tests). Without that, it is tribal knowledge.

### 9. Exception classification by string matching

FK detection via message contains `"conflicted with the FOREIGN KEY constraint"` is a smell. It works until culture, provider, or EF wording changes. Prefer structured exception types / SQL error numbers where possible.

### 10. Shared mutable `SyncSession` on the manager

One session instance for UI binding is convenient. It is also a concurrency hazard if anything else reads it while a run mutates flags, issues, settings. Mitigated with a result *copy* at the end—good—but the live object is still a race surface for UI and tests.

### 11. Session state as flags for a linear pipeline

`SyncSessionState` is `[Flags]` and states accumulate (`Started | Configuring | Configured | Pulling | …`). For a linear workflow, an exclusive enum (or a small state machine) is clearer. Flags make “what phase am I in?” multi-bit decoding and invite invalid combinations.

Not fatal; just muddier than the rest of the design.

### 12. Inconsistent defaults

- Manager seed: `ItemsPerSyncRequest = 600`
- `SyncSettings.Reset()`: `10000`
- Server clamp: max `10000`

Small, but it signals the settings object has multiple authors over time without a single “defaults of record.”

### 13. Sync types as free-form strings

`supportedSyncTypes` as `string[]` is flexible. It also means no compile-time safety, easy typos, and no structured composition of “what filters belong to Full vs Accounts.” A typed registry (`ISyncProfile`) would scale better as scenarios grow.

---

## Architectural tensions (not bugs, but tradeoffs to own)

| Tension | Current choice | Cost |
|--------|----------------|------|
| Simplicity vs multi-master | LWW + direction | Silent lost updates under concurrent edit |
| Performance vs safety | Batch then individual | Individual path is slow; good for correctness |
| Flexibility vs discoverability | Filters as include list | Silent omissions |
| Generality vs speed | Reflection converters/relationships | Harder AOT, harder to follow |
| UI convenience vs purity | Shared session + dispatcher | Mutation visibility races |
| Protocol completeness vs implementation | Corrections API exists | Default client no-ops |

None of these are automatically wrong. They are choices. The problem is when the *code implies* a richer system than you actually run (corrections, multi-device safety).

---

## Security / trust notes

**Good**

- Server sanitizes page size and permanent delete
- `ValidateSyncClient` hook for allowlisting
- Web path separates transport

**Gaps to be aware of**

- Filters and sync direction are partly client-influenced; server copies `SyncDirection` and last-synced stamps from untrusted settings after only partial re-homing. A hostile client can influence *what window* they claim. Server should re-derive or clamp last-synced from server-side session store if that matters.
- Authorization beyond “client supported” is not visible in this layer (must live in web pipeline / credentials).
- `IncludeIssueDetails` can leak internals if ever enabled server-side for clients.

---

## What to fix first (priority)

1. **Fix or delete `WaitForSyncsToComplete` wait logic** — high severity if `Sync()` is used.
2. **Decide the story for corrections** — implement or demote the protocol noise.
3. **Break `WebSyncClient : ServerSyncClient`** — type model currently lies.
4. **Document conflict model as LWW** in product terms; add version column if better conflict handling is needed.
5. **Rename filter API** toward include semantics; keep predicates secondary.
6. **Add tests around:** filter omission, relationship order, soft delete create-then-delete, last-synced only on success, cancel mid-page.
7. **Harden type materialization** for AOT if mobile/browser targets matter.

---

## What not to rewrite

Do not throw away:

- Entity / Model / Object separation  
- `SyncId` + soft delete  
- Manager / Session / Client split  
- Batch→individual apply  
- Server sanitization of dangerous options  
- SyncOrder for dependency apply  

That core is sound. The framework needs **tightening and finishing**, not a greenfield redesign.

---

## Bottom line

This is a **client/server entity sync engine with offline-friendly identity, pragmatic apply semantics, and real failure handling**—not a marketing “sync” wrapper.

Where it is wrong or incomplete:

- Corrections look real; default path is empty.  
- Web client inheritance says “server” when it isn’t.  
- Wait-for-complete looks inverted.  
- Conflict handling is weaker than the rest of the system’s sophistication implies.  
- Filter-as-include and string type names are power tools that punish small mistakes.

Where it is right:

- Layering, identity, apply pipeline, server distrust of client settings, and the general pull/push orchestration.