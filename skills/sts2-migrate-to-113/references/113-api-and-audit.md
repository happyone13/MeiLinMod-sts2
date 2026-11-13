# Verified 0.113 API Changes and Audit Evidence

## Evidence Boundary

Verified on 2026-11-13 against the installed Windows game:

- `release_info.json`: `v0.113.0`, commit `16ac0fe3`, build date `2026-11-12T18:09:24+00:00`.
- Game assembly: `D:\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll`.
- Latest log: `C:\Users\lozalia\AppData\Roaming\SlayTheSpire2\logs\godot.log`.
- Runtime: RitsuLib `0.6.11`, loader `Host=v0.113.0; runtime=0.113.0`.
- Engine: MegaDot `4.5.1-m.14`. The game update did not establish a need to upgrade the project's Godot SDK.

The changes below are verified differences between the previously compiled mod contracts and 0.113. They were not bisected against a 0.112 installation, so do not claim that each change originated specifically in 0.113.

The preceding rotated log, `godot2026-11-13T09.14.00.log`, reports `v0.111.0` and has no MeiLin initialization. Do not use it to claim a successful MeiLin regression comparison.

## Death Callback Adds a Card Source

Installed 0.113 metadata:

```csharp
public static Task Hook.AfterDeath(
    IRunState runState, ICombatState combatState,
    Creature creature, CardModel source,
    bool wasRemovalPrevented, float deathAnimLength);

public virtual Task AbstractModel.AfterDeath(
    PlayerChoiceContext choiceContext,
    Creature creature, CardModel source,
    bool wasRemovalPrevented, float deathAnimLength);
```

`source` is between `creature` and `wasRemovalPrevented`. Inspect nullability independently; the metadata display above describes parameter types, not nullable annotations.

MeiLin has two separate consequences:

- `MeiLinBattleReadyOverlayPatches.cs:84`: `GetTargets()` specifies the obsolete five-argument `Hook.AfterDeath`. Log lines 531-537 show this optional patch failing. Add `typeof(CardModel)` in the correct position when migrating. A Harmony postfix that does not need `source` can omit it; the target signature must still be complete and correct.
- `GloomyPrimeMonster.cs:154`: the old four-argument override fails compilation with CS0115. Add the source parameter when migrating. Its body queues pending replacements for dead allied beasts; preserve the filters and count behavior.

The installed old DLL can initialize even though the old virtual callback signature no longer matches the new callback slot. Do not infer that the new game will dispatch that old method. Gloomy's lost death bookkeeping is a static compatibility finding; an actual replacement combat was not run during the audit.

Existing `OptionalOverlayPatchTests.cs:80` expects the old signature and must change alongside the target. Existing Gloomy escape tests cover escape/token behavior, not the beast-death replacement path.

## Character Selection Removes the Character Argument

Installed 0.113 metadata:

```csharp
public virtual void NCharacterSelectScreen.SelectCharacter(
    NCharacterSelectButton charSelectButton);

public CharacterModel NCharacterSelectButton.Character { get; }
```

The old Yuki postfixes inject a `CharacterModel characterModel` parameter that the original method no longer has. Latest log lines 807 onward show:

```text
HarmonyException: Patching exception in ... NCharacterSelectScreen::SelectCharacter(...)
Parameter "characterModel" not found
```

Migration shape:

```csharp
public static void Postfix(
    NCharacterSelectScreen __instance,
    NCharacterSelectButton charSelectButton)
{
    CharacterModel characterModel = charSelectButton.Character;
    // Preserve the existing filtering and UI logic here.
}
```

Known Yuki source sites:

- `YukiModCode/Patches/YukiCharacterSelectBgToggleButtonPatch.cs:44`
- `YukiModCode/Patches/YukiCharacterSelectAnimLoopPatch.cs:22`

Search skin mods for the same pattern before changing them. This audit did not establish that Shu, Mark, or every skin mod uses it.

## Damage Collection Overload Gains an Optional Argument

Installed 0.113 metadata:

```csharp
public static Task<IEnumerable<DamageResult>> CreatureCmd.Damage(
    PlayerChoiceContext choiceContext,
    IEnumerable<Creature> targets,
    decimal amount, ValueProp props,
    Creature dealer, CardModel cardSource, CardPlay cardPlay,
    ModifyDamageHookType modifyDamageHookType = ModifyDamageHookType.All);
```

The optional enum default is metadata value `14`. Source calls can compile with an omitted optional argument, but old compiled method references and exact Harmony signatures still contain the old parameter count. Never treat source compatibility as binary compatibility.

MinionLib's old seven-argument collection damage patch fails target resolution at log lines 64-66. Inspect its full target declaration before implementing a repair.

MeiLin's 334 direct game member references all resolved against the installed 0.113 assembly, including the damage overloads it actually references. Do not make an unrelated rewrite of its damage code merely because MinionLib failed.

## Merchant Override Changes

Installed 0.113 metadata:

```csharp
protected abstract Task<(bool, int)> MerchantEntry.OnTryPurchaseInternal(
    MerchantInventory inventory, bool ignoreCost);
```

The method's metadata flags are `Family, Virtual, HideBySig, VtableLayoutMask, Abstract`.

GoldTransferMod fails type loading because `MerchantTransferGoldEntry` does not implement the new contract. The latest log contains `ReflectionTypeLoadException` and the nested `TypeLoadException` at lines 137-142. This is independent of MeiLin.

The reusable metadata script also identified its old `OnTryPurchase(...)` override and unresolved call to `OnTryPurchaseWrapper(...)`. The installed game exposes `public Task<bool> OnTryPurchaseUnsynchronized(MerchantInventory, bool)` instead of that wrapper. Inspect the implementation's synchronization and purchase-event behavior before choosing a replacement; a rename alone is not a verified repair.

## Build and Deployment Facts

MeiLin's audit baseline:

- Mod `v0.4.6`, project target `111`, RitsuLib compile package `0.5.11`.
- Manifest minimum game `v0.111.0`, RitsuLib minimum `0.5.11`.
- Project recognizes only targets `103`, `107`, `108`, `109`, `110`, `111`.
- Test project and migration assertions still target 0.111.
- `HuoLongXinZang.cs:42` and `QuanXinQuanLingPower.cs:32` select modern `CardLocation` behavior only for `STS2_109 || STS2_110 || STS2_111`.

The lower manifest minimum does not itself prevent loading on 0.113. The missing 113 routing and old assertions are migration work, not proof of an additional runtime crash.

Read-only audit compilation:

```powershell
$env:NUGET_FALLBACK_PACKAGES = (Resolve-Path -LiteralPath '.\packages').Path
dotnet build .\MeiLinMod.csproj -t:Compile `
  -p:Sts2Path111="D:\Steam\steamapps\common\Slay the Spire 2" `
  -p:IsInnerGodotExport=true -v:minimal
```

The existing 111 symbol deliberately selects the currently supported modern source branch while the reference path points to 0.113. This is diagnostic, not completed 113 routing.

Observed result: restore succeeded; compilation stopped at `GloomyPrimeMonster.AfterDeath`, CS0115. No gameplay source was edited and no migration DLL/PCK was deployed. Declaration errors can mask further body-level compiler errors, so compile again after a requested fix.

`CopyToModsFolderOnBuild` executes ordinary post-build copies even with `IsInnerGodotExport=true`. PCK export also uses `ContinueOnError=WarnAndContinue`; an overall build result alone does not prove a valid exported PCK.

The loaded Workshop DLL matched the local 2026-10-20 release copy:

```text
SHA256 FDD8EE41039911F2D55E00B273333E9A51A6D06B3070DC5F51ADDC0626AA87B3
```

A recursive Cecil metadata scan found 334 resolvable direct game member references and one unmatched override among 1226 virtual overrides: Gloomy's `AfterDeath`. These counts include nested types but exclude reflection targets and runtime behavior.

## Resource Warnings and Unassigned Errors

MeiLin's VFX startup prewarm loaded `52/52`, with `failed=0`. Of 41 patch targets applied across seven groups, 40 applied and the overlay death target failed.

Warnings:

- `meilin_bg.tscn:5` uses script UID `dnwqjs0fcnjsx`, while the local script `.uid` contains `ulv7r824rm8i`.
- `meilin_bg.tscn:6` and `meirin_attack_play1_root.tscn:4` use material UID `cs88ys720nr1b` which the runtime cannot resolve.
- Godot explicitly falls back to the resource text paths. No missing VFX resource was established by these warnings. Repair references/import data before re-export when requested; avoid a bulk rewrite of all UIDs without evidence.

`Invalid Task ID` appears at lines 1890 and 1896 in a Godot script bridge stack. An older 0.111 log also contains the error without MeiLin loaded. Its ownership is unproven.

Exit-time RID/resource leak messages occur after explicit game shutdown in a multi-mod session. They do not establish a MeiLin-specific or 0.113-specific leak without isolation.

Other failures in the same session:

- Yuki's death overlay patch fails independently of its character-selection initializer failure.
- MultiplayerModSync declares a missing DLL: packaging/install fault.
- Yusha initializes, but BaseLib prints unsupported saved-property warnings. This does not prove Yusha save corruption or justify replacing its RitsuLib serialization.
- Shu and Mark appear in older 0.111 logs, not this 0.113 session. Their current compatibility remains unverified.

## RitsuLib 0.6.11 Telemetry Migration

The previous two-argument `TelemetryRequest.RunHistory(description, captureFilter)` call does not compile against 0.6.11. Supplying `sharedContributionSubscriptions: null` selects an obsolete overload. The current filtered API is:

```csharp
TelemetryRequest.RunHistoryFiltered(description, captureFilter: IsMeiLinCapture);

private static bool IsMeiLinCapture(TelemetryCaptureContext context)
{
    return context.SourceData is RunEndedEvent evt && IsMeiLinRun(evt);
}
```

Preserve the existing `IsMeiLinRun` character predicate. Other capture data must return false; this adapter must not widen collection to unrelated characters. The existing consent and applicant configuration stays intact.

## Test Framework Binary Compatibility

Installed 0.113 adds `isCustomGame` before `ascensionLevel`:

```csharp
RunState.CreateForTest(
    IReadOnlyList<Player>? players = null,
    IReadOnlyList<ActModel>? acts = null,
    IReadOnlyList<ModifierModel>? modifiers = null,
    GameMode gameMode = GameMode.Standard,
    bool isCustomGame = false,
    int ascensionLevel = 0,
    string? seed = null);
```

TestTheSpire NuGet 0.1.2 and 0.1.4 both contain the old method reference without the bool, so merely upgrading between these packages does not fix the runner.

MeiLin's `tools/build_testthespire_113.ps1` copies the sibling TestTheSpire project and `Framework` sources into `tmp/testthespire-113` and rebuilds them against the installed game, leaving the original framework repository untouched. Its named-argument source call `RunState.CreateForTest(players, seed: seed)` needs no source change. The test project accepts `TestTheSpireAssemblyPath` and discovers that staged build by default. Explicitly copy the rebuilt DLL into the test-mod payload as well; successful compilation alone did not populate that payload during the first migration attempt.

TestTheSpire's `Play` helper waits until a card is absent from hand. `HuoLongXinZang` can return before the next polled frame, causing a false timeout although its effect succeeded. For this case use `EnqueueNetworkAction(new PlayCardAction(card, null))`, then assert qi consumed, energy gained, and final hand/discard membership.

On Windows, a direct GUI-executable shell invocation returned before test completion. `Start-Process -WindowStyle Hidden -PassThru`, redirected stdout/stderr, `WaitForExit`, and explicit exit-code/summary checks reliably tracked the run. A zero-length new `godot.log` and a rotated historical log do not prove that tests ran.

## MeiLin Migration Verification

The requested implementation changed MeiLin to v0.4.7 / target 113 / RitsuLib 0.6.11, fixed both death signatures, retained modern card-location branches, and adapted the telemetry predicate. Main and test C# builds passed with zero warnings/errors.

The isolated 0.113 game run loaded the new v0.4.7 DLL from the local test mods directory, selected RitsuLib's 0.113.0 branch, applied all 41 MeiLin patch targets across seven groups, and prewarmed VFX 52/52 with zero failures. All four new migration combat tests passed, including actual Gloomy death-to-replacement dispatch and the patched overlay hook's prevented-death/local-player guards.

The existing suite's first complete run passed 72/73; the remaining assertion omitted the existing `UltimateCinematicsEnabled` setting. Update that expected schema without removing the setting or changing the shared file/domain keys. Replay-animation registration assertions were also stale at 37 classes/39 targets; the existing implementation has 39 classes/41 targets.

After that test-only schema correction, the settings group passed 4/4 with process exit code zero. Across the complete run and targeted rerun, all 73 distinct cases have a passing result and none are skipped. This is combined coverage, not a claim that the first complete run was green.

The first export encountered duplicate Spine registration with both available 4.5.1 editors. Follow-up inspection found two entries in `.godot/extension_list.cfg`: the real `res://addons/spine/spine_godot_extension.gdextension` and a temporary `res://tmp/roswitha_delivery/sources/project/bin/spine_godot_extension.gdextension`. Removing only the temporary entry and keeping scratch projects ignored produced a clean export with zero warnings/errors using the configured editor. This was a local import-cache fault, not evidence that both editors were incompatible.

## Follow-up Audio and Resource Repairs

The real graphical session logged `cannot find sfx path: meilin_select` despite successful registration of the three existing `SfxCmd` targets. The installed audio manager exposes both a forwarding convenience overload and the following inner entry point:

```csharp
NAudioManager.PlayOneShot(
    string name, Dictionary<string, float> parameters, float volume);
```

MeiLin now also patches this three-argument overload and routes recognized custom keys through its existing custom audio handler. This catches calls that bypass the higher-level interception; the evidence does not isolate JIT inlining as the sole cause. Preserve unrelated sound passthrough, replay suppression, custom gains, and player-only death handling. The new behavioral test invokes the real manager, checks exactly one new `AudioStreamPlayer`, verifies its resource and playing state, and stops it during cleanup.

The background script UID now matches `ulv7r824rm8i`. The invalid material UID was removed from `meilin_bg.tscn`, `meilin_character_camp.tscn`, and `meirin_attack_play1_root.tscn`, retaining the valid resource path; the material has no declared UID to substitute. A behavioral test loads and instantiates all three scenes from the exported PCK.

Final v0.4.7 verification: single complete run 75/75 passed, no skips, exit zero, empty stderr; D3D12 graphical audio subset 5/5 passed, exit zero, with only the engine's unsupported PSO caching warning. All 42 patch targets applied and VFX prewarm remained 52/52. The installed DLL and PCK hashes matched staging.

Do not treat these checks as visual inspection of all animations, multiplayer testing, or a fix for the separate D3D12 allocation failure observed when system available memory was about 0.39 GiB. An old Mark save referencing an unloaded Mark character also remains separate from MeiLin compatibility; do not delete or rewrite that save as a MeiLin repair.

## Abandonment Death Cue

A subsequent real session logged `Can not find animation: die` immediately after abandoning an in-progress MeiLin run. The stack is `RunManager.AbandonInternal` -> `GuaranteeKillAllPlayers` -> `CreatureCmd.Kill` -> game-over screen -> RitsuLib `CharacterGameOverScreenCompatibilityPatch.TryPlayDeathAnimation` -> `ModCreatureVisualPlayback.TryPlayCue` -> `TryPlaySpine`.

The game-over handler requests `die` directly, bypassing MeiLin's correct combat animator mapping `Dead` -> `death`. `TryPlaySpine` attempts alias names with `SetAnimation` inside a C# try/catch. The native Spine binding logs the missing clip without necessarily throwing, so it can return success before trying `death`. Do not fix this by changing the combat animator or death gameplay.

MeiLin's local `MeiLinDeathAnimationCuePatch` prefixes the public `TryPlayCue(NCreatureVisuals, CharacterModel, string, ReadOnlySpan<string>, bool)` entry point and changes only MeiLin's primary `die` cue to `death`. It preserves normal RitsuLib playback and other characters. Harmony resolves and applies this target with the span argument omitted from the prefix parameters.

For graphical regression tests, the framework enables game `TestMode` even when using D3D12. `Creature.CreateVisuals()` returns null in that mode. Use the character's existing `RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>` path to construct a fresh model, attach it to the tree, then invoke the real game-over cue API and inspect its Spine track. Do not interpret the test-mode null as an actual game-over model failure.

## Yuki Dynamic Variables and Migration Verification

Yuki's independent compile revealed another removed virtual member: `DynamicVar.GetBaseValueForIConvertible`. Installed 0.113 metadata shows `get_ShownValue` converting `PreviewValue` to an integer, and numeric conversion methods and `GetSourceValue` using `ShownValue`.

`ZhaoJia` already updates `PreviewValue` through its existing card-preview hook. Retain its old conversion override only under `#if !STS2_113`; no substitute override or gameplay rewrite is needed. The new combat regression plays a prior card, then checks preview value 6, `Convert.ToDecimal` value 6, and an actual block gain of 6. All three passed.

Yuki v0.1.8 now targets 113 and RitsuLib 0.6.11. Its fixes include both character-select postfixes, the overlay death target, Gloomy's death override, character-scoped telemetry, the dynamic-variable override, and the scoped game-over `die` -> `death` cue correction. Its selection-reticle scene retained a valid resource text path while dropping a stale texture UID. No custom audio-manager fallback was needed in the actual Yuki selection test.

Verification on the same installed 0.113 host:

- Main build, fresh Godot PCK export, and test build: zero warnings/errors.
- One complete regression run: 37/37 passed, no skips, process exit zero, empty stderr.
- D3D12 graphical migration subset: 5/5 passed; actual character selection, both loaded backgrounds, the visible background toggle, and the death animation were exercised. Selection and death screenshots were inspected. The initial graphical run's texture UID warning was repaired before the final export and complete regression.
- Installed-DLL metadata scan: 361 direct game references resolved and 963 virtual overrides checked with no mismatch or unresolved base assemblies.
- Optional content/presentation patch groups: 1/1 and 6/6 targets applied. VFX prewarm: 48/48, failed zero.

Use the game's actual selection scene in the graphical selection test: TestTheSpire's test-mode creation path can skip normal UI setup. Invoke the real `SelectCharacter` method and inspect Harmony's registered postfixes as well as the visible result. A compiling postfix alone is insufficient.

Yuki's tests use a workspace-built 0.113 TestTheSpire DLL, isolated test saves, and temporary test-only mod payloads. Remove only those payloads after verification. These checks do not establish a complete manual run, live network multiplayer compatibility, or validation of retained older version routes.
