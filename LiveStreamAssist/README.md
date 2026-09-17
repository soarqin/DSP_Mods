# LiveStreamAssist

Keeps the Dyson Sphere Program statistics window active during a live stream by switching between the Production Statistics and Dyson Sphere tabs at a random interval. Optionally exposes a local/LAN WebSocket API for read-only game data.

## Usage

- Press Ctrl+F8 to start or stop the assist. The key can be changed in the game's keyboard settings.
- Starting the assist opens the statistics window and selects Production Statistics.
- The mod switches between Production Statistics and Dyson Sphere every 15 to 30 seconds.
- Closing the statistics window stops the assist automatically.

The mod requires UXAssist.

## WebSocket API

Disabled by default. In the BepInEx config file, set:

```text
[WebSocketApi]
Enabled = true
ListenAddress = 127.0.0.1
Port = 18080
```

Then connect to `ws://<host>:18080/api/v1`. There is no authentication. Use a LAN bind address or `0.0.0.0` only on trusted networks. Configuration changes require a game restart.

- [WebSocket API reference](https://github.com/soarqin/DSP_Mods/blob/master/LiveStreamAssist/docs/WebSocketApi.md)
- [WebSocket design and implementation plan](https://github.com/soarqin/DSP_Mods/blob/master/LiveStreamAssist/docs/WebSocketDesign.md)
