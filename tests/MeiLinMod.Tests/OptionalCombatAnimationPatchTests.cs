using System.Reflection;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Patches;
using STS2RitsuLib.Patching.Models;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class OptionalCombatAnimationPatchTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-optional-combat-animation-patches");
    }

    [Fact]
    public async Task Optional_combat_animation_patches_stay_non_critical_with_stable_descriptions()
    {
        await InitializeBattle();

        Type[] patchTypes =
        [
            typeof(MeiLinBattleAnimationGenerateAnimatorPatch),
            typeof(MeiLinBattleAnimationSetAnimationPrefixPatch),
            typeof(MeiLinBattleAnimationSetAnimationPostfixPatch),
            typeof(MeiLinTriggerAnimPatch)
        ];

        foreach (var patchType in patchTypes)
        {
            Assert.False(ReadStatic<bool>(patchType, nameof(IPatchMethod.IsCritical)));
            Assert.False(string.IsNullOrWhiteSpace(ReadStatic<string>(patchType, nameof(IPatchMethod.PatchId))));
            Assert.False(string.IsNullOrWhiteSpace(ReadStatic<string>(patchType, nameof(IPatchMethod.Description))));
            Assert.NotEmpty(InvokeStatic<ModPatchTarget[]>(patchType, nameof(IPatchMethod.GetTargets)));
        }
    }

    [Fact]
    public async Task Combat_animation_patch_targets_keep_expected_animation_and_trigger_hooks()
    {
        await InitializeBattle();

        var targetDescriptions = new[]
            {
                typeof(MeiLinBattleAnimationGenerateAnimatorPatch),
                typeof(MeiLinBattleAnimationSetAnimationPrefixPatch),
                typeof(MeiLinBattleAnimationSetAnimationPostfixPatch),
                typeof(MeiLinTriggerAnimPatch)
            }
            .SelectMany(type => InvokeStatic<ModPatchTarget[]>(type, nameof(IPatchMethod.GetTargets)))
            .Select(target => target.ToString())
            .OrderBy(target => target, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(4, targetDescriptions.Length);
        Assert.Contains("CreatureCmd.TriggerAnim(Creature, String, Single)", targetDescriptions);
        Assert.Equal(2, targetDescriptions.Count(target => target == "MegaAnimationState.SetAnimation(String, Boolean, Int32)"));
        Assert.Contains(targetDescriptions, target => target.Contains("MeiLinMod.GenerateAnimator", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Trigger_anim_patch_only_intercepts_meilin_player_attacks_and_always_schedules_return()
    {
        await InitializeBattle();

        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Patches", "MeiLinTriggerAnimPatch.cs"));

        Assert.Contains("!string.Equals(triggerName, \"Attack\", StringComparison.Ordinal)", source);
        Assert.Contains("!creature.IsPlayer", source);
        Assert.Contains("MeiLinTarget.IsTarget(creature.Player)", source);
        Assert.Contains("return true;", source);
        Assert.Contains("MeiLinBattleAnimationService.ConsumeNextAttackSegment(creature)", source);
        Assert.Contains("MeiLinAnimationSequenceManager.BeginAction($\"attack:{segment.Command}\")", source);
        Assert.Contains("MeiLinAttackMovementController.MoveToTargetIfNeededAsync(caster, segment.Target)", source);
        Assert.Contains("if (segment.IsFirstSegment)", source);
        Assert.Contains("MeiLinAudioService.TryPlayAttackVoice(caster.Player)", source);
        Assert.Contains("queueEndAnimation: segment.RemainingSegments == 0", source);
        Assert.Contains("MeiLinAttackMovementController.ScheduleReturnAfterSegment", source);
        Assert.Contains("MeiLinAttackMovementController.ForceReturnSoon(caster, interruptedCommandName: segment.Command)", source);
    }

    [Fact]
    public async Task Movement_controller_keeps_abandoned_multihit_and_layer_restore_guards()
    {
        await InitializeBattle();

        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Vfx", "MeiLinAttackMovementController.cs"));

        Assert.Contains("private const float AbandonedSegmentReturnPadSeconds = 0.25f;", source);
        Assert.Contains("duration + AbandonedSegmentReturnPadSeconds", source);
        Assert.Contains("interruptedCommandName: commandName", source);
        Assert.Contains("MeiLinBattleAnimationService.AbortActiveAttack(caster)", source);
        Assert.Contains("RestoreLayer(casterNode, session);", source);
        Assert.Contains("ResetSession(session);", source);
        Assert.Contains("casterParent.MoveChild(casterNode, desiredIndex);", source);
        Assert.Contains("session.OriginalSiblingIndex", source);
        Assert.Contains("casterNode.ZIndex = Math.Max(casterNode.ZIndex, targetNode.ZIndex + 1);", source);
        Assert.DoesNotContain("CanvasLayer", source, StringComparison.Ordinal);
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
