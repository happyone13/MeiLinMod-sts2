using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class CanYingPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrikeOrDefend(card))
            return playCount;

        var extraTriggers = (int)decimal.Floor(Amount);
        if (extraTriggers <= 0)
            return playCount;

        if (XiangzuLegacyPower.IsInAttackStance(Owner) && BasicStrikeDefendHelper.IsBasicStrike(card))
            return playCount + extraTriggers;

        if (XiangzuLegacyPower.IsInGuardStance(Owner) && BasicStrikeDefendHelper.IsBasicDefend(card))
            return playCount + extraTriggers;

        return playCount;
    }

    public override async Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrikeOrDefend(card))
            return;

        var extraTriggers = (int)decimal.Floor(Amount);
        if (extraTriggers <= 0)
            return;

        if (!XiangzuLegacyPower.IsInAttackStance(Owner) && !XiangzuLegacyPower.IsInGuardStance(Owner))
            return;

        await PowerCmd.Decrement(this);
    }
}
