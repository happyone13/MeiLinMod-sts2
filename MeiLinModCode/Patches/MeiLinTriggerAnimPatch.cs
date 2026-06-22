using HarmonyLib;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using MeiLinMod.MeiLinModCode.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace MeiLinMod.MeiLinModCode.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim))]
public static class MeiLinTriggerAnimPatch
{
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
        await MeiLinAttackMovementController.MoveToTargetIfNeededAsync(caster, segment.Target);

        await MeiLinCommandVfxCoordinator.PlayCommandSegmentAsync(
            segment.Command,
            caster,
            segment.Target,
            waitTime,
            queueEndAnimation: segment.RemainingSegments == 0);

        MeiLinAttackMovementController.ScheduleReturnAfterSegment(
            caster,
            segment.Command,
            isFinalSegment: segment.RemainingSegments == 0);
    }
}
