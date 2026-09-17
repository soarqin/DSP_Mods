# LiveStreamAssist JS client

Browser helper for the [WebSocket API](../docs/WebSocketApi.md). It is intended for OBS Browser Sources and local overlay pages with BigInt and `Promise.allSettled` support. There is no npm build; load the packed script as a classic `<script>`.

The library has three layers:

| Layer | Script surface | WebSocket? | Role |
| --- | --- | --- | --- |
| Low-level client | `LiveStreamAssist.Client` | Yes | JSON-RPC 2.0 transport, method wrappers, pending-request limit |
| High-level query | `LiveStreamAssist.GameQuery` | Yes | Common overlay reads: item rates, research, Dyson power |
| Game text | `LiveStreamAssist.GameText` | No | ID to localized name, DSP KMG formatting |

`GameText` never sends API requests. Names come from generated dictionaries converted from the game's `Locale/` files and prototype tables.

## Files

- `livestream-assist.js` — packed browser bundle
- `generated/dsp-text.js` — generated locale/proto dictionary
- `src/` — layer sources concatenated by `scripts/pack_js.py`
- `scripts/generate_text.py` — regenerate text data from a local DSP install

## Overlay usage

```html
<div id="research"></div>
<div id="rates"></div>
<div id="power"></div>
<script src="../js/generated/dsp-text.js"></script>
<script src="../js/livestream-assist.js"></script>
<script>
  (async function () {
    const text = LiveStreamAssist.GameText.fromGenerated({ language: "en" });
    const client = new LiveStreamAssist.Client({ url: "ws://127.0.0.1:18080/api/v1" });
    const query = new LiveStreamAssist.GameQuery(client);

    await client.systemInfo();
    await client.validate({ apiMajor: 1, requiredCapabilities: ["reflection.read"] });
    await query.waitUntilReady();

    const research = await query.getCurrentResearch();
    const iron = await query.getItemRates(1101);
    const dyson = await query.getDysonPower();

    document.getElementById("research").textContent = research.active
      ? text.techDisplayName(research.techId, research.curLevel)
      : "No active research";
    document.getElementById("rates").textContent = text.itemName(1101) + " " + text.formatRate(iron.production) + " / min";
    document.getElementById("power").textContent = text.formatPower(dyson.watts);
  })().catch(function (error) { console.error(error); });
</script>
```

Examples:

- [research-overlay.html](../examples/research-overlay.html)
- [stats-overlay.html](../examples/stats-overlay.html)

Keep `examples/` and `js/` side by side when copying these pages. Both examples retry failed connections, wait for each polling cycle to finish before scheduling another, and discard old results when the URL changes. Use `?lang=en`, `?lang=zh-CN`, or an LCID such as `?lang=2052`; the stats example also accepts `?item=1101`.

## Low-level client

`Client` speaks the documented JSON-RPC methods only: `system.ping`, `system.info` (`systemInfo()`), `system.validate`, `system.stats`, `data.roots`, `data.describe`, and `data.read`. Unknown parameters are omitted so the server does not reject the request.

- Request ids are `n-<n>` and are unique among outstanding calls.
- At most `maxPending` requests are in flight (default 8, then `system.info.limits.maxPendingRequestsPerConnection`). An explicit caller limit remains an upper bound. Limits must be positive integers.
- Responses are correlated by id and may arrive out of order.
- `Int64`/`UInt64` values stay strings on the wire. `GameQuery` converts them where needed.
- `LiveStreamAssist.key(id)` builds a dictionary path segment `{ key }`.
- `connect(url)` replaces a connection when its URL changes. `close()` cancels connecting, pending, and queued work and stops reconnect timers. Call `connect()` explicitly to reopen a closed client; requests never replay across connections.

API errors throw `LiveStreamAssist.ApiError` with stable `kind` from `error.data.kind`.

## High-level queries

Compound queries reject with `SESSION_CHANGED` instead of combining reads from different saves. Retry the entire query. Reads within one session can still sample different game ticks; they are not atomic snapshots.

### Item production

`query.getItemRates(itemId, { window, scope })`

- `window`: `1min` (default), `10min`, `1hour`, `10hour`, `100hour`, `total`. The default 1-minute window matches the statistics panel's shortest rate view: `ProductStat.total[1]` production and `total[8]` consumption, already items/min.
- `scope`: omitted/`'cluster'` sums every factory; `'localPlanet'` uses `localPlanet.factoryIndex`; `{ factoryIndex }` reads one factory.
- Result fields: `production`, `consumption`, `theoreticalProduction`, `theoreticalConsumption`. Theoretical values are `refProductSpeed` / `refConsumeSpeed`, which the game stores in items/min after the production extra-info calculator runs. They stay 0 until the statistics window refreshes that extra info (LiveStreamAssist's Ctrl+F8 assist helps keep that window open).

Pass an array of positive integer item ids to share the factory scan; duplicates do not multiply totals. Only existing product indices are cached, so newly produced items become visible on later polls. An unbuilt local planet returns zero rates; an unavailable local planet during space travel still raises `ROOT_UNAVAILABLE`.

### Research

- `getTechState(techId)` reads `history.techStates[techId]`.
- `getCurrentResearch()` reads `history.currentTech`, then that tech's state. `techId === 0` means no active research. `progressPercent` uses integer hash math, same as the sample overlay.

### Dyson power

`getDysonPower()` sums `game.dysonSpheres[i].energyGenCurrentTick` across non-null spheres, projecting energy fields in collection pages. `getDysonPower({ starIndex })` reads only that sphere. Watts are `joulesPerTick * 60` (DSP ticks per second). `originalWatts` is the pre-Dark-Fog-debuff figure. Star display names use `overrideName` or `name` because `displayName` is a getter and is not readable through the API. Empty results retain their session metadata.

## Game text

Load `generated/dsp-text.js` before the library, then `GameText.fromGenerated({ language: "zh-CN" })`. Language may be an LCID (`1033`, `2052`), `en`, `zh`, `fr`, `de`, or `ja`.

- `itemName`, `techName`, `recipeName`, `signalName`, `veinName`, and `name(kind, id)`
- `translate(key)` for generated locale keys (missing entries return the key and explicit empty translations stay empty, matching DSP)
- `techDisplayName(id, curLevel)` matches `TechState.currentTechString`: name, or name + `杠等级` + level
- `formatKmg`, `formatPower`, `formatEnergy` match `StringBuilderUtility.WriteKMG` / `WriteKMGPower` / `WriteKMGEnergy`: values below 10000 stay raw, then k/M/G/T/P/E with three leading digits. Decimal separators follow the selected language (`小数点`).

## Regenerating text data

Requires a local DSP install plus `pip install -r LiveStreamAssist/js/scripts/requirements.txt`.

```text
python LiveStreamAssist/js/scripts/generate_text.py --game-root "<DSP install>"
python LiveStreamAssist/js/scripts/pack_js.py
```

The generator locates Steam/DSP the same way `UpdateGameDlls.ps1` does when `--game-root` is omitted. It reads BOM-aware `Locale/` files in the header's stable page order and extracts prototype id/name maps from `resources.assets`. Missing item or tech tables abort generation before overwriting the dictionary. Edit `src/` and repack rather than editing `livestream-assist.js` directly.

## Validation

Run from the repository root. JavaScript checks use Node.js 20 or later and require no npm packages. Python checks use the standard library; locale integration checks run when DSP is installed.

```text
python LiveStreamAssist/js/scripts/pack_js.py
node --test LiveStreamAssist/js/scripts/test_client.js LiveStreamAssist/js/scripts/test_query.js LiveStreamAssist/js/scripts/test_text.js LiveStreamAssist/js/scripts/test_overlays.js
python LiveStreamAssist/js/scripts/test_text.py
```

These checks cover connection cancellation, queue limits, session changes, production caching, Dyson pagination, locale parsing, formatting, and overlay polling. They do not replace DSP/OBS or LAN acceptance testing.
