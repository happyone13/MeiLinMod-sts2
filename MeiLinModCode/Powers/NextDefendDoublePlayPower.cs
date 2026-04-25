using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class NextDefendDoublePlayPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsDefendCard(card))
            return playCount;

        var extraPlays = (int)decimal.Floor(Amount);
        if (extraPlays <= 0)
            return playCount;

        return playCount + extraPlays;
    }

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsDefendCard(card))
            return;

        await PowerCmd.Decrement(this);
    }
}
