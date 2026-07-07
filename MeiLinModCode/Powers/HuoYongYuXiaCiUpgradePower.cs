using System.Collections.Generic;
using System.Linq;
using MeiLinMod.MeiLinModCode.Migration;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace MeiLinMod.MeiLinModCode.Powers;

public class HuoYongYuXiaCiUpgradePower : MeiLinModPower
{
    private static readonly Dictionary<Player, int> PendingUpgrades = new();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override int DisplayAmount => (int)Amount;

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner?.Player == null || Owner.IsDead)
            return Task.CompletedTask;

        var upgraded = TryUpgradeRandomCard(Owner.Player);
        MainFile.Logger.Info($"[HuoYongYuXiaCi] AfterCombatEnd upgrade attempted, success={upgraded}.");
        Flash();
        return Task.CompletedTask;
    }

    public static bool TryUpgradeRandomCard(Player? player)
    {
        if (player == null)
            return false;

        var deckPile = PileType.Deck.GetPile(player);
        var upgradableCards = deckPile.Cards
            .Where(c => c.IsUpgradable)
            .ToList();
        if (upgradableCards.Count == 0)
            return false;

        var picked = player.RunState.Rng.CombatCardSelection.NextItem(upgradableCards);
        if (picked == null)
            return false;

        CardCmd.Upgrade(picked);
        return true;
    }

    public static void QueueUpgrade(Player? player, int count = 1)
    {
        if (player == null || count <= 0)
            return;

        if (PendingUpgrades.TryGetValue(player, out var existing))
            PendingUpgrades[player] = existing + count;
        else
            PendingUpgrades[player] = count;
    }

    public static int ConsumePendingUpgrades(Player? player)
    {
        if (player == null)
            return 0;

        if (!PendingUpgrades.TryGetValue(player, out var count))
            return 0;

        PendingUpgrades.Remove(player);
        return count;
    }
}
