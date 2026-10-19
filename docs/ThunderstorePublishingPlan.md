# Local Thunderstore Publishing Plan

Status: implemented as a local PowerShell script. No real package upload has been performed.

## Goal and scope

Add a repository-root `Publish-Mod.ps1` that builds, packages, and publishes one explicitly named mod to the Dyson Sphere Program community through tcli. Reuse the existing MSBuild packaging targets and upload their ZIP without rebuilding it through tcli. Keep credentials out of version control.

This proposal covers a local PowerShell command. It does not add a GitHub Actions workflow, change mod versions, upload dependencies, or publish packages during implementation checks.

## Metadata source

Use the existing `<Project>/package/manifest.json` for package metadata. Query Team from the service-account token and read Package Name from the manifest's `name` field. Keep the manifest's existing schema; no additional package metadata file or Team field is needed.

For publication, send the selected token as `Authorization: Bearer <token>` to `GET https://thunderstore.io/api/experimental/current-user/`. Require an authenticated profile with a non-empty username and a `teams` array containing exactly one valid Team name. Preserve that name's capitalization as tcli's namespace. Stop on an unauthenticated or malformed response, an empty or ambiguous Team list, invalid names, HTTP errors, or network failures. Do not guess a namespace or select the first of multiple Teams. `-DryRun` skips credentials and all Thunderstore requests, leaving Team unresolved until publication.

| Publishing value | Source and rule |
| --- | --- |
| Team / tcli package namespace | The selected token's unique Team from the current-user API, preserving capitalization; unresolved in `-DryRun`. |
| Package Name | `manifest.json.name`, preserving capitalization. |
| Version | Evaluated `.csproj` `Version`; `ZipMod` synchronizes `manifest.json.version_number`. Check the packaged value afterward. |
| Community | Constant `dyson-sphere-program`. |
| Repository | Constant `https://thunderstore.io`. |
| Category | Explicitly empty tcli category configuration; preserve existing listing categories. |
| Package contents | Existing `ZipMod` output, including its manifest, README, icon, DLLs, and optional changelog. |

## Command interface

```powershell
# Publish using the caller directory's .env, or TCLI_AUTH_TOKEN if no file exists.
.\Publish-Mod.ps1 -Project UXAssist

# Read the token from an explicitly selected file.
.\Publish-Mod.ps1 -Project Dustbin -EnvFile "$env:USERPROFILE\.config\DSP_Mods\thunderstore.env"

# Build and inspect the package without uploading; a token is not required.
.\Publish-Mod.ps1 -Project UXAssist -DryRun
```

| Parameter | Behavior |
| --- | --- |
| `-Project <string>` | Required. Select one top-level project by directory name, such as `UXAssist` or `Dustbin`. Resolve paths from `$PSScriptRoot`, independently of the caller's current directory. |
| `-EnvFile <path>` | Optional. Read `TCLI_AUTH_TOKEN` from an explicitly selected file. When omitted, use `.env` in the caller's current directory if present, otherwise use the process variable. |
| `-DryRun` | Build, package, validate, and show local package metadata with Team unresolved. Skip credential loading, Team lookup, remote package checks, generated tcli configuration, and publication. This mode still writes normal build and package outputs. |

Use Release configuration. Omit package-name, category, community, version-override, publish-all, and skip-build parameters from the initial interface. Selecting a preloader or helper project directly must fail with an actionable message identifying the main mod where applicable.

Target Windows PowerShell 5.1 and PowerShell 7. The existing packaging targets invoke `powershell.exe`, so this proposal does not promise cross-platform packaging.

## Build and publish sequence

1. Resolve the named project within the repository. Require `<Project>/<Project>.csproj`, `package/manifest.json`, `package/icon.png`, and `README.md`. Reject traversal, wildcard input, helpers, and projects with `PackPreloaderTargetDir` set. Validate the manifest's `name` before building.
2. Evaluate Release MSBuild properties instead of guessing output paths or parsing only literal XML: `Version`, `ProjectName`, `TargetPath`, `PackHasChangelog`, `PackUsePluginsLayout`, and `PackPreloaderTargetDir`. Read evaluated `ProjectReference` items to find bundled preloaders. MSBuild 17.8 or newer provides the required `-getProperty` and `-getItem` JSON output.
3. For a real publish, resolve the token before changing to the repository's build directory, check the pinned local tcli tool, and resolve its unique Team before the expensive build. tcli 0.2.4 returns exit code 1 even when `--version` successfully prints its version: accept 0 or 1 only for this check, and require recognizable version output. For `-DryRun`, neither a token nor an installed tcli tool is required and no Thunderstore requests are made. Restore project NuGet assets when missing, then use `--no-restore` for packaging.
4. For each referenced preloader whose evaluated `PackPreloaderTargetDir` belongs to the selected mod's package, run `CopyToParentPackage` in Release before packaging the main mod. This refreshes Dustbin's bundled preloader even if an old DLL already exists under `package/patchers`. Do not upload preloaders or referenced mods separately.
5. Run `dotnet build <Project>/<Project>.csproj -t:ZipMod -c Release --no-restore`. Check every native command's exit code; builds and publication must stop immediately on a nonzero result. Retain the existing warning policy.
6. Select the exact ZIP path defined by the existing target: `<Project>/package/<ProjectName>-<Version>.zip`. Do not select the newest ZIP by timestamp or a wildcard. Read the ZIP and check its root manifest identity and version, required root files, 256x256 PNG icon, expected DLL layout, and optional changelog. Compare bundled main/preloader DLLs with the current evaluated build outputs. When a separate changelog is included, require its latest English and Chinese headings to match the project version. Do not change any version automatically.
7. Print the project, Team, Package Name, version, community, ZIP path, and category-preservation behavior. In `-DryRun`, identify Team as unresolved, stop successfully, and retain the ZIP for inspection. Otherwise, write generated, non-secret tcli configuration under `<Project>/obj/thunderstore-publish/<run-id>/thunderstore.toml` with the resolved Team.
8. Before a real upload, query the exact package-version endpoint using the resolved namespace, manifest package name, and evaluated project version. If the version exists, stop without uploading. Treat only HTTP 404 as an absent version; authentication, network, server, and response-format errors must remain errors. Do not automatically increment versions or retry a publish with an uncertain result.
9. Provide the token to the tcli child process and run `dotnet tool run tcli -- publish --config-path <generated-config> --file <exact-zip>`. Restore process environment and working directory in `finally`. Remove only this invocation's generated configuration directory after verifying its resolved path. Preserve the ZIP.
10. Return success only after tcli reports successful publication. Show the package page and tcli's download URL. After a submission timeout or failure, explain how to inspect the exact version before retrying; a failed command does not prove that the server created no release.

For example, Dustbin's packaging commands are:

```powershell
dotnet build DustbinPreloader/DustbinPreloader.csproj -t:CopyToParentPackage -c Release --no-restore
dotnet build Dustbin/Dustbin.csproj -t:ZipMod -c Release --no-restore
```

Only the selected mod is published. Existing dependency declarations remain authoritative; packaging a dependent mod does not publish UXAssist. Retired LabOpt is excluded from routine implementation checks.

## Generated tcli configuration

Use a repository-local .NET tool manifest at `.config/dotnet-tools.json` and pin tcli to `0.2.4`, the latest version currently available from NuGet. First-time setup is `dotnet tool restore`; do not silently install or update a global tcli. Recheck the available version when implementation begins, and change the pin only after checking the behavior below.

Example configuration for an existing UXAssist version:

```toml
[config]
schemaVersion = "0.0.1"

[package]
namespace = "soarqin"
name = "UXAssist"
versionNumber = "1.6.2"

[build]
outdir = "."

[publish]
repository = "https://thunderstore.io"
communities = ["dyson-sphere-program"]
categories = []
```

Generate `namespace` from the token's unique Team, `name` from `package/manifest.json.name`, and `versionNumber` from the evaluated project version. The values above illustrate `-Project UXAssist` when the token resolves to Team `soarqin`; they are not defaults. Validate permitted identifier/version characters before writing TOML, and pass command arguments as an array rather than composing executable PowerShell text. `[build].outdir` satisfies tcli's publish-time configuration validation; `--file` prevents tcli from building a second ZIP. The uploaded archive supplies the actual package description, dependencies, website, and other manifest content.

`categories = []` is deliberate. In tcli 0.2.4, omitting the setting leaves example `riskofrain2` categories from the configuration model. An empty array produces empty legacy categories and an empty per-community category map. The inspected Thunderstore server code only adds categories when the submitted list is non-empty, so an existing listing retains its current categories. Categories belong to the community listing, rather than an individual package version. A first release has no previous categories to inherit and starts without categories. No category lookup or local category cache is necessary.

## Token handling

Use a Thunderstore Team service-account token through `TCLI_AUTH_TOKEN`. Select an explicit `-EnvFile` first, otherwise read `.env` in the caller's current directory if it exists, and finally fall back to the inherited process variable when no default file exists. Resolve this before changing directories. A missing explicit file or invalid token in a selected file is an error, not a reason to use another source. Do not add a token parameter or pass `--token` to tcli. Never write the token into TOML, the tool manifest, package metadata, a generated script, or a log.

The environment file contains one token assignment:

```dotenv
TCLI_AUTH_TOKEN=<service-account-token>
```

Read it as UTF-8 data. Support blank lines, full-line comments, and an optional matching pair of single or double quotes around the value. Reject duplicate token assignments, empty values, malformed lines, and unsupported keys. Do not evaluate PowerShell, expand variables, or execute the file. Do not print offending file contents in errors.

Allow local environment files inside or outside the checkout. Keep `.env` and `.env.*` ignored by Git so that a repository-local `.env` remains untracked. Use only synthetic tokens in implementation checks.

Use the same selected token for the read-only current-user request and tcli publication. Send the Bearer header only to the fixed Thunderstore HTTPS endpoint with redirects disabled and a bounded timeout. Do not forward it to the public package-version endpoint or include response bodies, headers, or underlying request exceptions in credential-related errors.

The pinned tcli version has a Windows-specific behavior: it reads the persisted User-scoped `TCLI_AUTH_TOKEN` before the process value. Before publication, compare that User-scoped value with the selected token without logging either value. If a different non-empty User value exists, stop and explain that the caller must remove or reconcile the stale setting. Do not change User or Machine environment variables automatically. Otherwise, temporarily set only the process variable immediately around tcli invocation and restore its original presence and value in `finally`.

## Implementation work

- [x] Implement required `-Project` and automatically resolve the token's unique Team. Read Package Name from the existing manifest and verify that it agrees with the archive manifest.
- [x] Add the pinned `.config/dotnet-tools.json`, the `Publish-Mod.ps1` entry point, and environment-file ignore patterns. Keep packaging logic in the existing MSBuild targets.
- [x] Add a short English/Chinese publishing section to the root README covering tool restoration, project selection, automatic Team resolution, credential sources, category retention, and `-DryRun`.
- [x] Verify local preparation and failure handling without making a real upload. Leave mod versions, release changelogs, runtime code, `AGENTS.md`, and GitHub Actions unchanged.

## Acceptance checks

| Check | Expected result |
| --- | --- |
| `UXAssist -DryRun`, invoked from another directory | Uses repository-relative inputs, produces the normal flat ZIP, and shows local identity, unresolved Team, and DSP community without loading credentials or making Thunderstore requests. |
| `Dustbin -DryRun` with an existing preloader DLL | Refreshes `DustbinPreloader`, packages `plugins/` and `patchers/`, and includes the current build outputs. |
| The token resolves to a different Team for the same project | Preserves that Team's name in generated configuration, version lookups, and package links while continuing to read Package Name from the existing manifest. |
| Team lookup returns an anonymous/malformed profile, no Teams, multiple Teams, an invalid name, or a request error | Stops before building or publication without guessing a namespace or exposing the token. |
| Missing project, malformed metadata, preloader/helper selection, or traversal input | Stops before publication and identifies the invalid input. |
| Explicit `-EnvFile`, default caller-directory `.env`, and process token are all available | Uses the explicit file first; without it, uses the caller's `.env`; without either file, uses the process variable. Repository-local `.env` files are supported. |
| Missing token, missing explicit file, malformed selected file, or conflicting Windows User-scoped token | Stops without exposing credentials or changing persisted environment variables. |
| tcli version check prints a version with exit code 1, returns no version, or cannot start | Accepts the documented version-output case only; missing/invalid output and startup failures stop before building or publication. |
| Failed restore/build/pack or invalid ZIP | Never invokes tcli publication and never falls back to an older ZIP. |
| Generated configuration | Contains only `dyson-sphere-program`, explicitly empty categories, and no credential values. Check the parsed tcli metadata as well as the TOML text. |
| Existing remote version, unexpected HTTP failure, tcli failure, or uncertain timeout | Returns a nonzero result and provides an appropriate next step; no automatic republish or version bump. |
| Stubbed successful tcli invocation | Receives the exact validated ZIP and intended token through its environment; receives no token argument. Process environment and working directory are restored after both success and failure. |

Use focused command stubs for publication/error-path checks with a synthetic token. Run actual packaging preparation for UXAssist and Dustbin and run `git diff --check` before implementation handoff. A real release is a separate operation against an explicitly selected project/version; validate the resulting listing and retained categories when that operation is requested.

## Verified upstream behavior

Sources checked on 2026-10-17; command entry point checked on 2026-10-18; Team lookup checked on 2026-10-19:

- [tcli publishing and authentication documentation](https://github.com/thunderstore-io/thunderstore-cli/wiki): `TCLI_AUTH_TOKEN`, namespace, and publishing an existing ZIP with `--file`.
- [NuGet tcli versions](https://api.nuget.org/v3-flatcontainer/tcli/index.json): currently available tool versions.
- [MSBuild property and item evaluation](https://learn.microsoft.com/en-us/visualstudio/msbuild/evaluate-items-and-properties): evaluated metadata and JSON output in MSBuild 17.8 and newer.
- [tcli 0.2.4 configuration model](https://github.com/thunderstore-io/thunderstore-cli/blob/0.2.4/ThunderstoreCLI/Models/ThunderstoreProject.cs) and [upload metadata](https://github.com/thunderstore-io/thunderstore-cli/blob/0.2.4/ThunderstoreCLI/Configuration/Config.cs): category defaults, the empty-array mapping, and publishing metadata.
- [tcli 0.2.4 environment provider](https://github.com/thunderstore-io/thunderstore-cli/blob/0.2.4/ThunderstoreCLI/Configuration/EnvironmentConfig.cs): Windows User-scope precedence.
- [tcli 0.2.4 command entry point](https://github.com/thunderstore-io/thunderstore-cli/blob/0.2.4/ThunderstoreCLI/Program.cs): parser results, including version/help output, return exit code 1.
- [Thunderstore service-account authentication](https://github.com/thunderstore-io/Thunderstore/blob/master/django/thunderstore/account/authentication.py), [default authentication settings](https://github.com/thunderstore-io/Thunderstore/blob/master/django/thunderstore/core/settings.py), and [current-user API](https://github.com/thunderstore-io/Thunderstore/blob/master/django/thunderstore/social/api/experimental/views/current_user.py): Bearer-token authentication and the authenticated profile's `teams` array.
- [Thunderstore upload form](https://github.com/thunderstore-io/Thunderstore/blob/685b2743035e0ac0786180d10813d67b72fcdc2d/django/thunderstore/repository/package_upload.py) and [listing update behavior](https://github.com/thunderstore-io/Thunderstore/blob/685b2743035e0ac0786180d10813d67b72fcdc2d/django/thunderstore/repository/models/package.py): empty submitted categories retain existing listing categories.
- [Thunderstore manifest serializer](https://github.com/thunderstore-io/Thunderstore/blob/685b2743035e0ac0786180d10813d67b72fcdc2d/django/thunderstore/repository/package_manifest.py): standard manifest fields and duplicate-version validation.
