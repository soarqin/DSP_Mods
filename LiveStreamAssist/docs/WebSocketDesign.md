# LiveStreamAssist WebSocket Design and Transport Migration Plan

Status: **in-process Kestrel transport implemented; desktop regressions pass; Unity/LAN and full conformance acceptance pending**. Production code explicitly pins ASP.NET Core **2.3.13** hosting, HTTP, Kestrel Core, Sockets, and WebSockets, `System.Net.WebSockets.WebSocketProtocol` **5.1.3**, and Newtonsoft.Json **13.0.3**. Fleck is removed. The desktop probe verifies a fragmented echo round trip; production-code checks cover connection cleanup, admission, deadlines, shutdown, and loopback requests. DSP/Unity Mono, Autobahn, and LAN checks have not passed.

The next remaining work is an actual DSP run of the probe/mod, then M3 conformance. Keep the mod on `net472`. A standalone gateway is a last-resort fallback after concrete in-process failures, not the default design.

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
- The game uses Unity 2022. Standard-library APIs, selected NuGet assets, and the entire runtime dependency graph must work in its actual Mono player.
- Prefer an in-process implementation. Consider a standalone gateway only when maintained, compatible in-process options cannot satisfy the compatibility and conformance gates.
- Retire Fleck and its internal-handler workaround. Do not carry them forward as the normal transport or as an automatic runtime fallback.

Keep the implementation small: one listener, a fixed method switch, fixed root providers, a bounded main-thread dispatcher, and shallow result projection. Use only the hosting services required by Kestrel; do not refactor the mod's feature architecture into a dependency-injection framework. No MVC, SignalR, REST API, web dashboard, general-purpose reflection service, or custom WebSocket framing implementation is required.

The initial listener configuration and all wire limits are defined in [API connection settings](WebSocketApi.md#connection) and [API limits](WebSocketApi.md#limits). Keep those tables as the single documentation source for their values. Configuration is startup-only in v1; do not introduce live configuration reload as part of this work.

## Starting or resuming implementation

1. Read the repository `AGENTS.md`, this document, and the API reference before editing code.
2. Inspect `git status` and the current diff. Treat existing changes as user work. Check whether some phases have already been implemented rather than assuming this document's initial status still describes the checkout.
3. Read the source entry points in the next section. Preserve the host-owned feature lifecycle and existing keyboard handling.
4. Recheck runtime-specific assumptions against the installed game's original assemblies if DSP or Unity has changed. Do not use stripped reference DLL method bodies as evidence.
5. Complete the migration phases in dependency order. M0 must pass before adopting a replacement in production code. Mark checks complete only after running them; an unavailable DSP or LAN test is blocked, not passed and not proof that an in-process solution is impossible.
6. If pausing between sessions, update this document's current status, remaining phase checks, selected dependency versions, and concrete blockers. Replace stale notes instead of appending a chronological transcript. Keep project-specific guidance here; `AGENTS.md` should contain only a link to this document.

The API reference is the source of truth for method names, message shapes, numeric error codes, null behavior, and capability names. Resolve any conflict with this plan before implementing it. The migration replaces transport and repairs its integration; it does not restart the reflection feature from scratch or expand the API scope. Do not preserve an implementation bug by weakening the documented contract.

## Transport decision and evidence

Research baseline: **2026-09-17**. The preferred candidate is the **serviced ASP.NET Core 2.3 package line for .NET Framework**, using Kestrel's managed socket transport and its WebSocket middleware in the game process. This is a package-based `netstandard2.0` route, not a requirement to load a modern .NET runtime into Unity.

Microsoft explicitly lists Kestrel, its socket transport, and WebSocket middleware in the [supported 2.3 package set][aspnet-support]. Its [servicing advisory][aspnet-advisory] distinguishes this line from unsupported ASP.NET Core 2.1/2.2 packages and from running old .NET Core runtimes. The reviewed [WebSockets 2.3.13][websockets-package] and [Sockets transport 2.3.13][sockets-package] packages publish `netstandard2.0` assets and September 2026 servicing releases. Recheck the latest supported patch when implementing; patch numbers need not be identical across all packages.

The [reviewed middleware source][middleware-source] obtains an opaque stream through Kestrel's `IHttpUpgradeFeature` and constructs the WebSocket through the protocol dependency. The [socket transport source][sockets-source] uses the managed socket transport. This avoids the game's unimplemented `HttpListenerContext.AcceptWebSocketAsync` path.

Important qualifications:

- Microsoft's support statement concerns ASP.NET Core on .NET Framework; it is **not a certification of Unity's Mono player**. M0 still requires an actual DSP run.
- This is a serviced compatibility line, not the current ASP.NET Core feature line. The required standard is the [API's RFC 6455/HTTP/1.1 profile](WebSocketApi.md#websocket-interoperability), not a claim of HTTP/2, HTTP/3, compression, or future extension support.
- The transitive [WebSocketProtocol package][protocol-package] labels its standalone factory API obsolete. Do not build a new handshake stack around that API. Use the maintained middleware's existing integration, review the exact resolved protocol package and advisories, and verify which concrete WebSocket implementation is loaded. A package's Microsoft ownership alone is not sufficient acceptance evidence.

### Candidate disposition

| Candidate | Disposition and evidence |
| --- | --- |
| ASP.NET Core 2.3 + managed Kestrel Sockets + WebSockets | First candidate for M0: serviced packages and a compatible declared API surface. Actual Mono loading, protocol behavior, and resource bounds remain to be tested. |
| Fleck 1.2.0 | Retired by the user's requirement. The reviewed [repository head][fleck-source] is a 2021 core commit; do not imply the repository is formally archived. The retired handler wrapper did not enforce a message-level limit. |
| TouchSocket.Http 4.3.x | Maintained and publishes compatible targets, but not an approved drop-in. The [reviewed 4.3.6 frame parser][touch-frame] allocates from the advertised payload length before an application callback; the [framework-target project][touch-project] also references `System.Web`. The [4.3.7 package][touch-package] is newer than the reviewed source snapshot. Reconsider only with matching released source and passing gates, without a private framing fork. |
| SuperSocket.WebSocket.Server 2.1.0 | The [current package][supersocket-package] requires .NET 6 or later, so it is not a compatible in-process replacement for this `net472` mod. Do not select an old preview merely to obtain a different TFM. |
| A separate supported .NET LTS gateway | Conditional fallback only; see [Fallback boundary](#fallback-boundary). It adds packaging, IPC, and process-lifetime responsibilities and must not be introduced just because game testing is unavailable. |

### Unity and dependency compatibility rules

The [Unity 2022.3 documentation][unity-profile] describes .NET Standard 2.1 and .NET Framework API compatibility profiles and explicitly excludes .NET Core-targeted managed plugins. The installed player, not a Unity Editor setting in a different project, is the runtime to validate.

1. Keep `LiveStreamAssist.csproj` and its UXAssist reference on the existing `net472` path. For the replacement, select actual `netstandard2.0` or compatible .NET Framework assets, including transitive dependencies. Unity's support for Standard 2.1 does not make a Standard-2.1-only package a valid SDK project reference from `net472`.
2. Do not reference `net6.0`, `net8.0`, or `net10.0` implementation DLLs in the mod. Do not add a `Microsoft.AspNetCore.App` framework reference, switch to `Microsoft.NET.Sdk.Web`, or retarget shared mods to make an incompatible package restore.
3. Package version and target framework are different facts. A dependency such as `System.IO.Pipelines` or `Microsoft.Extensions.Options` can have an 8.x version and still supply a compatible asset; inspect the selected asset path and its APIs instead of deciding from its version number.
4. Inspect `project.assets.json`, resolved runtime copy items, assembly references, and the actual game load results. Pay particular attention to `System.Memory`, `System.Buffers`, `System.Threading.Tasks.Extensions`, `System.Runtime.CompilerServices.Unsafe`, `System.IO.Pipelines`, and `Microsoft.Extensions.*`, including conflicts with other installed mods.
5. Never replace the game's `mscorlib`, `System`, `System.Core`, `netstandard`, Unity assemblies, or framework facades to make the host start. Do not ship reference assemblies from a package's `ref` directory. Conversely, do not discard every `System.*` DLL: reviewed package implementations such as `System.Net.WebSockets.WebSocketProtocol.dll` may be required runtime dependencies.
6. Use the classic `WebHostBuilder`/`IWebHost` hosting surface and `WebSocket` array-segment/task APIs available to the selected target. Avoid APIs copied from modern examples, such as `WebApplication.CreateBuilder`, `Task.WaitAsync`, or memory-based WebSocket overloads absent from the compile/runtime surface. Use ordinary task cancellation/timeouts where necessary.
7. A successful desktop .NET Framework build or console run is only an early check. `MissingMethodException`, `TypeLoadException`, `NotImplementedException`, or unsupported socket behavior in the actual player fails M0. Do not conceal such failures with warning suppression, private-reflection patches, or global assembly-resolution hooks.

Prospective direct dependencies are the supported 2.3.x hosting/Kestrel components, `Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets`, and `Microsoft.AspNetCore.WebSockets`, plus the existing Newtonsoft.Json dependency. Prefer explicit components if a convenience package brings unused transports. Select the latest compatible serviced protocol dependency as well; do not let a minimum transitive constraint silently pin an obsolete patch. Record and lock the exact approved graph after M0.

## Repository entry points

Paths in this table are relative to the repository root.

| File / symbol | Why it matters |
| --- | --- |
| `LiveStreamAssist/LiveStreamAssist.cs` / `LiveStreamAssist.Awake` | BepInEx plugin startup; performs localization setup and feature discovery. Bind API configuration before discovery. |
| `LiveStreamAssist/LiveStreamAssist.cs` / `LiveStreamAssistFeature` | Existing feature, order 50; owns Ctrl+F8 and statistics-window switching. Its `_enabled` flag is not the server-enabled flag. |
| `LiveStreamAssist/LiveStreamAssist.csproj` | Inherits `net472`, currently references UXAssist, and owns the mod version. Add only the server's required dependencies and project-scoped packaging customization here. |
| `LiveStreamAssist/Api/WebSocketApiServer.cs` | Kestrel host, `ApiConnection`, and `PendingRequest`; owns transport lifetime, admission, bounded I/O, and host-error logging. |
| `LiveStreamAssist/Api/MainThreadDispatcher.cs` | Current game-work queue and completion path; inspect physical queue bounds and timeout behavior as well as frame budgets. |
| `LiveStreamAssist/Api/ApiProtocol.cs` | Reuse the JSON-RPC contract and system methods; review serializer concurrency and remove transport-specific raw-socket limits. |
| `LiveStreamAssist/tools/ReflectionReaderCheck/` | Existing fixture runner. Preserve its coverage; it does not prove WebSocket conformance or game-runtime compatibility. |
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

### Transport integration invariants

Keep these rules when changing the transport. The desktop harness exercises the production classes with controlled work scheduling and sockets; it does not replace the Unity acceptance gate.

| Code path | Required behavior |
| --- | --- |
| `ReceiveLoop` | Bound complete-message payloads, preserve fragmentation state across empty fragments, and cancel stalled partial reads independently of the next receive completion. |
| `ApiConnection.NotifyDisconnected` / `Dispose` | Cancel I/O and wake an idle sender, clear queued output, and release every admission exactly once. Overflow must use this cleanup path rather than setting the terminal flag early. |
| `TryAdmit` / `DeliverAsync` / `RunSendLoop` | Retain IDs and admission slots through delivery. Include active sends in output capacity, and deduct queued time from the send deadline. Invalid requests with a reliable ID still participate in duplicate-ID checks. |
| `CloseAsync` / `ReceiveLoop` | Serialize close output with application sends; leave peer-close reception to the single receive loop. Bound the handshake and abort stalled I/O. |
| `MainThreadDispatcher` | Watch queued and executing work, bound the physical queue, and remove canceled entries. Completion continuations must not serialize inline in the game-work producer. |
| `StartAsync` / `StopAsync` / `HandleHttp` | One startup/shutdown task per owner, no resurrection after stop, no disposal racing startup, and concrete connection reservations that can be canceled during upgrade. |
| `WebSocketApiFeature` | Bind clocks and failure callbacks to their originating owner, publish immutable snapshots with cross-thread visibility, and invalidate only old-session work. |
| `ApiProtocol` / `ReflectionReader` | Use per-operation serializers, reject explicitly null typed parameters, and apply runtime/declaration type boundaries consistently to reads, descriptions, and projections. |
| `tools/Test-WebSocketApi.ps1` | Use per-operation deadlines, strict-mode-safe optional access, bounded reads, and cleanup after failed upgrades. Require an actual HTTP rejection rather than treating every connection failure as a passed route test. |

## Code organization

Use these responsibilities as the starting layout; keep small private helpers with their owner rather than creating an interface or service for each step.

| File | Responsibility after migration |
| --- | --- |
| `LiveStreamAssist/Api/WebSocketApiFeature.cs` | Preserve configuration, feature order 51, Unity-context capture, version publication, sessions, and root providers; coordinate asynchronous host lifetime. |
| `LiveStreamAssist/Api/WebSocketApiServer.cs` | Private Kestrel host and `System.Net.WebSockets.WebSocket` connection loops, bounded admission, serialized sends, and cleanup. Keep framework-specific types here. |
| `LiveStreamAssist/Api/ApiProtocol.cs` | DTOs, error constants, validation, fixed method dispatch, system methods, and the API version constant. |
| `LiveStreamAssist/Api/ReflectionReader.cs` | Shared path resolution, allowed-member discovery, value projection, and detached result construction. |
| `LiveStreamAssist/Api/MainThreadDispatcher.cs` | Physically bounded game-work queue, one scheduled pump, cancellation, and detached completion; no JSON serialization or socket work. |
| `LiveStreamAssist/tools/Test-WebSocketApi.ps1` | Runnable WebSocket integration checks using PowerShell 7 and `System.Net.WebSockets.ClientWebSocket`. |
| `LiveStreamAssist/tools/WebSocketTransportCheck/` | Desktop `net472` echo probe and production transport/dispatcher regression checks. Keep test-only echo behavior out of the production API. |

Bind the three API settings in plugin startup before `ModFeatureRegistry.Discover`. Use a separate `[ModFeature]` class for the server. Do not have LiveStreamAssist call registry dispatchers, add a second shared update driver, relax UXAssist's menu/typing guards, or attach server activation to Ctrl+F8.

Keep Newtonsoft.Json for the existing API encoding. Do not migrate the JSON contract to SignalR or a different serializer as part of replacing Fleck. No new general-purpose transport factory or public abstraction is needed; isolate the selected framework at the existing server boundary.

## Runtime design

### Lifecycle and metadata

- `Init`: prepare configuration references and non-network state. Keep initialization safe if the API is disabled.
- `Start`: if enabled, capture `SynchronizationContext.Current` on Unity's main thread, initialize a server owner instance, subscribe to game lifecycle events, and start the listener without blocking the game loop. If the current context is null or is not Unity's installed context, fail startup with a useful log; a newly constructed generic `SynchronizationContext` is not a main-thread dispatcher.
- Build/start the private `IWebHost` on a worker and observe its startup task. Initialize owner/admission state before requests can arrive. Use explicit configuration from the mod rather than process-global URL settings, console lifetime, or unrelated hosting-startup discovery. Do not call `Run()` or terminate the game process when the host stops.
- Publish version data from `GameLogicProc.OnDataLoaded`. If starting after preload, use the original preload completion flags to populate it immediately. Publish an immutable snapshot; network callbacks never read `GameConfig` or Unity state directly. Preserve this snapshot across save changes.
- Source `modVersion` from generated `PluginInfo.PLUGIN_VERSION`. Keep `apiVersion` independent, initially `1.0.0`. Use the explicit `System.Version` name if parsing the API version to avoid DSP's global `Version` type.
- Subscribe to `OnGameBegin` and `OnGameEnd` for session transitions. Account for starting after a game has already begun.
- `Uninit`: stop admitting work, invalidate the owner/session, cancel requests, unsubscribe events, close the listener, and close connections. Clear metadata caches owned by the server. Posted callbacks must check their old owner token so they cannot run against a restarted instance.
- Startup errors, including an occupied port or invalid settings, disable only this server instance and are logged. Do not silently bind a different address/port or let an exception escape into other features' startup/shutdown.
- Start/stop and cleanup must be idempotent. Never synchronously wait on a task whose completion needs the Unity thread during shutdown.
- Bound and observe `StopAsync`; dispose the host off the Unity thread, including startup-failure paths. A stopped or superseded owner cannot resume startup and leave a listener behind. Keep lifecycle/event-state transitions coordinated with the existing main-thread feature owner.
- Forward Kestrel warnings/errors to the mod logger so runtime failures before the API handler are visible. The host must not require process-global logging configuration.

### Kestrel connection and receive loop

- Explicitly select the managed socket transport and the configured IP endpoint. Do not use `HttpListener`, HTTP.sys, IIS integration, or Libuv. Use the supported WebSocket middleware for handshake validation and the opaque-stream upgrade; do not recreate handshake or frame parsing in mod code.
- Restrict the route to `/api/v1` before upgrade. Return an HTTP rejection for other paths, invalid handshakes, or exhausted upgrade capacity. Bound ordinary HTTP connections, upgraded connections, request-header size, and header-read time using supported host options. A Kestrel HTTP request-body limit is not a WebSocket message limit.
- Await the connection handler for the lifetime of the WebSocket so the HTTP request pipeline does not dispose the upgraded stream prematurely. Each connection owns one receive loop, one serialized application send loop, and cancellation linked to server shutdown and client disconnect.
- Read through the target-compatible `ReceiveAsync(ArraySegment<byte>, CancellationToken)` API. Accumulate at most `maxRequestBytes`, checking before each append; deliver one complete text message only when `EndOfMessage` is true. Handle an empty message as a JSON parse failure, not as a disconnect. Give an incomplete message an operation deadline once it starts, while allowing an otherwise idle connection to remain open.
- Let the maintained protocol implementation handle masking, RSV/opcode validation, fragmentation state, ping/pong, and close parsing. Use strict UTF-8 decoding for the bounded completed text. Do not install another private-handler hook or write a frame parser to compensate for failed conformance.
- Keep compression/extensions disabled for the v1 profile. Preserve binary-message rejection (`1003`), invalid UTF-8 (`1007`), invalid framing (`1002`), oversized messages (`1009`), and application policy errors (`1008`) as distinct outcomes.
- Await and observe sends. Apply a deadline covering both queued time and the active send; use a bounded close attempt followed by `Abort`/disposal if the peer cannot drain. Do not send reserved close status values, swallow a failed send and continue, or launch overlapping application sends.

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
       -> asynchronous completion signal only
  -> worker serializes within the output cap
  -> serialized send; release admission only after delivery/cancellation
```

- Network receive, JSON parsing, serialization, and socket sends stay off the Unity thread. Avoid accidentally capturing Unity's context in network async continuations.
- Only one pump callback may be scheduled per server owner at a time. Process work within the API's per-frame count and soft-time budget, then repost remaining work. Do not busy-wait, spin, or post one unbounded callback per request. Track the Unity frame for the budget if the context is pumped more than once in a frame.
- Use a standard queue with a small lock or `ConcurrentQueue` with correct admission accounting. A custom scheduler or lock-free queue is unnecessary. If using task completions, run continuations asynchronously so response work does not execute inline in the pump.
- A request owns one admission slot until response delivery or cancellation. Capacity includes outgoing responses. Serialize sends per connection, bound the send queue, and close slow consumers rather than retaining unlimited output.
- Distinguish response production from terminal delivery: `admitted -> result/error produced -> response sent or connection canceled`. Keep the ID reserved through the last step. A task completion source with asynchronous continuations is sufficient; do not serialize or release the slot inside its main-thread producer.
- Expiry is based on monotonic elapsed time, not `Time.time` or game ticks. A worker-enforced production deadline must fire even when Unity stops pumping. Check expiry before reading as well. Once timed out, work cannot later emit success. A deadline does not abort a getter halfway through; the read policy must keep getters short and bounded.
- Reuse the documented timeout duration for a separate bounded send deadline beginning at response enqueue, including the active send. A send timeout closes/aborts the connection; it must not interleave a second error envelope into a partly transmitted response.
- Connection cancellation removes or cheaply skips queued work and releases its slot exactly once. Session invalidation and production timeout instead produce the documented error; retain the ID and slot until that error is sent or the connection is canceled. Use one terminal cleanup path so competing outcomes cannot double-complete or leak admission counts.
- Enforce a physical game-work queue bound even after canceled requests release network admission. A queue full of stale entries is not permission to allocate another unbounded queue or task list. Bound unslotted parse/busy-error responses as well.
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

## Migration phases

Checkmarks describe the completed work below; stated desktop results do not close the separate Unity/LAN gates. Keep evidence for the tested package graph and actual game runtime with the current phase status.

### M0: prove the in-process compatibility path

- [x] Build a small `net472` probe with the latest serviced ASP.NET Core 2.3 hosting, managed socket transport, and WebSocket packages. The researched transport/middleware baseline is 2.3.13; resolve compatible current versions for the other components and the protocol dependency instead of assuming all version numbers match.
- [x] Inspect and record the selected compile/runtime assets and transitive closure, including the concrete protocol assembly. Check advisories and the upstream support statement, not just the package title or a computed NuGet compatibility list.
- [x] Prove that the probe uses managed Kestrel Sockets and the maintained middleware upgrade path, with no fallback to the game's `HttpListener` implementation, Libuv, HTTP.sys, or a hand-written handshake/parser.
- [ ] Run the same candidate in actual DSP/Unity 2022 Mono. Record game/Unity versions, architecture, loaded assembly identities/locations, and the WebSocket implementation type. Test both a clean supported installation and coexistence with the normal mod set. **Blocked: DSP was not launched.**
- [ ] Verify a real opening handshake, fragmented text with interleaved ping, exact ping/pong payload behavior, a bounded oversized input, and closing/releasing the listener. An advertised huge frame length must not trigger an allocation proportional to that length before the application can enforce its limit. **Desktop checks cover handshake, fragmented round trips, payload limits, and listener release. Interleaved ping/pong and huge advertised lengths remain unverified.**
- [ ] Confirm an idle API, startup failure, and normal shutdown do not disturb Ctrl+F8 or the shared feature lifecycle. Verify a port already in use fails softly. **Blocked: DSP.**
- [x] Record a go/no-go decision and the exact graph to adopt. If a compatible package asset is missing or an actual Mono API fails, record the concrete failing call/assembly rather than suppressing it.

Desktop probe (repository root):

```powershell
dotnet build LiveStreamAssist/tools/WebSocketTransportCheck/WebSocketTransportCheck.csproj -c Release
& LiveStreamAssist/tools/WebSocketTransportCheck/bin/Release/net472/WebSocketTransportCheck.exe
& LiveStreamAssist/tools/WebSocketTransportCheck/bin/Release/net472/WebSocketTransportCheck.exe --regression
```

Observed desktop identities: Kestrel.Core **2.3.13**, Transport.Sockets **2.3.13**, WebSockets middleware **2.3.13**, protocol type `System.Net.WebSockets.ManagedWebSocket` from `System.Net.WebSockets.WebSocketProtocol` **5.1.3**. Microsoft.Extensions.* **8.x** assets selected as `lib/net462`. Newtonsoft.Json **13.0.3**. No Libuv package. The harness references the production project, and all 34 transport/hosting package versions match its lock file. Unity/Mono adoption remains unconfirmed.

The desktop regression harness loads Unity type metadata but runs on the Windows CLR. Its test-only `App.config` binds `netstandard` to the desktop 2.0 facade; Unity's Mono 2.1 type forwarders are not interchangeable with CLR forwarders. This configuration and the copied test host/game assemblies are not mod package contents and do not establish Mono compatibility.

Exit condition: the candidate actually starts, exchanges bounded messages, and disposes under the game's runtime using the intended managed assemblies. Desktop-only tests or an unavailable DSP launch leave M0 blocked. Investigate supported in-process alternatives before crossing the fallback boundary.

### M1: replace the Fleck transport

- [x] Replace the listener and connection implementation at `WebSocketApiServer.cs` using the M0-approved graph. Keep framework-specific hosting types at this boundary.
- [x] Implement explicit endpoint/path handling, bounded pre-upgrade and upgraded connection admission, and a correctly awaited WebSocket connection lifetime.
- [x] Implement the single receive loop and bounded complete-message assembly using the standard WebSocket API. Remove all Fleck references, `BoundedHandler`, handler casts/reflection, and `MaxIncomingSocketBytes`.
- [x] Coordinate asynchronous host startup/stop with the existing feature owner and cancellation token. Dispose partial startup instances and prevent late callbacks from resurrecting stopped state.
- [x] Retain API version `1.0.0`, the existing configuration keys/defaults, seven methods, root providers, reflection policy, game-session identifiers, numeric encoding, and statistics placeholder. Do not add authentication or subscriptions.

Exit condition: the production request path no longer uses Fleck or a private frame implementation, and the original API requests work through the new host. This does not yet establish all race/resource guarantees.

### M2: repair completion, accounting, and deadlines

- [x] Separate detached result production from delivery. No Unity-thread completion path may serialize JSON or enter a socket send. Verify thread affinity rather than assuming an async method automatically moves work to another thread.
- [x] Keep request IDs and per-connection/global slots until the response is sent or the connection is canceled. Test success, error, timeout, disconnect, and stop races for exactly-once slot release. **Desktop held-send, overflow, timeout, and disconnect checks pass; in-game transitions remain pending.**
- [x] Enforce response-production deadlines on a worker while the Unity pump is intentionally blocked. Do not cancel the receive loop merely because one game-data request times out.
- [x] Enforce queued and active-send deadlines, observe failed tasks, and bound close attempts before aborting failed transports. Include the active send in queue/memory accounting.
- [x] Put a hard bound on the physical game-work queue and remove/prune canceled entries. Repeated timeouts while the game loop is stopped must not accumulate stale work without limit.
- [x] Make response serialization safe under concurrency while preserving the byte cap and existing JSON encoding.
- [x] Preserve session-generation checks and invalidate queued old-save work. Already produced/in-transit snapshots remain tagged with the sampling session; never claim they can be recalled.
- [x] Retain and rerun the reflection fixture checks. Add focused deterministic checks for these transport-integration defects using held completions/fake work scheduling, not production-only debug endpoints. **Reflection fixtures and the production `--regression` harness pass.**

Exit condition: a blocked Unity frame does not block system methods or timeout responses; all queues remain bounded; slots and IDs survive until terminal delivery/cancellation; no late success follows a timeout.

### M3: conformance and game integration

- [ ] Run a pinned [Autobahn Testsuite][autobahn] configuration in `fuzzingclient` mode against a test-only echo host using the same approved transport packages and configuration path. Use its isolated toolchain image and record the image digest, case results, and justified exclusions. Do not expose an echo method on the production JSON-RPC endpoint. Probe `--echo` exists; Autobahn was not run.
- [ ] Test opening-handshake validation separately: HTTP method/version, required Upgrade/Connection tokens, WebSocket version/key, invalid paths, capacity rejection, and oversized or stalled headers. Autobahn's documented opening-handshake coverage is incomplete.
- [ ] Cover masking direction, RSV/reserved opcodes, continuation ordering, interleaved control frames, control-frame FIN/size rules, invalid length encodings, UTF-8 across fragments, close status/reason validation, and ping/pong payload equality.
- [ ] Test application limits separately with the real v1 settings: exact boundary and boundary-plus-one payloads, byte-by-byte fragmentation, multiple messages in one socket read, interleaved pings, and huge advertised lengths. Do not mistake an Autobahn echo expectation for a requirement to accept application payloads above 64 KiB.
- [ ] Use a raw-socket/fuzzing test tool for invalid frames; `ClientWebSocket` intentionally does not generate many malformed frame cases. Keep malformed-frame generation in test tooling, not production transport code.
- [x] Repair the PowerShell smoke harness: per-operation cancellation, response-ID matching, optional-field checks under strict mode, buffer/socket cleanup, and HTTP handshake-rejection handling. Move duplicate-outstanding-ID checks to a held-request harness so a legitimately completed first request does not create a timing-dependent failure.
- [ ] Run no-save and active-save API checks, then real-game pause/text-input, loading, space travel, save switch/reload, and Ctrl+F8 regressions. Run a LAN client from a second machine and a current browser/OBS browser source. **Blocked: DSP.**
- [ ] Exercise multiple clients and slow/non-reading consumers, blocked game work, malformed input followed by valid input, port conflicts, repeated start/stop, and shutdown during startup/read/send.

Desktop production checks pass for fragmented JSON, binary/invalid-UTF-8/oversized rejection (`1003`/`1007`/`1009`), duplicate IDs (`1008`), shutdown (`1001`), blocked-pump timeout delivery, stalled fragments, occupied ports, and startup/stop races. Full framing conformance, multi-client stress, and actual game/LAN scenarios remain open.

Autobahn proves the covered transport behavior, not the reflection API or Unity scheduling. Compression/other unsupported extensions and application size limits must be accounted for explicitly. A partial run, waived core-protocol failure, or test against a different runtime/package build is not a full pass. Seek an upstream fix for a required protocol defect; do not reconstruct a private frame layer to turn the report green.

Exit condition: the applicable RFC 6455 cases and application/resource tests pass with recorded evidence, and the actual Unity/Mono plus LAN integration matrix passes. No unmeasured FPS, latency, or future-standard guarantee is implied.

### M4: packaging and maintained delivery

- [x] Replace the two-DLL Fleck packaging list with the audited runtime closure of the approved host, while retaining project-local packaging customization.
- [x] Pin the approved package versions and record the resolved graph, selected TFMs, and source/support references. Use a project-local package lock and locked restore for repeatable validation once the graph is approved; do not change dependency policy for unrelated mods.
- [x] Fail packaging when a required runtime dependency is absent. Do not silently drop mandatory files behind `Exists` conditions, copy the entire build output, or ship reference/game/framework assemblies.
- [x] Verify the final assembly references and ZIP contain no Fleck dependency or leftover handler shim. Remove stale Fleck files only from LiveStreamAssist-owned staging/install locations; other mods may legitimately use their own copies.
- [ ] Test the packaged mod on a clean installation containing the declared BepInEx/UXAssist dependencies, then with the ordinary mod set. Do not depend on a developer machine's shared .NET runtime or incidental plugin DLLs. **Blocked: DSP.**
- [x] Update API/README status only after the documented gates pass. Record the approved graph, exact probe/conformance commands, runtime evidence, and any remaining blocker in this document. Keep `AGENTS.md` as a link only. **Status records remaining Unity/LAN blockers; do not treat this as a full gate pass.**
- [ ] Run the narrow build/fixture/smoke checks and `git diff --check`; review the diff for unrelated changes. **Release builds, fixtures, desktop transport checks, and ZipMod pass. The PowerShell script parses successfully; its game/LAN run is still pending.**

Use the existing project-local `BeforeTargets="ZipMod"` extension point and `_PackRootFiles`, driven by an audited dependency list or filtered resolved-runtime items. Include required supporting implementations such as the approved protocol/buffer packages, while excluding game references, Unity/BepInEx assemblies, and UXAssist itself. Record why each packaged assembly is needed; a `System.*` filename alone neither includes nor excludes it.

### Maintenance policy

- Review upstream support, servicing releases, and package advisories before a mod release and when an advisory affects shipped dependencies. Old release dates alone do not prove abandonment, and frequent releases alone do not prove conformance.
- Review updates within the supported compatibility line. Do not automatically upgrade package majors or switch to a `net6.0+` asset because it has a higher version number. Avoid floating transport versions in a released build.
- For every runtime-dependency update, inspect the graph/TFM diff, run the transport/protocol regressions and Unity smoke tests, and retain the conformance configuration/results. Rerun the wider game/LAN matrix when runtime behavior or hosting changes.
- Prefer upstream fixes and serviced packages over local protocol patches. Track a discovered defect with a minimal reproducer and regression case. If a necessary fix cannot be obtained on a supported compatible line, reopen the selection decision instead of permanently freezing an unmaintained fork.
- Keep compiler/audit policy intact. Document an actual compatibility limitation rather than suppressing it to obtain a green build. This process reduces maintenance risk; it does not promise bug-free dependencies.

## Fallback boundary

The user requested in-process investigation first. A standalone gateway becomes an option only after recording concrete failures of the preferred route and reasonable maintained in-process alternatives: unavailable Mono APIs, an unresolvable assembly-identity conflict, required protocol defects without a supported fix, or loss of upstream support. Missing access to DSP, a failed search, or extra package DLLs is not such evidence.

If this boundary is reached, revise the architecture before implementation: a supported .NET LTS process can own Kestrel/WebSockets while the Unity `net472` mod continues to own reflection and main-thread snapshots. The public API should remain compatible. Specify bounded local IPC, connection/request routing, deadlines, session invalidation, startup failure, parent/child exit cleanup, packaging, and runtime servicing in that revision. Modern .NET assemblies must remain in the separate process, never loaded into Unity. Do not scaffold or ship this fallback alongside a working in-process host.

## Validation commands and acceptance matrix

Run commands from the repository root. These target the existing mod and tools; their inclusion does not mean the replacement or runtime checks have passed. When adding the M0 probe and M3 conformance harness, record their exact build/run commands, test configuration path, and pinned toolchain version here.

Before the integration commands, install the built mod and its required DLLs, set `WebSocketApi.Enabled` to true, and restart DSP. Run no-save checks at the main menu, then open a save for `-RequireGame`. For the LAN check, configure the actual LAN bind address or `0.0.0.0` before restarting and connect from the second machine to the server's real LAN address, not to `0.0.0.0`.

```powershell
dotnet restore LiveStreamAssist/LiveStreamAssist.csproj --locked-mode
dotnet build LiveStreamAssist/LiveStreamAssist.csproj -c Release --no-restore
dotnet build LiveStreamAssist/tools/WebSocketTransportCheck/WebSocketTransportCheck.csproj -c Release
& LiveStreamAssist/tools/WebSocketTransportCheck/bin/Release/net472/WebSocketTransportCheck.exe
& LiveStreamAssist/tools/WebSocketTransportCheck/bin/Release/net472/WebSocketTransportCheck.exe --regression
dotnet build LiveStreamAssist/tools/ReflectionReaderCheck/ReflectionReaderCheck.csproj -c Release
& LiveStreamAssist/tools/ReflectionReaderCheck/bin/Release/net472/ReflectionReaderCheck.exe
pwsh -NoProfile -File LiveStreamAssist/tools/Test-WebSocketApi.ps1 -ServerUri ws://127.0.0.1:18080/api/v1
pwsh -NoProfile -File LiveStreamAssist/tools/Test-WebSocketApi.ps1 -ServerUri ws://127.0.0.1:18080/api/v1 -RequireGame
dotnet build LiveStreamAssist/LiveStreamAssist.csproj -t:ZipMod -c Release --no-restore
git diff --check
```

The fixture tools copy Unity metadata/facades from the installed game; the transport harness also copies BepInEx for desktop type loading. These copies are test-only. Use locked restore for repeatable validation; dependency updates require an explicit graph review and lock update. The packaged ZIP contains 49 declared runtime dependencies plus the plugin, manifest, README, and icon. Neither tool binaries/configuration nor game/core-framework/host assemblies belong in it.

The smoke script must accept those parameters, require no WebSocket client package, close its sockets in cleanup, and exit nonzero when an assertion fails. `-RequireGame` fails clearly if a save is not ready; no-save mode skips only game-dependent assertions, not protocol/system checks. Repeat the relevant script from a second machine with the actual LAN address. A loopback-only run does not establish LAN acceptance.

Do not use game-mutating debug endpoints to make checks pass. If a particular research ID is needed, discover it from current research, the research queue, or known game data; do not assume the API example's `1001` is always active.

| Scenario | Required observation |
| --- | --- |
| Unity/Mono dependency loading | The reviewed runtime assets load from the intended locations without missing APIs, assembly conflicts, or framework replacement. |
| Default configuration | API disabled, no listener, existing assist still works. |
| Opening handshake / RFC 6455 | Valid clients connect; invalid handshakes and framing fail as specified; applicable conformance cases pass with recorded results. |
| Preload / main menu | Ping works; version readiness is explicit; menu demo is not an available player root. |
| Active save | Roots, metadata, research reads, projections, and pages are correct. |
| Pause / text input | Data reads still complete through the Unity context. |
| Space travel | Absent local roots produce `ROOT_UNAVAILABLE`, not a crash or false whole-game failure. |
| Large integer fixture | `Int64`/`UInt64` values beyond JavaScript's safe range round-trip as decimal strings. |
| Forbidden getter / infrastructure | Every resolver path enforces the same policy without invoking the denied code. |
| Cyclic object fixture | Shallow summaries and bounded explicit paths terminate without recursive graph export. |
| Save switch / reload | Old queued requests fail; new queries carry a new session identifier. |
| Invalid / oversized input | Documented JSON error or WebSocket close; no unlimited fragment buffer. |
| Fragmentation / control frames | Payload accounting survives interleaved pings and TCP chunking; huge declared lengths cannot force proportional allocation. |
| Multiple clients / slow consumer | Bounded admission and output; normal clients recover after pressure is removed. |
| Blocked Unity pump | System methods and production deadlines still work; stale game-work entries remain bounded. |
| Active send / ID reuse | IDs and slots stay reserved through delivery; send failure or timeout aborts without a second response or leaked capacity. |
| Timeout / disconnect / stop | No double completion, leaked slots, stale game references, or Unity-thread wait deadlock. |
| Startup / shutdown races | Occupied-port failure is isolated; stopping during startup leaves no orphan listener or late owner callbacks. |
| Clean installation | All required third-party DLLs resolve without an incidental mod installation. |
| Existing statistics assist | Ctrl+F8 and automatic tab switching retain their behavior. |

Normal validation remains the narrow LiveStreamAssist build, with warnings treated as errors under the repository policy. Report unrelated pre-existing failures instead of changing other mods. API-only functionality does not require save-data persistence, a preloader, or changes to the UXAssist public API.

## Deferred work

`system.stats` deliberately stops at the placeholder. When statistics are requested, define a concrete schema before enabling `api.stats`; likely candidates are current connections, completed/failed requests, bytes sent/received, and durations in milliseconds since server start. V1 admission counters are resource controls, not an excuse to expose an undocumented metrics schema.

Subscriptions, writes, method invocation, authentication, additional root catalogs, aggregated gameplay endpoints, dictionary enumeration, and recursive object export require separate scope decisions. Do not implement extension scaffolding for them now.

[aspnet-support]: https://dotnet.microsoft.com/en-us/platform/support/policy/aspnet/2.3-packages
[aspnet-advisory]: https://devblogs.microsoft.com/dotnet/servicing-release-advisory-aspnetcore-23/
[websockets-package]: https://www.nuget.org/packages/Microsoft.AspNetCore.WebSockets/2.3.13
[sockets-package]: https://www.nuget.org/packages/Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets/2.3.13
[middleware-source]: https://github.com/dotnet/aspnetcore/blob/1db07eda0330bfc87e383d1d65a50e9f0d43c6e9/src/Middleware/WebSockets/src/WebSocketMiddleware.cs
[sockets-source]: https://github.com/dotnet/aspnetcore/blob/1db07eda0330bfc87e383d1d65a50e9f0d43c6e9/src/Servers/Kestrel/Transport.Sockets/src/SocketTransportFactory.cs
[protocol-package]: https://www.nuget.org/packages/System.Net.WebSockets.WebSocketProtocol
[fleck-source]: https://github.com/statianzo/Fleck/commit/45672e0781974bb04dbad1b94320756a33c60a6d
[touch-frame]: https://github.com/RRQM/TouchSocket/blob/bf377c2a1363576e8b0ecb0924e9718f0093f0dc/src/TouchSocket.Http/WebSockets/Common/WSDataFrame.cs
[touch-project]: https://github.com/RRQM/TouchSocket/blob/bf377c2a1363576e8b0ecb0924e9718f0093f0dc/src/TouchSocket.Http/TouchSocket.Http.csproj
[touch-package]: https://www.nuget.org/packages/TouchSocket.Http/4.3.7
[supersocket-package]: https://www.nuget.org/packages/SuperSocket.WebSocket.Server/2.1.0
[unity-profile]: https://docs.unity3d.com/2022.3/Documentation/Manual/dotnetProfileSupport.html
[autobahn]: https://github.com/crossbario/autobahn-testsuite
