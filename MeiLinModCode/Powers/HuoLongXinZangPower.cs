using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class HuoLongXinZangPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || player != Owner.Player)
            return;

        if ((Owner.GetPower<QiPower>()?.Amount ?? 0m) <= 0m)
            return;

        await PowerCmd.Apply<QiPower>(Owner, -1m, Owner, null);
        await PlayerCmd.GainEnergy(1m, player);
        await CardPileCmd.Draw(choiceContext, 1m, player);
    }
}
