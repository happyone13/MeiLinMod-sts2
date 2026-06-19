using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class ZhenLongAwakeningEnemyEmberNextTurnPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        foreach (var enemy in CombatState.HittableEnemies)
            await PowerCmd.Apply<EmberPower>(new BlockingPlayerChoiceContext(), enemy, Amount, Owner, null);

        await PowerCmd.Remove(this);
    }
}
