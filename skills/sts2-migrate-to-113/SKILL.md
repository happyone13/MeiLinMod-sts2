---
name: sts2-migrate-to-113
description: Audit or migrate Slay the Spire 2 C#/Godot mods from 0.111 or 0.112 to 0.113, including MeiLinMod, YukiMod, Shu, Yusha, Mark, and skin mods. Use for AfterDeath source parameters, character-select Harmony failures, damage overload changes, merchant purchase overrides, RitsuLib runtime variants, version routing, and migration logs.
---

# STS2 Migration to 0.113

## Establish the Actual Target

- Confirm `release_info.json`, the log's `Release Version`, the selected RitsuLib runtime branch, and the actual DLL/PCK load paths. A log's filename or modification date does not identify its game version.
- Respect audit-only requests: produce findings and this version's migration notes without changing gameplay code or deploying. Compiling and inspecting metadata are useful audit steps.
- Read [references/113-api-and-audit.md](references/113-api-and-audit.md) before changing affected APIs. It records verified signatures and distinguishes observations from untested behavior.
- Do not assume all named mods are broken in the same way. Search each repository and inspect its own runtime log.

## Find Compatibility Breaks

Search the active source and tests, excluding generated outputs and packages:

```powershell
rg -n 'AfterDeath|SelectCharacter|characterModel|CreatureCmd\.Damage|OnTryPurchaseInternal|STS2_111|STS2_112|Sts2TargetVersion|RitsuLibVersion' MeiLinModCode GodotScripts tests
```

Adapt source directories to the target repository.

Check three independent surfaces:

1. Source compilation against the installed game's assembly: catches invalid overrides and removed API calls.
2. Runtime patch registration: catches string/reflection targets and Harmony parameter injection. A patch method may compile while its target no longer exists.
3. Behavior reached only during play: death callbacks, Gloomy replacements, selection backgrounds, cinematic cleanup, and card result locations.

When inspecting an existing DLL, [scripts/Inspect-Sts2ModApi.ps1](scripts/Inspect-Sts2ModApi.ps1) reads metadata without executing mod code:

```powershell
& "<skill>/scripts/Inspect-Sts2ModApi.ps1" `
  -GameDirectory "D:\Steam\steamapps\common\Slay the Spire 2" `
  -ModDll "<actual loaded Mod.dll>" `
  -CecilDll "<Mono.Cecil.dll>" `
  -ReferenceDirectory "<RitsuLib>/compat/0.113.0", "<GodotSharp library directory>"
```

It reports unresolved game member references and candidate stale overrides separately. Neither a clean member-reference scan nor a clean build validates reflection patches, Harmony arguments, resources, or combat behavior. Unresolved base assemblies make the override scan incomplete; inspect those errors before interpreting its results. Unmatched generic/covariant overrides require manual review.

## Build Without Accidental Deployment

- Inspect custom MSBuild targets first. In MeiLinMod, ordinary `Build` copies to game directories and exports a PCK; `IsInnerGodotExport=true` suppresses export but does **not** suppress DLL/manifest copying.
- For diagnosis, use `dotnet build <project> -t:Compile` with the supported version property's path explicitly pointing to the installed 0.113 copy. State that the existing symbol is being used only to audit the old source.
- A missing global NuGet fallback folder is an environment failure, not a game API break. A process-local `NUGET_FALLBACK_PACKAGES` pointing to an existing package directory resolved the observed SDK failure. Respect permission requirements for registry reads and external writes.
- Do not run TestTheSpire while the main project cannot compile, or run an old test DLL and report it as verification of new source.
- If exporting registers Spine twice, inspect `.godot/extension_list.cfg` for imported temporary project copies before replacing the editor or extension. Exclude scratch projects from import/export and repair only confirmed duplicate cache entries.

## Implement Only When Migration Is Requested

- Add `113` target selection, `Sts2Path113`/`STS2_113_PATH`, validation, `STS2_113`, and propagation into nested Godot exports when the repository uses version routing.
- Extend existing compatible version branches individually. MeiLin's `HuoLongXinZang` and `QuanXinQuanLingPower` need the `CardLocation` branch on 0.113; adding only a new project symbol would incorrectly select their old fallback APIs.
- Update the death override and patch target independently. Preserve local-player checks, `wasRemovalPrevented` behavior, summon counts, and cleanup ownership.
- For character-select patches, derive the character from `charSelectButton.Character`; remove injection of the deleted `characterModel` original argument. Inspect every patch on the method.
- Match compile-time framework packages to a verified 0.113 runtime. RitsuLib `0.6.11` was verified locally on 2026-11-13; this is not a claim about the latest or minimum compatible release. Preserve the loader and its split runtime assemblies.
- With RitsuLib 0.6.11, use `RunHistoryFiltered` and filter `TelemetryCaptureContext.SourceData` as `RunEndedEvent` to preserve character-scoped telemetry. See the reference for the old overload trap.
- Align manifests, generated test manifests, dependency minimums, version assertions, and distribution version with the chosen supported targets.
- Preserve model IDs, localization keys, shared settings, save compatibility, and encounter/token ownership across independently usable mods. Do not turn skin mods into gameplay mods merely to resolve a patch failure.

## Focused Verification

After source fixes, compile once and rerun only after a meaningful change or failure. Use existing focused tests for patch-target resolution and affected behavior. For MeiLin, prioritize `PatchRegistrationTests`, overlay death cleanup, Gloomy beast death/replacement, and the two `CardLocation` cards. Add a behavioral regression only where existing coverage misses the changed contract.

TestTheSpire 0.1.2 and 0.1.4 binaries reference an obsolete `RunState.CreateForTest` signature. Rebuild a workspace copy of its source against 0.113 before running; the reference explains the compatible signature and MeiLin helper. For cards that immediately return to hand, wait for the queued play action instead of using a helper that waits to observe the card absent from hand. Keep test saves isolated and remove only test payloads created by the session after verification.

For skin/animation mods, verify character selection, backgrounds, combat start, card animations, death, cinematic cancellation, and return to menu as applicable.

Check abandonment/game-over death separately from combat `Dead` triggers. RitsuLib's game-over path can call `TryPlayCue(..., "die")` directly; MeiLin's Spine clip is `death`. A native Godot missing-animation error may not throw a C# exception, so an exception-based alias fallback can incorrectly report success on its first invalid name. Prefer a character-scoped cue correction over a global animation rename.

If a custom selection sound reaches the game's missing-SFX logger despite successful `SfxCmd` patches, trace the actual audio-manager entry point. MeiLin needed a scoped `NAudioManager.PlayOneShot` fallback; do not add it indiscriminately to other mods. Verify one custom player per call and normal audio passthrough. For invalid resource UIDs, match existing script `.uid` files or retain a valid text path without a stale UID; load and instantiate the affected exported scenes.

Inspect the new log for the actual host version, zero target-mod patch failures, and complete initialization. Confirm the loader used the newly built artifacts. A trailing `Finished mod initialization` line can follow an exception and is not a success guarantee.

Report separately:

- Confirmed target-mod faults with source and log locations.
- Build/manifest migration work not yet performed.
- Other mods' failures and environment failures.
- Resource warnings and engine errors whose ownership is unproven.
- Behavioral checks actually completed and those still pending.
