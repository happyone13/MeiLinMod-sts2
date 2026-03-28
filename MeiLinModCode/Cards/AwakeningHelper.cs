using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Cards;

public static class AwakeningHelper
{
    public const int DefaultEnergySpentThreshold = 3;

    public static bool IsAwakened(CardPlay cardPlay, int energySpentThreshold = DefaultEnergySpentThreshold)
    {
        var owner = cardPlay.Card.Owner;
        var currentEnergy = owner.PlayerCombatState?.Energy ?? 0;
        var energyAtPlayStart = currentEnergy + cardPlay.Resources.EnergySpent;
        return energyAtPlayStart >= energySpentThreshold;
    }

    public static bool CanAwakenNow(CardModel card, int energySpentThreshold = DefaultEnergySpentThreshold)
    {
        return (card.Owner.PlayerCombatState?.Energy ?? 0) >= energySpentThreshold;
    }
}
