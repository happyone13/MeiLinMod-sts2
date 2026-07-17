using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Encounters;

public sealed class GloomyEscapeCardBeforeCombatStartPatch : IPatchMethod
{
    public static string PatchId => "MeiLinMod.GloomyEscapeCard.BeforeCombatStart";

    public static bool IsCritical => false;

    public static string Description => "Give each player one Gloomy escape token before opening-hand draw";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method(typeof(Hook), nameof(Hook.BeforeCombatStart), typeof(IRunState), typeof(ICombatState))
    ];

    public static void Postfix(ICombatState? combatState, ref Task __result)
    {
        __result = AddCardsAfterOriginal(__result, combatState);
    }

    private static async Task AddCardsAfterOriginal(Task original, ICombatState? combatState)
    {
        await original;

        if (combatState?.Encounter is not GloomyPackEncounter encounter
            || encounter.WasPlayerEscape
            || encounter.EscapeCardsDealt)
        {
            return;
        }

        try
        {
            // Player ordering is stable and must be identical on every multiplayer peer.
            foreach (var player in combatState.Players)
            {
                var card = combatState.CreateCard<GloomyEscape>(player);
                await CardPileCmd.AddGeneratedCardToCombat(
                    card,
                    PileType.Hand,
                    player,
                    CardPilePosition.Top);
            }

            encounter.MarkEscapeCardsDealt();
        }
        catch (Exception ex)
        {
            // A compatibility failure must not prevent the combat itself from starting.
            MainFile.Logger.Warn("[GloomyEscapeCard] Failed to deal escape tokens: " + ex.Message);
        }
    }
}
