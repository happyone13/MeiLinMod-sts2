using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class XinHuoXiangChuanPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;

        if (!BasicStrikeDefendHelper.IsStrikeOrDefendCard(cardPlay.Card))
            return;

        if (Owner.Player != null)
            await CardPileCmd.Draw(context, 1m, Owner.Player);

        var remainingStacks = Amount - 1m;
        await PowerCmd.Apply<XinHuoXiangChuanPower>(Owner, -1m, Owner, cardPlay.Card, silent: true);

        if (remainingStacks <= 0m)
            await PowerCmd.Remove(this);
    }
}
