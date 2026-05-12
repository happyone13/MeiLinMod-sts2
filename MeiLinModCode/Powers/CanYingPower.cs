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
        if (card.Owner?.Creature != Owner)
            return playCount;

        var extraTriggers = (int)decimal.Floor(Amount);
        if (extraTriggers <= 0)
            return playCount;

        if (XiangzuCombatState.IsInAttackStance(Owner) && BasicStrikeDefendHelper.IsStrikeCard(card))
            return playCount + extraTriggers;

        if (XiangzuCombatState.IsInGuardStance(Owner) && BasicStrikeDefendHelper.IsDefendCard(card))
            return playCount + extraTriggers;

        return playCount;
    }
}
