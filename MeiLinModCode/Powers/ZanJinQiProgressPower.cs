using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class ZanJinQiProgressPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || player != Owner.Player)
            return;

        await PowerCmd.Apply<QiPower>(Owner, 2m, Owner, null);

        var remainingTurns = Amount - 1m;
        await PowerCmd.Apply<ZanJinQiProgressPower>(Owner, -1m, Owner, null, silent: true);

        if (remainingTurns <= 0m)
            await PowerCmd.Remove(this);
    }
}
