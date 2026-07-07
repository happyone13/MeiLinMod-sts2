using System;
using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class LongZhiJingShen() : MeiLinModCard(1, CardType.Power, CardRarity.Uncommon, TargetType.AnyPlayer)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        MeiLinHoverTipFactory.XiangzuLegacy,
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiGauge
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await PlayPowerCastAnim();
        await ApplyXiangzuLegacy(cardPlay.Target);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private async Task ApplyXiangzuLegacy(Creature target)
    {
        await PowerCmd.Apply<XiangzuLegacyPower>(new BlockingPlayerChoiceContext(), target, 1m, Owner.Creature, this);
        await PowerCmd.Remove<StanceYuPower>(target);
        await PowerCmd.Apply<StanceGongPower>(new BlockingPlayerChoiceContext(), target, 1m, Owner.Creature, this, silent: true);
    }
}
