using System.Threading;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.MeiLinModCode.Services;

public static class MeiLinReplayVisuals
{
    private static readonly AsyncLocal<CardModel?> ActiveCard = new();

    internal static CardModel? CurrentCard
    {
        get => ActiveCard.Value;
        set => ActiveCard.Value = value;
    }

    public static bool ShouldSkip(Creature? caster = null)
    {
        var card = CurrentCard;
        return card is { IsMutable: true, CurrentPlayIndex: > 0 }
            && card.Owner?.Character is MeiLinCharacter
            && (caster == null || card.Owner.Creature == caster);
    }
}

public sealed class MeiLinReplayVisualContextPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.Animation.ReplayContext";
    public static bool IsCritical => false;
    public static string Description => "Scope replay visuals to the current MeiLin card play";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(CardModel), nameof(CardModel.OnPlayWrapper),
            typeof(PlayerChoiceContext), typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool))
    ];

    public static void Prefix(CardModel __instance, out CardModel? __state)
    {
        __state = MeiLinReplayVisuals.CurrentCard;
        MeiLinReplayVisuals.CurrentCard = __instance;
    }

    // The original async method has captured this play's context before returning its Task.
    // Restore the caller immediately so nested/autoplayed cards get their own scope.
    public static void Postfix(CardModel? __state) => MeiLinReplayVisuals.CurrentCard = __state;
    public static void Finalizer(CardModel? __state) => MeiLinReplayVisuals.CurrentCard = __state;
}

public sealed class MeiLinReplayAttackAnimationPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.Animation.SkipReplayAttack";
    public static bool IsCritical => false;
    public static string Description => "Keep replay damage while skipping repeated MeiLin attacker animations";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(AttackCommand), nameof(AttackCommand.Execute), typeof(PlayerChoiceContext))
    ];

    public static void Prefix(AttackCommand __instance)
    {
        if (__instance.CardPlay is { IsFirstInSeries: false } play
            && play.Card.Owner?.Character is MeiLinCharacter)
            __instance.WithNoAttackerAnim();
    }
}
