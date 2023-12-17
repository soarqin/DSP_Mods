# UserCloak

#### Cloak(Fake) user account info
#### 隐匿(伪装)用户账号信息

## Changlog
* 1.0.0
  + Initial release

## Usage
* Works only for Steam version.
* Config entries:
  +`[Cloak]`
    - `Mode` [Default Value: false]:
      - `0`: Disable cloaking.
      - `1`: Fake user account info.
      - `2`: Completely hide user account info. This also disables Milkyway completely. 
    - `FakeUserID` [Default Value: random on first launch]: Fake Steam user ID. Works when `Mode` is set to 1. This number is part Z of SteamID as described [here](https://developer.valvesoftware.com/wiki/SteamID).
    - `FakeUsername` [Default Value: anonymous]: Fake Steam user name. Works when `Mode` is set to 1.

## CREDITS
* [Dyson Sphere Program](https://store.steampowered.com/app/1366540): The great game

## 更新日志
* 1.0.0
  + 初始版本

## 使用说明
* 仅支持Steam版
* 设置选项:
  +`[Cloak]`
    - `Mode` [默认值: false]:
      - `0`: 关闭隐匿
      - `1`: 伪装用户账号信息
      - `2`: 完全隐藏用户账号信息，同时完全禁用银河系
    - `FakeUserID` [默认值: 首次启动时随机生成]: 伪装的Steam用户ID，仅在`Mode`设置为1时生效。这个数字是SteamID中的Z部分，详见[这里](https://developer.valvesoftware.com/wiki/SteamID)
    - `FakeUsername` [默认值: anonymous]: 伪装的Steam用户名，仅在`Mode`设置为1时生效

## 鸣谢
* [戴森球计划](https://store.steampowered.com/app/1366540): 伟大的游戏
