using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class NextDefendDoublePlayPower : MeiLinModPower
{
    private bool _processing;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (_processing)
            return;

        if (cardPlay.Card.Owner.Creature != Owner || !BasicStrikeDefendHelper.IsBasicDefend(cardPlay.Card))
            return;

        _processing = true;
        try
        {
            await CardCmd.AutoPlay(context, cardPlay.Card.CreateDupe(), null);
            await PowerCmd.Remove(this);
        }
        finally
        {
            _processing = false;
        }
    }
}

