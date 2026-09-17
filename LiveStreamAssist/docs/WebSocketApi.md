# LiveStreamAssist WebSocket API

Status: planned contract for API `1.0.0`; the WebSocket service is not implemented yet. This reference describes the intended first release, not functionality available in the current mod.

Audience: developers of live-stream overlays, OBS integrations, and other clients. Server architecture and implementation tasks are documented separately in the [design and implementation plan](WebSocketDesign.md).

## Connection

```text
ws://<host>:18080/api/v1
```

The service supports local and LAN clients without authentication. No token, login request, or application-level handshake is required. The client sends requests after the WebSocket connection opens. Other URL paths are rejected and must not execute API requests.

The planned BepInEx configuration section is `WebSocketApi`:

| Setting | Type | Default | Meaning |
| --- | --- | --- | --- |
| `Enabled` | Boolean | `false` | Start the listener when the mod starts. |
| `ListenAddress` | IP address string | `127.0.0.1` | Bind address; use a specific LAN address or `0.0.0.0` for LAN access. |
| `Port` | Integer, 1 through 65535 | `18080` | Listening port. |

Configuration changes require a game restart in v1. The server remains available when leaving a save. Reflection reads require an active player game; menu-demo data is not exposed as a player game.

Suggested client sequence:

1. Call `system.info` to read versions, capabilities, and limits.
2. Optionally call `system.validate` to check the client's requirements.
3. Call `data.roots`, then use `data.describe` to discover readable members.
4. Call `data.read` and schedule any subsequent polling in the client.

There are no subscriptions or unsolicited application messages. Normal WebSocket control frames, including ping/pong and close frames, are transport operations rather than application notifications.

## Message format

The API uses a restricted JSON-RPC 2.0 request/response format. It does not implement the full JSON-RPC feature set: batches and notification requests are deliberately rejected.

Each complete WebSocket text message contains one UTF-8 JSON object. Fragmentation is supported at the WebSocket transport layer; an individual frame is not necessarily a complete JSON message.

### Request

```json
{
  "jsonrpc": "2.0",
  "id": "request-1",
  "method": "system.ping",
  "params": {}
}
```

| Field | Required | Rules |
| --- | --- | --- |
| `jsonrpc` | Yes | Exactly `"2.0"`. |
| `id` | Yes | Nonempty string, at most 64 UTF-16 code units. Unique among outstanding requests on this connection. |
| `method` | Yes | Case-sensitive method name from this reference. |
| `params` | No | An object; omission is equivalent to `{}`. Positional parameter arrays are unsupported. |

Identifiers, root names, member names, and capability names use ordinal, case-sensitive comparison. Unknown method parameters are rejected rather than silently ignored. Duplicate JSON object property names are rejected with `PARSE_ERROR` and `id: null`. Clients must ignore additional response fields introduced by compatible API extensions.

Missing, null, numeric, or otherwise invalid IDs produce an invalid-request response with `id: null`. A message without an ID is not treated as a notification. A batch array produces one invalid-request response with `id: null`.

Reusing an outstanding ID closes the connection with code `1008` and cancels its outstanding requests, avoiding ambiguous responses. An ID may be reused after its previous response has completed, although monotonically increasing client IDs are simpler.

### Response

```json
{
  "jsonrpc": "2.0",
  "id": "request-1",
  "result": {
    "serverTimeUtc": "2026-09-17T12:00:00.0000000Z"
  }
}
```

A response contains exactly one of `result` or `error`. Responses may arrive out of request order; correlate them by `id`. An accepted request completes at most once. Disconnection or server shutdown can prevent delivery; the server does not replay requests after reconnection.

All timestamps and game values in examples are illustrative.

## Versions and capabilities

The following versions have different meanings:

| Field | Meaning |
| --- | --- |
| `jsonrpc` | Envelope format version, always `"2.0"`. |
| `apiVersion` | API semantic version, initially `"1.0.0"`. The URL selects its major version. |
| `modVersion` | Installed LiveStreamAssist plugin version. |
| `gameVersion` | Running DSP version string, without the separate build number. |
| `gameBuild` | Running DSP build number, as an integer. |

Breaking API changes require a new API major version. Compatible additions use a minor version; compatible fixes use a patch version. Raw game member names and types can change when DSP changes even if the API version does not. Discover members for the running game rather than assuming API compatibility also guarantees a particular reflection path.

The first release advertises these capability keys:

| Capability | Value | Meaning |
| --- | --- | --- |
| `reflection.read` | `true` | `data.read` is implemented. |
| `reflection.describe` | `true` | Root and member discovery are implemented. |
| `api.stats` | `false` | API statistics are reserved but not collected or returned. |
| `subscriptions` | `false` | Subscription and notification delivery are unsupported. |

Unknown capabilities are unavailable. A development build must not advertise a capability before it actually implements it.

## Limits

These are the initial v1 defaults. `system.info.limits` is the authoritative source for the connected server. Byte limits count UTF-8 payload bytes, excluding WebSocket framing. A KiB is 1024 bytes.

| `limits` field | Value | Meaning |
| --- | --- | --- |
| `maxConnections` | 8 | Simultaneous connections. |
| `maxPendingRequestsPerConnection` | 8 | Outstanding accepted requests on one connection. |
| `maxPendingRequests` | 64 | Outstanding accepted requests across the server. |
| `maxRequestBytes` | 65536 | Complete incoming message, including all fragments. |
| `maxResponseBytes` | 262144 | Complete outgoing JSON response. |
| `maxJsonDepth` | 32 | Maximum nesting of objects and arrays in a request. |
| `maxIdLength` | 64 | Request ID length in UTF-16 code units. |
| `maxPathSegments` | 16 | Segments in a reflection path. |
| `maxSelectMembers` | 32 | Members in an explicit projection. |
| `defaultPageSize` | 64 | Array/list page size when `limit` is omitted. |
| `maxPageSize` | 128 | Maximum requested array/list page size. |
| `requestTimeoutMs` | 5000 | Deadline for producing a response, measured from request admission. |
| `maxRequestsPerFrame` | 8 | Maximum game-data requests started in one Unity frame. |
| `mainThreadBudgetMs` | 2 | Soft main-thread scheduling budget per Unity frame. |

Outstanding requests include queued reads, executing reads, and responses waiting to be sent. System methods also consume outstanding-request capacity, but do not consume the game-data execution budget. A saturated server returns `SERVER_BUSY` when it can deliver an error without growing the send queue; otherwise it closes the connection with `1013`.

The main-thread budget does not preempt a running read. Deadlines are independent of game ticks and pause state. A timed-out request cannot later produce a success response. If a response cannot be delivered to a stalled client within the bounded send deadline, the connection closes instead of retaining output indefinitely.

An oversized incoming message closes the connection with `1009`. A result that would exceed the response limit returns `LIMIT_EXCEEDED`; it is not silently truncated.

## Methods

### `system.ping`

Parameters: none.

Result: `{ "serverTimeUtc": "<UTC timestamp>" }`.

Tests application-level connectivity. It does not read game objects or wait for a game frame, so it can succeed while a game-data request times out during loading. UTC timestamps use ISO 8601 round-trip formatting with a `Z` suffix.

### `system.info`

Parameters: none.

Returns versions, capabilities, and the limits listed above. It is available without an open save.

```json
{
  "jsonrpc": "2.0",
  "id": "info-1",
  "result": {
    "apiVersion": "1.0.0",
    "modVersion": "1.0.0",
    "gameVersionReady": false,
    "gameVersion": null,
    "gameBuild": null,
    "capabilities": {
      "reflection.read": true,
      "reflection.describe": true,
      "api.stats": false,
      "subscriptions": false
    },
    "limits": {
      "maxConnections": 8,
      "maxPendingRequestsPerConnection": 8,
      "maxPendingRequests": 64,
      "maxRequestBytes": 65536,
      "maxResponseBytes": 262144,
      "maxJsonDepth": 32,
      "maxIdLength": 64,
      "maxPathSegments": 16,
      "maxSelectMembers": 32,
      "defaultPageSize": 64,
      "maxPageSize": 128,
      "requestTimeoutMs": 5000,
      "maxRequestsPerFrame": 8,
      "mainThreadBudgetMs": 2
    }
  }
}
```

Before game initialization supplies version information, `gameVersionReady` is `false` and both game version fields are `null`. Once ready, `gameVersionReady` is `true`, `gameVersion` is a string, and `gameBuild` is an integer. Leaving a save does not clear this process-level version information.

### `system.validate`

| Parameter | Required | Default | Rules |
| --- | --- | --- | --- |
| `apiMajor` | Yes | None | Positive 32-bit integer. |
| `requiredCapabilities` | No | `[]` | At most 32 distinct, nonempty capability-name strings. |

```json
{
  "jsonrpc": "2.0",
  "id": "validate-1",
  "method": "system.validate",
  "params": {
    "apiMajor": 1,
    "requiredCapabilities": ["reflection.read", "api.stats"]
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": "validate-1",
  "result": {
    "compatible": false,
    "apiVersion": "1.0.0",
    "apiMajorCompatible": true,
    "missingCapabilities": ["api.stats"]
  }
}
```

`compatible` is true only when the requested API major matches and every required capability is true. `missingCapabilities` preserves request order and includes unknown capability names. A major mismatch or unavailable capability is a successful validation result with `compatible: false`, not a protocol error.

This is not authentication, game-version certification, or reflection-path validation. Calling it does not negotiate connection state or gate later requests.

### `system.stats`

Parameters: none.

The v1 result is exactly:

```json
{
  "available": false,
  "metrics": null
}
```

The method exists as a placeholder even though `api.stats` is false. It must not return fabricated zero counters. Future metrics will be specified separately before this capability becomes true.

### `data.roots`

Parameters: none.

Returns `gameReady`, `sessionId`, `gameTick`, and a `roots` array. Each root entry has `name`, `type`, and `available` fields. All root names are always listed.

| Root | Declared type | Meaning |
| --- | --- | --- |
| `game` | `GameData` | Active player-game data. |
| `player` | `Player` | Main player. |
| `history` | `GameHistoryData` | Research and progression data. |
| `statistics` | `GameStatData` | In-game statistics data, unrelated to `system.stats`. |
| `galaxy` | `GalaxyData` | Current galaxy. |
| `localPlanet` | `PlanetData` | Current local planet, if present. |
| `localStar` | `StarData` | Current local star, if present. |

Without an active player game, `gameReady` is false, `sessionId` and `gameTick` are null, and every root is unavailable. A paused player game is ready. A local planet or star can be unavailable during space travel even while `gameReady` is true.

`sessionId` is an opaque identifier for one loaded/new game session. It changes when a different session starts, including reloading the same save. `gameTick` is a decimal string. Discovery requires a main-thread turn and can time out if the game loop is blocked.

### Reflection paths

`data.describe` and `data.read` share these parameters:

| Parameter | Required | Default | Rules |
| --- | --- | --- | --- |
| `root` | Yes | None | One registered root name. Arbitrary CLR type names are not roots. |
| `path` | No | `[]` | At most 16 path segments; an empty path addresses the root itself. |

| Segment | Meaning | Example |
| --- | --- | --- |
| Nonempty string | Exact instance field or permitted property name. | `"currentTech"` |
| Nonnegative 32-bit integer | Zero-based array/list index. | `0` |
| Object containing only `key` | Exact dictionary-key lookup. | `{"key": 1001}` |

Strings are literal member names, not expressions: `"history.currentTech"` is not split at the dot. Wildcards, method calls, filters, arithmetic, and arbitrary indexer invocation are unsupported.

V1 supports one-dimensional zero-based arrays, concrete BCL `List<T>`, and concrete BCL `Dictionary<TKey, TValue>` with `Int32` or string keys. String keys are not converted to integers. Other collection implementations and arbitrary `IEnumerable` enumeration are unsupported.

Fields can be public or non-public instance fields on permitted game-data types. Static fields, compiler-generated backing fields, methods, setters, and indexer properties are not exposed. Property reads require an explicit server allowlist. The initial property allowlist contains `GameHistoryData.currentTech`; other getters are not automatically allowed merely because they are public or have no setter.

Use `data.describe` to discover the permitted surface. If inheritance hides a name, the most-derived declaration wins. Field/property traversal through runtime infrastructure such as Unity native objects, delegates, reflection objects, streams, or synchronization objects is unsupported.

Reflected values use DSP's native units and member names. The API does not localize member names, convert values to UI display units, or calculate aggregate gameplay statistics.

### `data.describe`

Parameters: `root` and optional `path` only.

Returns metadata for the addressed value:

| Result field | Meaning |
| --- | --- |
| `type` | Diagnostic CLR type name; runtime type when non-null, declared type when null. |
| `isNull` | Whether the addressed value is null. |
| `members` | Permitted members, sorted by ordinal name. Each has `name`, `type`, and `kind` (`field` or `property`). |
| `collection` | Null for non-collections; otherwise `kind` (`array`, `list`, or `dictionary`), `elementType`, `keyType`, and `count`. |
| `sessionId` | Session that supplied the metadata. |
| `gameTick` | Sampling tick as a decimal string. |

For arrays/lists, `keyType` is null. For a null collection, `count` is null. Scalars and supported BCL containers have an empty `members` array; their framework internals are not exposed. A null terminal value can still be described using its declared type.

Resolving the explicit path can read permitted getters. Listing the target's members does not evaluate those members, their getters, or collection elements. Denied members are omitted. Type names are diagnostic metadata, not instructions to load CLR types in a client.

This method requires an active game and an available root. Passing through null before the final segment fails with `NULL_PATH`.

### `data.read`

Additional parameters:

| Parameter | Required | Default | Rules |
| --- | --- | --- | --- |
| `select` | No | Omitted | 1 through 32 distinct, nonempty immediate member names for an object projection or for each object in an array/list page. No nested selection syntax. |
| `offset` | No | `0` | Nonnegative 32-bit integer; valid only when the terminal value is an array/list. |
| `limit` | No | `64` | Integer from 1 through 128; valid only when the terminal value is an array/list. |

The result always contains `type`, `value`, `sessionId`, and `gameTick`. `type` follows the same rule as in `data.describe`. A read or projection fails as a whole if any requested member cannot be read; v1 has no partial-success envelope.

Terminal-value behavior:

| Terminal value | `value` representation |
| --- | --- |
| Null | JSON null; no further member/element reads occur. |
| Scalar | Encoded scalar, as specified below. |
| Object without `select` | A shallow object summary. |
| Object with `select` | An object containing exactly the selected members. |
| Array/list | A page object containing `items`, `offset`, `totalCount`, and `hasMore`. |
| Supported dictionary | A shallow dictionary summary; read individual entries using a key path segment. |

`select` is invalid for non-null scalar and dictionary terminals. In a collection page it applies to each non-null object element; requesting it for scalar elements is invalid. Null elements remain null. `offset` or `limit` on a non-collection is invalid, including null values whose declared type is not an array/list. Parameters are validated even when the selected page is empty; member availability is checked on objects actually read.

Pages return up to `limit` items. `offset` at or beyond the current count returns an empty page, preserving the requested offset, with `hasMore: false`. A single-element path index at or beyond the count is instead `INDEX_OUT_OF_RANGE`. `totalCount` means the underlying container count, not the number of live entities in a DSP pool. No filtering, compaction, sorting, or zero-ID skipping is implied. Pages from different requests need not describe an unchanged collection.

Complex values inside a projection or page remain summaries unless they are the direct objects selected by the request. The server never recursively expands an entire object graph. Summary shapes are:

```json
{
  "$kind": "object",
  "$type": "GameData"
}
```

```json
{
  "$kind": "array",
  "$type": "System.Int32[]",
  "count": 8
}
```

Collection summaries use `$kind` values `array`, `list`, or `dictionary` and include `count`. The dollar-prefixed keys belong to the wire representation, not to the reflected object's members. To expand a summarized value, issue another request whose path addresses that value.

Scalar encoding:

| CLR value | JSON encoding |
| --- | --- |
| Boolean, string | Boolean or string. |
| Character | One-character string. |
| Signed/unsigned integers up to 32 bits | Number. |
| `Int64`, `UInt64` | Invariant decimal string, even when the value would fit JavaScript's safe-integer range. |
| Finite `Single`, `Double` | Number. |
| `Decimal` | Invariant decimal string. |
| Enum | Object with `name` (declared name or null) and `value` (underlying integer as a decimal string). |
| `DateTime`, `DateTimeOffset` | ISO 8601 round-trip string; preserve the source time-zone/Kind information. An unspecified `DateTime` has no implied UTC suffix. |
| `Guid` | Standard hyphenated string. |

Non-finite floating-point values fail with `UNSUPPORTED_VALUE`. Unsupported runtime types fail with `UNSUPPORTED_TYPE`; the server does not serialize their internals or call arbitrary `ToString()` implementations. Plain game-data structs, including position structs defined by DSP, follow the object/projection rules. Unity `Vector2`, `Vector3`, `Vector4`, and `Quaternion` support their instance fields through the same rules; Unity objects such as components and textures do not.

#### Research read example

First read `history` at `["currentTech"]`; a value of zero means there is no current research. Use a nonzero returned ID as the dictionary key, rather than assuming the illustrative ID below is active:

```json
{
  "jsonrpc": "2.0",
  "id": "research-1",
  "method": "data.read",
  "params": {
    "root": "history",
    "path": ["techStates", {"key": 1001}],
    "select": ["curLevel", "hashUploaded", "hashNeeded"]
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": "research-1",
  "result": {
    "type": "TechState",
    "value": {
      "curLevel": 3,
      "hashUploaded": "12345678901234567",
      "hashNeeded": "22345678901234567"
    },
    "sessionId": "session-1",
    "gameTick": "6000"
  }
}
```

#### Array read example

```json
{
  "jsonrpc": "2.0",
  "id": "queue-1",
  "method": "data.read",
  "params": {
    "root": "history",
    "path": ["techQueue"],
    "offset": 0,
    "limit": 2
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": "queue-1",
  "result": {
    "type": "System.Int32[]",
    "value": {
      "items": [1001, 1002],
      "offset": 0,
      "totalCount": 8,
      "hasMore": true
    },
    "sessionId": "session-1",
    "gameTick": "6000"
  }
}
```

## Game state and consistency

- Menu demos, loading, and the absence of a running player game make `data.read` and `data.describe` fail with `GAME_NOT_READY`. `data.roots` can still report unavailability.
- An unavailable known root during an active game, such as `localPlanet` during space travel, produces `ROOT_UNAVAILABLE`. This differs from a null terminal member on an available root.
- Every game-data request is associated with the session state in which it was admitted. A queued request cannot silently read a subsequently loaded save; a session transition invalidates it with `SESSION_CHANGED`.
- Results are detached samples taken on the main thread. `gameTick` identifies sampling time; it is not a transactional snapshot ID. No cross-request or globally atomic game-world snapshot is promised.
- A response sampled before a session transition may already be in transit. Clients should use `sessionId` and new discovery results to discard data belonging to a previous session.

## Errors and recovery

```json
{
  "jsonrpc": "2.0",
  "id": "read-1",
  "error": {
    "code": -32001,
    "message": "Game data is unavailable.",
    "data": {
      "kind": "GAME_NOT_READY"
    }
  }
}
```

`message` is English diagnostic text, not a stable identifier. `error.data.kind` is stable. Error data may additionally contain `segmentIndex` (zero-based), `member`, or `root` to identify the failure. Stack traces, filesystem paths, and exception objects are not returned.

| Code | Kind | Meaning / client action |
| --- | --- | --- |
| `-32700` | `PARSE_ERROR` | Malformed JSON or duplicate object keys; response ID is null. Correct the message. |
| `-32600` | `INVALID_REQUEST` | Invalid envelope, batch, or invalid/missing ID. Correct the request; ID is null if it cannot be reliably determined. |
| `-32601` | `METHOD_NOT_FOUND` | Unknown method. Check API version and method spelling. |
| `-32602` | `INVALID_PARAMS` | Wrong/missing/unknown parameter or invalid parameter combination. Correct the request. |
| `-32603` | `INTERNAL_ERROR` | Unexpected server failure. Inspect server logs. |
| `-32001` | `GAME_NOT_READY` | No ready player game. Poll root availability before retrying. |
| `-32002` | `ROOT_NOT_FOUND` | Unknown root name. Rediscover roots. |
| `-32003` | `ROOT_UNAVAILABLE` | Known root is absent in this game state. Wait for it to become available. |
| `-32004` | `MEMBER_NOT_FOUND` | Requested member does not exist. Rediscover members for this game version. |
| `-32005` | `MEMBER_NOT_ALLOWED` | Member exists but is outside the read policy. Choose an exposed member. |
| `-32006` | `NULL_PATH` | Null encountered before completing the path. Query a shorter path or retry after state changes. |
| `-32007` | `INDEX_OUT_OF_RANGE` | Element index is outside the current array/list. Refresh collection metadata. |
| `-32008` | `KEY_NOT_FOUND` | Dictionary has no matching key. Refresh the source of the key. |
| `-32009` | `UNSUPPORTED_TYPE` | Value type or traversal operation is unsupported. Use supported data members. |
| `-32010` | `READ_FAILED` | A permitted read failed, including a getter exception. Inspect logs or rediscover the path. |
| `-32011` | `LIMIT_EXCEEDED` | A documented resource limit would be exceeded. Reduce depth, projection, page size, or output size. |
| `-32012` | `SERVER_BUSY` | Admission capacity is exhausted. Reduce concurrent requests and back off. |
| `-32013` | `REQUEST_TIMEOUT` | Response production exceeded its deadline. Retry later with a new ID. |
| `-32014` | `SESSION_CHANGED` | Session changed after admission. Rediscover roots and discard old-session work. |
| `-32015` | `UNSUPPORTED_VALUE` | Value cannot be represented under the scalar rules. |

Malformed parameter types and negative indexes/offsets are `INVALID_PARAMS`. For otherwise well-formed method parameters, exceeding a documented positive maximum, such as path length or page size, is `LIMIT_EXCEEDED`. Envelope validity and transport limits follow their specific rules above. A well-formed but unknown root is `ROOT_NOT_FOUND` before game availability is evaluated. Path resolution then stops at its first failure.

Transport closure codes relevant to clients:

| Code | Meaning |
| --- | --- |
| `1001` | Server is stopping. |
| `1003` | Binary application message is unsupported. |
| `1007` | Invalid UTF-8 payload. |
| `1008` | Protocol violation such as reuse of an outstanding ID. |
| `1009` | Incoming message is too large. |
| `1013` | Overload or inability to drain a bounded send queue. |

Do not retry invalid paths unchanged in a tight loop. Game-state errors can be retried after discovery; overload and timeouts require client-side backoff. Reconnection creates a new request-ID namespace but does not restore any requests from the old connection.
