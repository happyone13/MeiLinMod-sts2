using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class BuMiePower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await ApplyToAllEnemies(cardSource);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || player != Owner.Player)
            return;

        await ApplyToAllEnemies(null);
    }

    private async Task ApplyToAllEnemies(CardModel? cardSource)
    {
        foreach (var enemy in CombatState.HittableEnemies)
            await PowerCmd.Apply<EnemyEmberHalfDecayPower>(enemy, 1m, Owner, cardSource, silent: true);
    }
}
