# LiveStreamAssist

Keeps the Dyson Sphere Program statistics window active during a live stream by switching between the Production Statistics and Dyson Sphere tabs at a random interval. Optionally exposes a local/LAN WebSocket API for read-only game data.

## Usage

- Press Ctrl+F8 to start or stop the assist. The key can be changed in the game's keyboard settings.
- Starting the assist opens the statistics window and selects Production Statistics.
- The mod switches between Production Statistics and Dyson Sphere every 15 to 30 seconds.
- Closing the statistics window stops the assist automatically.

The mod requires UXAssist.

## WebSocket API

The in-tree listener uses in-process ASP.NET Core 2.3 Kestrel (managed sockets). Unity 2022/Mono and LAN acceptance remain pending. The design document below defines the remaining gates.

Disabled by default. In the BepInEx config file, set:

```text
[WebSocketApi]
Enabled = true
ListenAddress = 127.0.0.1
Port = 18080
```

Then connect to `ws://<host>:18080/api/v1`. There is no authentication. Use a LAN bind address or `0.0.0.0` only on trusted networks. Configuration changes require a game restart.

Open the overlay examples in a browser or OBS Browser Source. Default URL is `ws://127.0.0.1:18080/api/v1`; override with `?ws=`.

- [examples/research-overlay.html](https://github.com/soarqin/DSP_Mods/blob/master/LiveStreamAssist/examples/research-overlay.html) shows the current research name, level, and hash progress.
- [examples/stats-overlay.html](https://github.com/soarqin/DSP_Mods/blob/master/LiveStreamAssist/examples/stats-overlay.html) also shows item production/consumption per minute and Dyson sphere generation.

The overlays use the [JS client](js/README.md): `Client` for JSON-RPC, `GameQuery` for those reads, and `GameText` for localized names and DSP KMG formatting. Keep the `examples/` and `js/` directories side by side when copying an overlay. `GameText` does not call the WebSocket API; regenerate `js/generated/dsp-text.js` from the repository root with `python LiveStreamAssist/js/scripts/generate_text.py`.

- [WebSocket API reference](https://github.com/soarqin/DSP_Mods/blob/master/LiveStreamAssist/docs/WebSocketApi.md)
- [WebSocket design and transport migration plan](https://github.com/soarqin/DSP_Mods/blob/master/LiveStreamAssist/docs/WebSocketDesign.md)
