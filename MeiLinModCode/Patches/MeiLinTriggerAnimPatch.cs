using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using MeiLinMod.MeiLinModCode.Services;
using MeiLinMod.MeiLinModCode.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class MeiLinTriggerAnimPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.Animation.TriggerAnim";

    public static bool IsCritical => false;

    public static string Description => "Route MeiLin attack TriggerAnim through custom animation and VFX flow";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim), typeof(Creature), typeof(string), typeof(float))
    ];

    public static bool Prefix(Creature creature, string triggerName, float waitTime, ref Task __result)
    {
        if (!string.Equals(triggerName, "Attack", StringComparison.Ordinal) ||
            !creature.IsPlayer)
        {
            return true;
        }

        bool isMeiLin = MeiLinTarget.IsTarget(creature.Player);
        MainFile.Logger.Info(
            $"[MeiLinTriggerAnimPatch] Player attack trigger. character={creature.Player?.Character?.Id.Entry ?? "<null>"}, isMeiLin={isMeiLin}");

        if (!isMeiLin)
            return true;

        var segment = MeiLinBattleAnimationService.ConsumeNextAttackSegment(creature);
        MainFile.Logger.Info(
            $"[MeiLinTriggerAnimPatch] Attack trigger intercepted. command={segment.Command}, remaining={segment.RemainingSegments}, hasTarget={segment.Target != null}");

        __result = PlayAttackSegmentAsync(creature, segment, waitTime);
        return false;
    }

    private static async Task PlayAttackSegmentAsync(
        Creature caster,
        MeiLinBattleAnimationService.AttackSegment segment,
        float waitTime)
    {
        using var actionScope = MeiLinAnimationSequenceManager.BeginAction($"attack:{segment.Command}");
        var completed = false;
        try
        {
            await MeiLinAttackMovementController.MoveToTargetIfNeededAsync(caster, segment.Target);

            if (segment.IsFirstSegment)
                MeiLinAudioService.TryPlayAttackVoice(caster.Player);

            await MeiLinCommandVfxCoordinator.PlayCommandSegmentAsync(
                segment.Command,
                caster,
                segment.Target,
                waitTime,
                queueEndAnimation: segment.RemainingSegments == 0);

            completed = true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinTriggerAnimPatch] Attack segment failed. command={segment.Command}, ex={ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (completed)
            {
                MeiLinAttackMovementController.ScheduleReturnAfterSegment(
                    caster,
                    segment.Command,
                    isFinalSegment: segment.RemainingSegments == 0);
            }
            else
            {
                MeiLinAttackMovementController.ForceReturnSoon(caster, interruptedCommandName: segment.Command);
            }
        }
    }
}
