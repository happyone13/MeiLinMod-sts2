using System.Reflection;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Patches;
using MeiLinMod.MeiLinModCode.Vfx;
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
                typeof(MeiLinTriggerAnimPatch)
            }
            .SelectMany(type => InvokeStatic<ModPatchTarget[]>(type, nameof(IPatchMethod.GetTargets)))
            .Select(target => target.ToString())
            .OrderBy(target => target, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(1, targetDescriptions.Length);
        Assert.Contains("CreatureCmd.TriggerAnim(Creature, String, Single)", targetDescriptions);
        Assert.DoesNotContain("MegaAnimationState.SetAnimation(String, Boolean, Int32)", targetDescriptions);
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

    [Fact]
    public async Task Timeline_generation_invalidates_the_previous_lease_for_the_same_caster()
    {
        await InitializeBattle();

        MeiLinVfxPrewarmReport prewarm =
            new MeiLinVfxPrewarmReport(3, 2) + new MeiLinVfxPrewarmReport(4, 4);
        Assert.Equal(7, prewarm.Requested);
        Assert.Equal(6, prewarm.Loaded);
        Assert.Equal(1, prewarm.Failed);

        Type generationType = typeof(MeiLinCommandVfxCoordinator).Assembly.GetType(
            "MeiLinMod.MeiLinModCode.Vfx.MeiLinTimelineGeneration",
            throwOnError: true)!;
        MethodInfo begin = generationType.GetMethod(
            "Begin",
            BindingFlags.Public | BindingFlags.Static)!;

        object first = begin.Invoke(null, [Player.Creature])!;
        PropertyInfo isCurrent = first.GetType().GetProperty("IsCurrent")!;
        Assert.True(Assert.IsType<bool>(isCurrent.GetValue(first)));

        object second = begin.Invoke(null, [Player.Creature])!;
        Assert.False(Assert.IsType<bool>(isCurrent.GetValue(first)));
        Assert.True(Assert.IsType<bool>(isCurrent.GetValue(second)));
    }

    [Fact]
    public async Task Battle_vfx_deep_warm_is_staged_and_room_scoped()
    {
        await InitializeBattle();

        string source = File.ReadAllText(RepoFile("MeiLinModCode", "Vfx", "MeiLinBattleVfxPrewarmer.cs"));
        string migration = File.ReadAllText(RepoFile("MeiLinModCode", "Migration", "MeiLinRitsuMigration.cs"));

        Assert.Contains("RegisterPatch<MeiLinBattleVfxWarmPatch>()", migration);
        Assert.Contains("await NextFrame(room);", source);
        Assert.Contains("ReferenceEquals(NCombatRoom.Instance, room)", source);
        Assert.Contains("generation == Volatile.Read(ref _generation)", source);
        Assert.Contains("MeiLinModConfig.UseCombatEffects", source);
        Assert.Contains("WarmAlpha = 0.001f", source);
        Assert.Contains("MeiLinAttackMovementController.GetPreloadScenePaths()", source);
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
