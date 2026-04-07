using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class NextBasicStrikeExtraPlayPower : MeiLinModPower
{
    private bool _processing;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (_processing)
            return;

        if (cardPlay.Card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrike(cardPlay.Card))
            return;

        _processing = true;
        try
        {
            await CardCmd.AutoPlay(context, cardPlay.Card.CreateDupe(), cardPlay.Target);

            if (Amount <= 1m)
            {
                await PowerCmd.Remove(this);
                return;
            }

            await PowerCmd.Apply<NextBasicStrikeExtraPlayPower>(Owner, -1m, Owner, cardPlay.Card, silent: true);
        }
        finally
        {
            _processing = false;
        }
    }
}
