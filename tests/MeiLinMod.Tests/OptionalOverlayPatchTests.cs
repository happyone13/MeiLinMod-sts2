using System.Reflection;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using STS2RitsuLib.Patching.Models;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class OptionalOverlayPatchTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-optional-overlay-patches");
    }

    [Fact]
    public async Task Optional_overlay_patches_stay_non_critical_with_stable_descriptions()
    {
        await InitializeBattle();

        Type[] overlayPatchTypes =
        [
            typeof(MeiLinBattleReadyAfterCombatVictoryPatch),
            typeof(MeiLinBattleReadyAfterDeathPatch),
            typeof(MeiLinBattleReadyBeforeCardPlayedPatch),
            typeof(MeiLinBattleReadyBeforeCombatStartPatch),
            typeof(MeiLinBattleReadyCancelPlayCardPatch),
            typeof(MeiLinBattleReadyControllerCardPlayStartPatch),
            typeof(MeiLinBattleReadyHandFocusPatch),
            typeof(MeiLinBattleReadyHandHoverEffectsPatch),
            typeof(MeiLinBattleReadyHandMousePressedPatch),
            typeof(MeiLinBattleReadyHandUnfocusPatch),
            typeof(MeiLinBattleReadyMouseCardPlayStartPatch)
        ];

        foreach (var patchType in overlayPatchTypes)
        {
            Assert.False(ReadStatic<bool>(patchType, nameof(IPatchMethod.IsCritical)));
            Assert.False(string.IsNullOrWhiteSpace(ReadStatic<string>(patchType, nameof(IPatchMethod.PatchId))));
            Assert.False(string.IsNullOrWhiteSpace(ReadStatic<string>(patchType, nameof(IPatchMethod.Description))));
            Assert.NotEmpty(InvokeStatic<ModPatchTarget[]>(patchType, nameof(IPatchMethod.GetTargets)));
        }
    }

    [Fact]
    public async Task Overlay_patch_targets_keep_expected_input_and_combat_hooks()
    {
        await InitializeBattle();

        var targetDescriptions = new[]
            {
                typeof(MeiLinBattleReadyBeforeCombatStartPatch),
                typeof(MeiLinBattleReadyAfterCombatVictoryPatch),
                typeof(MeiLinBattleReadyAfterDeathPatch),
                typeof(MeiLinBattleReadyHandFocusPatch),
                typeof(MeiLinBattleReadyHandUnfocusPatch),
                typeof(MeiLinBattleReadyHandMousePressedPatch),
                typeof(MeiLinBattleReadyMouseCardPlayStartPatch),
                typeof(MeiLinBattleReadyControllerCardPlayStartPatch),
                typeof(MeiLinBattleReadyHandHoverEffectsPatch),
                typeof(MeiLinBattleReadyCancelPlayCardPatch),
                typeof(MeiLinBattleReadyBeforeCardPlayedPatch)
            }
            .SelectMany(type => InvokeStatic<ModPatchTarget[]>(type, nameof(IPatchMethod.GetTargets)))
            .Select(target => target.ToString())
            .OrderBy(target => target, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(11, targetDescriptions.Length);
        Assert.Contains("Hook.BeforeCombatStart(IRunState, ICombatState)", targetDescriptions);
        Assert.Contains("Hook.AfterCombatVictory(IRunState, ICombatState, CombatRoom)", targetDescriptions);
        Assert.Contains("Hook.AfterDeath(IRunState, ICombatState, Creature, Boolean, Single)", targetDescriptions);
        Assert.Contains("NHandCardHolder.OnFocus", targetDescriptions);
        Assert.Contains("NHandCardHolder.OnUnfocus", targetDescriptions);
        Assert.Contains("NHandCardHolder.OnMousePressed(InputEvent)", targetDescriptions);
        Assert.Contains("NHandCardHolder.DoCardHoverEffects(Boolean)", targetDescriptions);
        Assert.Contains("NMouseCardPlay.Start", targetDescriptions);
        Assert.Contains("NControllerCardPlay.Start", targetDescriptions);
        Assert.Contains("NCardPlay.CancelPlayCard", targetDescriptions);
        Assert.Contains("Hook.BeforeCardPlayed(CombatState, CardPlay)", targetDescriptions);
    }

    [Fact]
    public async Task Overlay_source_preserves_click_and_unfocus_state_rules()
    {
        await InitializeBattle();

        var patchesSource = File.ReadAllText(RepoFile("MeiLinModCode", "Mechanics", "CardHoldOverlay", "MeiLinBattleReadyOverlayPatches.cs"));
        var hoverSource = File.ReadAllText(RepoFile("MeiLinModCode", "Mechanics", "CardHoldOverlay", "MeiLinCharacterHoverAnimation.cs"));
        var overlaySource = File.ReadAllText(RepoFile("MeiLinModCode", "Mechanics", "CardHoldOverlay", "MeiLinBattleReadyOverlay.cs"));

        Assert.Contains("Input.IsMouseButtonPressed(MouseButton.Left)", patchesSource);
        Assert.Contains("MeiLinCharacterHoverAnimation.NotifyClicked(card!)", patchesSource);
        Assert.Contains("return;", patchesSource);
        Assert.Contains("InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }", patchesSource);
        Assert.Contains("MeiLinBattleReadyOverlay.NotifyHovered(card!, hovered: true)", patchesSource);
        Assert.Contains("MeiLinAnimationSequenceManager.NotifyBattleIdleRequested(card, MeiLinBattleIdleRequest.MouseClick)", hoverSource);
        Assert.Contains("private const float OutDelaySeconds = 0.2f;", overlaySource);
        Assert.Contains("private const float CancelOutDelaySeconds = 0.8f;", overlaySource);
    }

    [Fact]
    public async Task Character_hover_animation_sequences_preserve_b_idle_transition_shape()
    {
        await InitializeBattle();

        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Mechanics", "CardHoldOverlay", "MeiLinAnimationSequenceManager.cs"));

        Assert.Contains("[\"enter_b_idle\"] = new(\"idle_to_b_idle\", false, \"b_idle\", true)", source);
        Assert.Contains("[\"exit_b_idle\"] = new(\"b_idle_to_idle\", false, \"idle\", true)", source);
        Assert.Contains("[\"attack_end\"] = new(\"attack_end\", false, \"b_idle_to_idle\", false, \"idle\", true)", source);
        Assert.Contains("private const double CardPlayFocusSuppressSeconds = 2.5;", source);
        Assert.Contains("IsCurrentAnimation(creatureNode, \"b_idle\")", source);
        Assert.Contains("IsCurrentAnimation(creatureNode, \"idle_to_b_idle\")", source);
        Assert.Contains("IsCurrentAnimation(node, \"idle\")", source);
        Assert.Contains("IsCurrentAnimation(node, \"b_idle_to_idle\")", source);
    }

    private async Task InitializeBattle()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);
    }

    private static T ReadStatic<T>(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(property);
        return Assert.IsType<T>(property.GetValue(null));
    }

    private static T InvokeStatic<T>(Type type, string methodName)
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<T>(method.Invoke(null, null));
    }

    private static string RepoFile(params string[] segments)
    {
        return Path.Combine(RepositoryRoot(), Path.Combine(segments));
    }

    private static string RepositoryRoot([System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
    {
        var testDirectory = Path.GetDirectoryName(sourcePath)
                            ?? throw new InvalidOperationException("CallerFilePath did not provide a source directory.");

        return Path.GetFullPath(Path.Combine(testDirectory, "..", ".."));
    }
}
