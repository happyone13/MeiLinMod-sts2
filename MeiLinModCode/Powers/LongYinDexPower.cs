using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MeiLinMod.MeiLinModCode.Cards;

namespace MeiLinMod.MeiLinModCode.Powers;

public class LongYinDexPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrikeOrDefend(cardPlay.Card))
            return;

        await PowerCmd.Apply<LongYinTemporaryDexterityPower>(Owner, Amount, Owner, cardPlay.Card);
    }
}
