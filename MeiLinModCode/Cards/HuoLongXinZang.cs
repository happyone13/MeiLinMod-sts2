using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class HuoLongXinZang() : MeiLinModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool IsPlayable => XiangzuCombatState.HasQi(Owner.Creature);
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded
        ? [CardKeyword.Retain]
        : [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        EnergyHoverTip,
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiConsume
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!await XiangzuCombatState.TryConsumeQi(Owner.Creature, 1, Owner.Creature, this))
            return;

        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
    }

#if STS2_108
    protected override (PileType, CardPilePosition) GetResultPileTypeAndPositionForCardPlay()
    {
        var result = base.GetResultPileTypeAndPositionForCardPlay();
        if (result.Item1 == PileType.Discard)
            return (PileType.Hand, result.Item2);

        return result;
    }
#else
    protected override PileType GetResultPileTypeForCardPlay()
    {
        var result = base.GetResultPileTypeForCardPlay();
        return result == PileType.Discard
            ? PileType.Hand
            : result;
    }
#endif

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
