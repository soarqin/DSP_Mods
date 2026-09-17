# LiveStreamAssist WebSocket Design and Implementation Plan

Status: implementation not started. This document is the handoff for a later implementation session. The current mod only implements statistics-window tab switching. The next implementation task is **Phase 0: runtime transport verification**.

The [API reference](WebSocketApi.md) defines the client-visible contract. This document defines the scope, implementation constraints, source references, work order, and acceptance checks. Keep API usage documentation separate from implementation investigation and progress notes.

## Confirmed scope

The following decisions were confirmed with the user and must survive a session handoff:

- Support both local and LAN WebSocket clients.
- Use **no authentication** in v1. The user explicitly chose this after LAN access was discussed. Do not add a shared token, login endpoint, accounts, or permissions workflow.
- Provide read-only, client-initiated requests for in-game data, primarily through reflection.
- Provide connectivity and API-compatibility validation. Here, validation does not mean identity authentication or a separate reflection-path-validation API.
- Report the API version, mod version, running game version, and running game build.
- Reserve API statistics through `system.stats`, returning `available: false` and `metrics: null`; advertise `api.stats: false`.
- Do not implement subscriptions, application notifications, batch requests, game writes, arbitrary method invocation, or script evaluation.
- Preserve the existing Ctrl+F8 statistics-window feature and its independent state.

Keep the implementation small: one listener, a fixed method switch, fixed root providers, a bounded main-thread dispatcher, and shallow result projection. No RPC framework, general-purpose reflection service, dependency-injection container, external server process, HTTP REST API, web dashboard, or custom WebSocket framing implementation is required.

The initial listener configuration and all wire limits are defined in [API connection settings](WebSocketApi.md#connection) and [API limits](WebSocketApi.md#limits). Keep those tables as the single documentation source for their values. Configuration is startup-only in v1; do not introduce live configuration reload as part of this work.

## Starting or resuming implementation

1. Read the repository `AGENTS.md`, this document, and the API reference before editing code.
2. Inspect `git status` and the current diff. Treat existing changes as user work. Check whether some phases have already been implemented rather than assuming this document's initial status still describes the checkout.
3. Read the source entry points in the next section. Preserve the host-owned feature lifecycle and existing keyboard handling.
4. Recheck runtime-specific assumptions against the installed game's original assemblies if DSP or Unity has changed. Do not use stripped reference DLL method bodies as evidence.
5. Complete phases in dependency order. Mark a phase complete only after its acceptance checks pass; identify unavailable real-game or LAN checks as blocked, not passed.
6. If pausing between sessions, update this document's current status, remaining phase checks, selected dependency versions, and concrete blockers. Replace stale notes instead of appending a chronological transcript. Keep project-specific guidance here; `AGENTS.md` should contain only a link to this document.

The API reference is the source of truth for method names, message shapes, numeric error codes, null behavior, and capability names. Resolve any conflict with this plan before implementing it. The transport package/version is intentionally provisional until Phase 0; that is not permission to redesign the protocol or expand the confirmed scope.

## Repository entry points

Paths in this table are relative to the repository root.

| File / symbol | Why it matters |
| --- | --- |
| `LiveStreamAssist/LiveStreamAssist.cs` / `LiveStreamAssist.Awake` | BepInEx plugin startup; performs localization setup and feature discovery. Bind API configuration before discovery. |
| `LiveStreamAssist/LiveStreamAssist.cs` / `LiveStreamAssistFeature` | Existing feature, order 50; owns Ctrl+F8 and statistics-window switching. Its `_enabled` flag is not the server-enabled flag. |
| `LiveStreamAssist/LiveStreamAssist.csproj` | Inherits `net472`, currently references UXAssist, and owns the mod version. Add only the server's required dependencies and project-scoped packaging customization here. |
| `UXAssist/Common/ModFeatures/ModFeatureRegistry.cs` | Initializes discovered features eagerly. UXAssist alone drives `StartAll`, `UninitAll`, and update dispatchers. |
| `UXAssist/UXAssist.cs` / `Update` | Skips feature updates while `VFInput.inputing` or `DSPGame.IsMenuDemo` is true. This is unsuitable as the sole server request pump. |
| `UXAssist/Common/GameLogic.cs` | Supplies `OnDataLoaded`, `OnGameBegin`, and `OnGameEnd` callbacks. Use the fully qualified UXAssist type or the existing `GameLogicProc` alias to avoid DSP's own `GameLogic` type. |
| `Directory.Build.props` | Shared BepInEx dependencies, Unity compile assets, original-game reference refresh dependency, and warning policy. |
| `Directory.Build.targets` / `ZipMod` | Packages the main assembly and selected root files. It does not automatically package NuGet runtime dependencies. |
| `UpdateGameDlls.ps1` | Documents Steam registry and `libraryfolders.vdf` discovery. Running it updates/publicizes compile references; read it for installation discovery rather than using its outputs for decompilation. |
| `LuaScriptEngine/LuaState.cs` | Existing research/statistics access examples, not a reusable WebSocket server. LuaScriptEngine is not a dependency of this feature. |

### Verified original-runtime facts

These facts were checked while preparing the plan. They are implementation constraints, not a substitute for testing the selected library in DSP:

| Original assembly / symbol | Finding and consequence |
| --- | --- |
| `System.dll` / `System.Net.HttpListenerContext.AcceptWebSocketAsync` | The inspected overloads throw `NotImplementedException`. Do not select the framework `HttpListener` WebSocket server path on the basis of desktop .NET tests. |
| `UnityEngine.CoreModule.dll` / `UnityEngine.UnitySynchronizationContext` | `Post` enqueues work; `Exec` copies queued work before running callbacks. Capture the installed main-thread context and post a bounded pump through it. |
| `Assembly-CSharp.dll` / `GameConfig` | Supplies `gameVersion` and the separate `build` field. Read these from the running game. |
| `Assembly-CSharp.dll` / global `Version` | DSP has its own `Version` struct. `ToString()` omits its `Build` field. Do not confuse this type with `System.Version` or assume an assembly version is the game version. |
| `Assembly-CSharp.dll` / `GameMain` | Root getters return current `GameData` members. Menu demos can have allocated game data; checking only `GameMain.data != null` is insufficient. Pausing does not end a running game. |
| `Assembly-CSharp.dll` / `VFPreload` | `InvokeOnLoadWorkEnded` runs before `done` becomes true. `done`/`dbDone` can be used to detect an already completed preload when starting late; do not require them to be true inside the load-work-ended callback itself. |
| `Assembly-CSharp.dll` / `GameHistoryData.currentTech` | This is an auto-property with a public getter and private setter, not a public field. The getter only returns its backing value and is the initial allowed property. |
| `Assembly-CSharp.dll` / `GameConfig` getters | Some apparently readable properties create directories. Public visibility or the absence of a public setter does not establish a side-effect-free getter. |

To reproduce inspection, locate Steam using the registry and parse `steamapps/libraryfolders.vdf` for app ID `1366540`, as `UpdateGameDlls.ps1` does. Resolve `<game_root>/DSPGAME_Data/Managed/` and decompile the installed original DLLs, for example:

```text
ilspycmd --disable-updatecheck -t GameConfig <managed>/Assembly-CSharp.dll
ilspycmd --disable-updatecheck -t Version <managed>/Assembly-CSharp.dll
ilspycmd --disable-updatecheck -t GameMain <managed>/Assembly-CSharp.dll
ilspycmd --disable-updatecheck -t GameHistoryData <managed>/Assembly-CSharp.dll
ilspycmd --disable-updatecheck -t VFPreload <managed>/Assembly-CSharp.dll
ilspycmd --disable-updatecheck -t UnityEngine.UnitySynchronizationContext <managed>/UnityEngine.CoreModule.dll
ilspycmd --disable-updatecheck -t System.Net.HttpListenerContext <managed>/System.dll
```

Replace placeholders and quote paths containing spaces. Do not depend on this session's temporary decompilation output or a hard-coded Steam drive letter. Do not commit game DLLs or decompiled game source.

## Intended code organization

Use these responsibilities as the starting layout; keep small private helpers with their owner rather than creating an interface or service for each step.

| Planned file | Responsibility |
| --- | --- |
| `LiveStreamAssist/Api/WebSocketApiFeature.cs` | Configuration references, feature lifecycle, version publication, game-session tracking, and root providers. Suggested feature order: 51. |
| `LiveStreamAssist/Api/WebSocketApiServer.cs` | Listener, connections, message admission, serialized sends, and connection cleanup. |
| `LiveStreamAssist/Api/ApiProtocol.cs` | DTOs, error constants, validation, fixed method dispatch, system methods, and the API version constant. |
| `LiveStreamAssist/Api/ReflectionReader.cs` | Shared path resolution, allowed-member discovery, value projection, and detached result construction. |
| `LiveStreamAssist/Api/MainThreadDispatcher.cs` | Bounded request queue, one scheduled pump, cancellation, and deadline/session checks. |
| `LiveStreamAssist/tools/Test-WebSocketApi.ps1` | Runnable WebSocket integration checks using PowerShell 7 and `System.Net.WebSockets.ClientWebSocket`. |

Bind the three API settings in plugin startup before `ModFeatureRegistry.Discover`. Use a separate `[ModFeature]` class for the server. Do not have LiveStreamAssist call registry dispatchers, add a second shared update driver, relax UXAssist's menu/typing guards, or attach server activation to Ctrl+F8.

Keep `net472`. Prefer one managed Mono-compatible WebSocket server dependency, initially evaluate Fleck, and explicitly reference Newtonsoft.Json. LuaScriptEngine's transitive Newtonsoft.Json reference does not make that DLL a guaranteed runtime dependency of LiveStreamAssist. Pin versions only after compatibility verification; record the chosen versions in this document and the project file.

## Runtime design

### Lifecycle and metadata

- `Init`: prepare configuration references and non-network state. Keep initialization safe if the API is disabled.
- `Start`: if enabled, capture `SynchronizationContext.Current` on Unity's main thread, initialize a server owner instance, subscribe to game lifecycle events, and start the listener without blocking the game loop. If the current context is null or is not Unity's installed context, fail startup with a useful log; a newly constructed generic `SynchronizationContext` is not a main-thread dispatcher.
- Publish version data from `GameLogicProc.OnDataLoaded`. If starting after preload, use the original preload completion flags to populate it immediately. Publish an immutable snapshot; network callbacks never read `GameConfig` or Unity state directly. Preserve this snapshot across save changes.
- Source `modVersion` from generated `PluginInfo.PLUGIN_VERSION`. Keep `apiVersion` independent, initially `1.0.0`. Use the explicit `System.Version` name if parsing the API version to avoid DSP's global `Version` type.
- Subscribe to `OnGameBegin` and `OnGameEnd` for session transitions. Account for starting after a game has already begun.
- `Uninit`: stop admitting work, invalidate the owner/session, cancel requests, unsubscribe events, close the listener, and close connections. Clear metadata caches owned by the server. Posted callbacks must check their old owner token so they cannot run against a restarted instance.
- Startup errors, including an occupied port or invalid settings, disable only this server instance and are logged. Do not silently bind a different address/port or let an exception escape into other features' startup/shutdown.
- Start/stop and cleanup must be idempotent. Never synchronously wait on a task whose completion needs the Unity thread during shutdown.

### Root providers and readiness

Register these fixed mappings inside LiveStreamAssist:

| Root | Provider |
| --- | --- |
| `game` | `GameMain.data` |
| `player` | `GameMain.mainPlayer` |
| `history` | `GameMain.history` |
| `statistics` | `GameMain.statistics` |
| `galaxy` | `GameMain.galaxy` |
| `localPlanet` | `GameMain.localPlanet` |
| `localStar` | `GameMain.localStar` |

A player game is ready when `GameMain.data` exists, `GameMain.isRunning` is true, loading is false, and `DSPGame.IsMenuDemo` is false. Re-evaluate these conditions on the main thread at execution time. Do not reject an otherwise ready game because it is paused, a fullscreen UI is open, or text input is active.

Root discovery always lists the fixed catalog, using declared types when no instance is available. `data.read`/`data.describe` reject a not-ready game. A known root that returns null during a ready game is `ROOT_UNAVAILABLE`; null reached at the end of a member path is a successful null value.

Do not retain root instances, collection instances, or getter return objects in reflection caches. Resolve roots per request. Treat only the specified root set as remote entry points; exposing a client-supplied assembly/type name would bypass this boundary.

### Network-to-main-thread flow

```text
WebSocket callback
  -> validate envelope, parameters, ID, size, and capacity
  -> system method: respond from immutable/process-only data
  -> game-data method: capture session generation and enqueue
       -> one Unity SynchronizationContext.Post pump
       -> recheck connection, deadline, session, and readiness
       -> resolve root/path and build detached result
  -> serialize and send off the Unity thread
```

- Network receive, JSON parsing, serialization, and socket sends stay off the Unity thread. Avoid accidentally capturing Unity's context in network async continuations.
- Only one pump callback may be scheduled per server owner at a time. Process work within the API's per-frame count and soft-time budget, then repost remaining work. Do not busy-wait, spin, or post one unbounded callback per request. Track the Unity frame for the budget if the context is pumped more than once in a frame.
- Use a standard queue with a small lock or `ConcurrentQueue` with correct admission accounting. A custom scheduler or lock-free queue is unnecessary. If using task completions, run continuations asynchronously so response work does not execute inline in the pump.
- A request owns one admission slot until response delivery or cancellation. Capacity includes outgoing responses. Serialize sends per connection, bound the send queue, and close slow consumers rather than retaining unlimited output.
- Expiry is based on monotonic elapsed time, not `Time.time` or game ticks. Check expiry before reading. Once timed out, work cannot later emit success. A deadline does not abort a getter halfway through; the read policy must keep getters short and bounded.
- Cancellation and session invalidation must remove or cheaply skip queued work and release its slot. Use one completion path so timeout, disconnect, and successful reads cannot double-complete or leak admission counts.
- The only mutable game references exist during main-thread execution. Return scalar values, strings, copied arrays/pages, and plain snapshot dictionaries/DTOs; never pass a live DSP object, boxed game struct with nested references, `MemberInfo`, or `UnityEngine.Object` to the background JSON serializer.

### Session transitions

Maintain an internal generation that changes at game-session boundaries and server stop, plus an opaque public `sessionId` for each ready player session. A GUID is sufficient for the public ID; it is not persisted and has no authentication meaning.

Publish the current admission generation as immutable/thread-safe state. Each admitted game-data request records it without carrying a game object across threads. Before executing and before completing an in-progress sample, compare it with current state. Cancel pending old-generation work with `SESSION_CHANGED` instead of resolving its path against a new save.

On game end, publish an unavailable session and invalidate pending work while keeping the listener and process-level version metadata alive. Game readiness checks still run when requests execute, covering state changes between callbacks. Already delivered/in-transit snapshots remain identified by their old `sessionId`; do not claim they can be recalled.

### Reflection and serialization policy

Use one resolver/member-policy path for `data.read`, `data.describe`, and projections. Fixing or restricting access must affect all three paths together.

1. Validate segment kinds and bounds before invoking reflection. Retain whether `select`, `offset`, and `limit` were explicitly supplied; array/list defaults must not cause ordinary scalar reads to fail parameter validation. No dotted-path parser, expression compiler, wildcard search, or arbitrary indexer binder is needed.
2. Resolve instance fields with public and non-public visibility, walking declared members from the most-derived type toward base types. Skip static and compiler-generated fields. Apply type restrictions before traversing a value. A denied declaration does not fall back to a same-named base declaration.
3. Permit ordinary data types defined in the game's `Assembly-CSharp` assembly, the API's scalar types, the specified concrete BCL containers, and the listed Unity value structs. Apply infrastructure exclusions before the assembly allowance: being a game-defined subclass of `UnityEngine.Object`, a stream, or another runtime-service type does not make it readable. Do not traverse native handles, pointer/by-ref fields, delegates, `Type`/reflection metadata, tasks/threads, locks, streams, or arbitrary external-plugin types.
4. Generic getter access starts with this exact allowlist only:

   | Declaring type | Property | Verified behavior |
   | --- | --- | --- |
   | `GameHistoryData` | `currentTech` | Public auto-property getter returning its backing value; private setter is not exposed. |

   Additional properties require a concrete query need and inspection of the original getter and any called methods. Record the exact declaration and reason here and update the API reference before adding it. Do not allow every public getter, call a setter, or bypass a rejected getter through its compiler-generated backing field.

5. Use concrete BCL array/list/dictionary adapters for indexing and counts. Do not accept arbitrary `IList`, `IDictionary`, or `IEnumerable` implementations merely because they expose a familiar interface. No full dictionary enumeration, custom collection internals, or pool compaction is needed in v1.
6. Keep projections shallow as specified by the API. Explicit paths may follow cyclic references within the path limit, but summaries terminate output traversal. No graph serializer, persistent object handles, reference registry, or general cycle-resolution framework is needed.
7. Encode 64-bit values and other scalars before passing snapshots to JSON serialization. Use Newtonsoft.Json without automatic CLR type construction (`TypeNameHandling.None`); parse request DTO/token data only. Never use `SerializeObject` or `JToken.FromObject` on an arbitrary game object.
8. Bound serialized output while producing it, not after creating an unlimited JSON string. Bound snapshot construction too: enforce projection/page limits and reject obviously oversized strings before copying/encoding them. Metadata discovery is shallow and must not execute listed getters.
9. Cache successful type/member metadata, not requests or failed arbitrary path strings. Use a bounded cache (for example, at most 1024 entries, then stop adding entries) rather than a new eviction framework. Clear it on server uninitialization.

Catch reflection/getter failures at the request boundary and map them to the API errors. Retain diagnostic exceptions in local logs, not response bodies. System methods and generic game-data methods must share envelope/error handling; do not introduce parallel protocol implementations.

## Implementation phases

### Phase 0: runtime transport verification

- [ ] Inspect the current project state and original runtime assumptions listed above.
- [ ] Evaluate Fleck first as a single managed WebSocket server dependency, plus an explicit Newtonsoft.Json dependency, while retaining `net472`.
- [ ] Verify the server inside actual DSP/BepInEx/Mono using a standard WebSocket client. A standalone .NET console server is not evidence of game-runtime compatibility.
- [ ] Verify text-message fragmentation, invalid UTF-8 handling, binary rejection, connection closure, and request-size enforcement before unbounded message accumulation. Check the library's behavior before promising a limit that its callbacks cannot enforce.
- [ ] Verify path rejection, clean listener disposal, occupied-port failure, and a bounded serialized send path.
- [ ] Record the selected package versions and any necessary runtime dependency DLLs here; pin them in `LiveStreamAssist.csproj`.

If Fleck cannot meet the runtime or bounded-buffer requirements, evaluate one other small managed Mono-compatible server library and document the reason. Do not write a new framing stack, switch to an ASP.NET host, change the target framework, or add multiple competing transports to hide an unresolved compatibility problem. Treat missing real-game verification as a blocker.

Exit condition: the selected transport can actually listen, exchange bounded messages, and release its resources in the game's runtime. Reuse the verified code in the implementation rather than leaving a second prototype server in the tree.

### Phase 1: lifecycle, protocol, and system methods

- [ ] Add the separate API feature and startup-only configuration; preserve the existing feature's registration and state.
- [ ] Implement request validation, ID tracking, fixed dispatch, response/error envelopes, capacity accounting, and serial sends.
- [ ] Implement `system.ping`, `system.info`, and `system.validate` with the specified state-independent semantics.
- [ ] Implement only the `system.stats` placeholder. Keep `api.stats` and `subscriptions` false; advertise reflection capabilities only when their implementations are ready.
- [ ] Publish runtime version metadata through main-thread initialization/lifecycle code and handle late feature startup.
- [ ] Add the integration script's no-save mode for system methods, malformed messages, invalid IDs, unknown methods, incompatible majors/capabilities, and the statistics placeholder.

Exit condition: system methods work from local and LAN clients without authentication, including at the main menu. Version fields match the running game after preload. Startup/stop failures do not interrupt other features. Invalid requests produce the documented response or transport closure.

### Phase 2: main-thread queries and reflection

- [ ] Add the bounded synchronization-context pump and current-session admission state.
- [ ] Add the seven fixed root providers and readiness checks, including the menu-demo exclusion and paused-game allowance.
- [ ] Implement the shared member policy, exact getter allowlist, typed path traversal, null handling, and deterministic metadata listing.
- [ ] Implement scalar encoding, summaries, explicit projections, and array/list pages exactly as the API reference specifies.
- [ ] Attach `sessionId` and `gameTick` to game-data results; ensure returned snapshots contain no live game references.
- [ ] Extend the integration script with `-RequireGame` checks that discover roots, read `history.currentTech`, read `history.techQueue`, and inspect/read a research state using an ID obtained from the game.
- [ ] Leave one small runnable set of fixture-based checks for the production reflection/projection code, covering inherited private fields, blocked getters, null intermediates/terminals, dictionary key types, indexes, shallow cyclic references, 64-bit precision, and non-finite values.

Keep fixture checks outside the shipped plugin and out of the remote root catalog. Use the smallest repository-compatible harness; no new testing framework or abstraction is required solely for these checks. Exercise the production reader, not a reimplementation of it, and record the exact run command here once its layout is chosen.

Exit condition: returned research values match the in-game data, precision is preserved beyond `2^53`, rejected members cannot be reached through alternate read/describe/projection paths, and queries execute on Unity's main thread while typing or paused.

### Phase 3: lifecycle races, resource bounds, and recovery

- [ ] Verify a queued read cannot run against a different save after loading, unloading, or reloading a session.
- [ ] Verify timeout/disconnect/session-change races release admission capacity once and cannot emit late or duplicate success responses.
- [ ] Verify menu, loading, pause, space-travel root absence, and game-end behavior match the documented error distinctions.
- [ ] Exercise capacity and payload limits with multiple clients, fragmented oversized input, slow readers, and large projections/pages. Output must stay bounded during serialization, not merely fail after allocation.
- [ ] Verify an ordinary query still succeeds after malformed requests, busy responses, and other recoverable errors.
- [ ] Verify idempotent stop, released listener ports, ignored callbacks after disposal, and fail-soft behavior when another process owns the configured port.
- [ ] Regress Ctrl+F8 startup/stop, window closure, and the existing randomized tab switching.

Exit condition: the server remains usable after recoverable failures, old sessions cannot leak through queued work, and overload cannot create unbounded queues or main-thread network work. Report actual observations rather than asserting an unmeasured FPS or latency guarantee.

### Phase 4: packaging and final handoff

- [ ] Add the selected runtime dependencies to LiveStreamAssist's package, scoped to this project.
- [ ] Build, run the fixture checks and integration script, and complete the manual real-game/LAN acceptance matrix below.
- [ ] Inspect the resulting ZIP and test a clean install with only the manifest-declared dependencies and packaged runtime DLLs. Do not rely on DLLs provided incidentally by LuaScriptEngine or the development environment.
- [ ] Update the API reference and project README to describe verified implemented behavior. Remove the planned-only status only when the implementation is actually ready.
- [ ] Update this design document's current status, dependency selection, verified environment, and any remaining blockers. Keep `AGENTS.md` as a documentation pointer instead of copying this plan into it.
- [ ] Run `git diff --check` and review the final diff for unrelated changes.

The existing `ZipMod` target does not gather dependency DLLs automatically. Prefer a project-local target with `BeforeTargets="ZipMod"` that adds the explicitly selected runtime DLLs to `_PackRootFiles`; the shared target will then stage them along with the plugin. Confirm the ordering and actual resolved filenames in the build output. Do not copy the entire output directory: that can include game references, Unity assemblies, BepInEx assemblies, and UXAssist itself. No shared packaging redesign is needed.

The project README can link to source-hosted developer documentation so its links also work when the README is distributed without the repository's `docs` folder.

## Validation commands and acceptance matrix

Run commands from the repository root. These are implementation-time commands; their inclusion here does not mean the server, scripts, or runtime checks already exist or have passed.

Before the integration commands, install the built mod and its required DLLs, set `WebSocketApi.Enabled` to true, and restart DSP. Run no-save checks at the main menu, then open a save for `-RequireGame`. For the LAN check, configure the actual LAN bind address or `0.0.0.0` before restarting and connect from the second machine to the server's real LAN address, not to `0.0.0.0`.

```powershell
dotnet restore LiveStreamAssist/LiveStreamAssist.csproj
dotnet build LiveStreamAssist/LiveStreamAssist.csproj -c Release --no-restore
pwsh -NoProfile -File LiveStreamAssist/tools/Test-WebSocketApi.ps1 -ServerUri ws://127.0.0.1:18080/api/v1
pwsh -NoProfile -File LiveStreamAssist/tools/Test-WebSocketApi.ps1 -ServerUri ws://127.0.0.1:18080/api/v1 -RequireGame
dotnet build LiveStreamAssist/LiveStreamAssist.csproj -t:ZipMod -c Release --no-restore
git diff --check
```

The smoke script must accept those parameters, require no WebSocket client package, close its sockets in cleanup, and exit nonzero when an assertion fails. `-RequireGame` fails clearly if a save is not ready; no-save mode skips only game-dependent assertions, not protocol/system checks. Repeat the relevant script from a second machine with the actual LAN address. A loopback-only run does not establish LAN acceptance.

Do not use game-mutating debug endpoints to make checks pass. If a particular research ID is needed, discover it from current research, the research queue, or known game data; do not assume the API example's `1001` is always active.

| Scenario | Required observation |
| --- | --- |
| Default configuration | API disabled, no listener, existing assist still works. |
| Preload / main menu | Ping works; version readiness is explicit; menu demo is not an available player root. |
| Active save | Roots, metadata, research reads, projections, and pages are correct. |
| Pause / text input | Data reads still complete through the Unity context. |
| Space travel | Absent local roots produce `ROOT_UNAVAILABLE`, not a crash or false whole-game failure. |
| Large integer fixture | `Int64`/`UInt64` values beyond JavaScript's safe range round-trip as decimal strings. |
| Forbidden getter / infrastructure | Every resolver path enforces the same policy without invoking the denied code. |
| Cyclic object fixture | Shallow summaries and bounded explicit paths terminate without recursive graph export. |
| Save switch / reload | Old queued requests fail; new queries carry a new session identifier. |
| Invalid / oversized input | Documented JSON error or WebSocket close; no unlimited fragment buffer. |
| Multiple clients / slow consumer | Bounded admission and output; normal clients recover after pressure is removed. |
| Timeout / disconnect / stop | No double completion, leaked slots, stale game references, or Unity-thread wait deadlock. |
| Clean installation | All required third-party DLLs resolve without an incidental mod installation. |
| Existing statistics assist | Ctrl+F8 and automatic tab switching retain their behavior. |

Normal validation remains the narrow LiveStreamAssist build, with warnings treated as errors under the repository policy. Report unrelated pre-existing failures instead of changing other mods. API-only functionality does not require save-data persistence, a preloader, or changes to the UXAssist public API.

## Deferred work

`system.stats` deliberately stops at the placeholder. When statistics are requested, define a concrete schema before enabling `api.stats`; likely candidates are current connections, completed/failed requests, bytes sent/received, and durations in milliseconds since server start. V1 admission counters are resource controls, not an excuse to expose an undocumented metrics schema.

Subscriptions, writes, method invocation, authentication, additional root catalogs, aggregated gameplay endpoints, dictionary enumeration, and recursive object export require separate scope decisions. Do not implement extension scaffolding for them now.
