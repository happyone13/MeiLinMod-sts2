using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class StrikeDefendQiProgressPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner)
            return;

        if (!BasicStrikeDefendHelper.IsBasicStrikeOrDefend(cardPlay.Card))
            return;

        var legacy = Owner.GetPower<XiangzuLegacyPower>();
        if (legacy != null)
            await legacy.AddQiCounterProgress((int)Amount);
    }
}

