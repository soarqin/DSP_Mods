# DSP Mods by Soar Qin

## [CheatEnabler](CheatEnabler)

Add various cheat functions while disabling abnormal determinants
添加一些作弊功能，同时屏蔽异常检测

## [CompressSave](CompressSave)

Moved [to another repo](https://github.com/soarqin/DSP_Mods_TO/tree/master/CompressSave)

## [Dustbin](Dustbin)

Can turn Storages and Tanks into Dustbin(Destroy incoming items)
储物仓和储液罐可以转变为垃圾桶(销毁送进的物品)

## [HideTips](HideTips)

Hide/Disable various tutorial tips/messages
隐藏/屏蔽各种引导提示/消息

## [LabOpt](LabOpt)

Performance optimizations for Matrix Labs
Marked as obsoleted temporarily, needs more investigation about LabComponent's mechanism
优化研究站性能
暂时标识为过期，需要更多关于LabComponent机制的研究

## [LogisticMiner](LogisticMiner)

Logistic Storages can mine all ores/water on current planet
物流塔可以采集当前星球的全部矿产(以及水)

## [LuaScriptEngine](LuaScriptEngine)
Write mod functions in lua scripts  
用lua脚本编写mod功能

## [MechaDronesTweaks](MechaDronesTweaks)

Some tweaks for mecha drones and build functions(Successor to FastDrones MOD)
机甲建设机和建设功能调整(FastDrones MOD的后继者)

## [OverclockEverything](OverclockEverything)

Boost nearly all structures
加速几乎所有建筑功能

## [PoolOpt](PoolOpt)

Optimize memory pools on loading gamesaves
加载游戏存档时优化内存池的使用

## [UniverseGenTweaks](UniverseGenTweaks)

Universe Generator Tweak
宇宙生成参数调节

## [UserCloak](UserCloak)

Cloak(Fake) user account info
隐匿(伪装)用户账号信息

## [UXAssist](UXAssist)

Some functions and patches for better user experience
一些提升用户体验的功能和补丁

## [LiveStreamAssist](LiveStreamAssist)

Keeps the statistics window active for live streams by switching between the Production Statistics and Dyson Sphere tabs every 15 to 30 seconds.

## Publishing to Thunderstore

Use Windows PowerShell 5.1 or PowerShell 7, a .NET SDK with MSBuild 17.8 or newer, and the repository's normal game references and build prerequisites. Restore the pinned local tcli tool once from the repository root:

```powershell
dotnet tool restore
```

Select a single main mod with `-Project`. Package Name comes from `<Project>/package/manifest.json`; version comes from the project file. For publication, the script queries Thunderstore's `/api/experimental/current-user/` endpoint with the service-account token and uses its single Team as the namespace. Missing, invalid, or multiple Teams and request failures stop publication before building. Preview the build and package without a token or network access:

```powershell
.\Publish-Mod.ps1 -Project UXAssist -DryRun
```

To publish, put a Team service-account token in a UTF-8 `.env` file in the directory where the command is invoked, then run the same command without `-DryRun`. If `.env` is absent, the script reads the process environment variable `TCLI_AUTH_TOKEN`. To select another file, pass `-EnvFile`:

```powershell
.\Publish-Mod.ps1 -Project Dustbin -EnvFile "$env:USERPROFILE\.config\DSP_Mods\thunderstore.env"
```

The file accepts one `TCLI_AUTH_TOKEN=...` assignment, optional quotes, blank lines, and full-line comments. Token selection follows this order: explicit `-EnvFile`, the caller directory's `.env`, then the process variable. A selected file with an invalid or missing token is an error. `.env` and `.env.*` are ignored by Git; keep real tokens out of version control and shell command history. tcli 0.2.4 prioritizes a persisted Windows User-scoped token over the process token; if the values conflict, the script stops so that the stale setting can be reconciled.

The script always uses Release builds and the existing `ZipMod` target. It refreshes bundled preloaders before packaging, validates the exact ZIP, rejects an already published version, and uploads only the selected main mod. `-DryRun` still creates normal build/package outputs but skips credential loading, Team lookup, remote version checks, and uploads; its Team remains unresolved until publication. Publication always targets **Dyson Sphere Program** (`dyson-sphere-program`) with empty Category input, preserving the existing listing's categories; a first release starts without categories. Versions are never bumped automatically. After an interrupted or failed submission, check the printed version page before retrying because the release may already exist.

Run the focused script checks with `powershell.exe -NoProfile -File tools/tests/Test-PublishMod.ps1` or `pwsh -NoProfile -File tools/tests/Test-PublishMod.ps1`. They use synthetic credentials and stubbed network/publication commands; scratch files stay under `UXAssist/obj/publish-mod-check`.

## 发布到 Thunderstore

使用 Windows PowerShell 5.1 或 PowerShell 7，并安装包含 MSBuild 17.8 或更新版本的 .NET SDK。游戏引用和其他构建依赖与正常编译一致。首次使用时，在仓库根目录还原固定版本的本地 tcli 工具：

```powershell
dotnet tool restore
```

通过 `-Project` 选择一个主 mod。Package Name 读取 `<Project>/package/manifest.json`，版本读取项目文件。正式发布时，脚本使用服务账号 token 查询 Thunderstore 的 `/api/experimental/current-user/` 接口，将返回的唯一 Team 作为 namespace。没有 Team、Team 名称无效、存在多个 Team 或请求失败时，脚本会在构建前停止。可以先预览构建和打包，无需 token，也无需联网：

```powershell
.\Publish-Mod.ps1 -Project UXAssist -DryRun
```

正式发布时，将 Team 服务账号的 token 写入调用目录下的 UTF-8 `.env` 文件，再去掉 `-DryRun` 执行。如果 `.env` 不存在，脚本会读取进程环境变量 `TCLI_AUTH_TOKEN`。也可以通过 `-EnvFile` 指定其他文件：

```powershell
.\Publish-Mod.ps1 -Project Dustbin -EnvFile "$env:USERPROFILE\.config\DSP_Mods\thunderstore.env"
```

文件允许一条 `TCLI_AUTH_TOKEN=...` 赋值，以及可选的引号、空行和整行注释。token 的读取优先级为：显式 `-EnvFile`、调用目录下的 `.env`、进程环境变量。选中的文件缺少 token 或格式无效时，脚本会报错。`.env` 和 `.env.*` 已加入 Git 忽略规则；真实 token 不应提交到版本控制，也不要写入 shell 命令历史。tcli 0.2.4 会优先读取 Windows 持久化的用户级 token；如果它与本次选定的 token 不同，脚本会停止，需先处理旧设置。

脚本始终使用 Release 构建和现有的 `ZipMod` 目标。它会在打包前更新附带的 preloader，检查本次生成的确切 ZIP，拒绝重复发布已有版本，并且只上传选定的主 mod。`-DryRun` 仍会生成正常的构建和打包产物，但跳过凭据读取、Team 查询、远程版本检查和上传；Team 留待正式发布时确定。发布社区固定为 **Dyson Sphere Program**（`dyson-sphere-program`）；Category 输入留空，保留已有条目的分类，首次发布则不设置分类。脚本不会自动修改版本号。提交中断或失败后，应先检查输出的版本页面，再决定是否重试，因为该版本可能已经发布成功。

可以运行 `powershell.exe -NoProfile -File tools/tests/Test-PublishMod.ps1` 或 `pwsh -NoProfile -File tools/tests/Test-PublishMod.ps1` 检查脚本。测试使用虚构的凭据，以及模拟的网络和发布命令；临时文件保存在 `UXAssist/obj/publish-mod-check` 下。
