using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Scaffolding.Content;

namespace MeiLinMod.MeiLinModCode.Powers;

public class YunQiPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Reuse previous LongYin power icon resources.
    public override PowerAssetProfile AssetProfile => new(
        IconPath: "long_yin_power.png".PowerImagePathOrDefault(),
        BigIconPath: "long_yin_power.png".BigPowerImagePathOrDefault());

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner || !BasicStrikeDefendHelper.IsStrikeOrDefendCard(cardPlay.Card))
            return;

        await PowerCmd.Apply<LongYinTemporaryStrengthPower>(new BlockingPlayerChoiceContext(), Owner, Amount, Owner, cardPlay.Card);
        await PowerCmd.Apply<LongYinTemporaryDexterityPower>(new BlockingPlayerChoiceContext(), Owner, Amount, Owner, cardPlay.Card);
    }
}
