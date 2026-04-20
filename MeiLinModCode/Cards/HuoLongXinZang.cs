using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class HuoLongXinZang() : MeiLinModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool IsPlayable => (Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m) >= 1m;

    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded
        ? [CardKeyword.Retain]
        : [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        EnergyHoverTip,
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiConsume
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if ((Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m) < 1m)
            return;

        await PowerCmd.Apply<QiPower>(Owner.Creature, -1m, Owner.Creature, this);
        await PlayerCmd.GainEnergy(1m, Owner);
    }

    protected override PileType GetResultPileType()
    {
        var result = base.GetResultPileType();
        if (result == PileType.Discard)
            return PileType.Hand;

        return result;
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
