using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Powers;

public class YunQiPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Reuse previous LongYin power icon resources.
    public override string CustomPackedIconPath => "long_yin_power.png".PowerImagePathOrDefault();
    public override string CustomBigIconPath => "long_yin_power.png".BigPowerImagePathOrDefault();

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner || !BasicStrikeDefendHelper.IsBasicStrikeOrDefend(cardPlay.Card))
            return;

        await PowerCmd.Apply<LongYinTemporaryStrengthPower>(Owner, Amount, Owner, cardPlay.Card);
        await PowerCmd.Apply<LongYinTemporaryDexterityPower>(Owner, Amount, Owner, cardPlay.Card);
    }
}
