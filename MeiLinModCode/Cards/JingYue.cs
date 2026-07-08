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
public class JingYue() : MeiLinModCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private const string ProgressKey = "Progress";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(ProgressKey, 2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiGauge
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayPowerCastAnim();
        await PowerCmd.Apply<StanceSwitchQiProgressPower>(new BlockingPlayerChoiceContext(), Owner.Creature, DynamicVars[ProgressKey].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[ProgressKey].UpgradeValueBy(1m);
    }
}
