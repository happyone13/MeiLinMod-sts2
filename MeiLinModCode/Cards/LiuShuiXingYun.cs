using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class LiuShuiXingYun() : MeiLinModCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private const string CountKey = "Count";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(CountKey, 4m)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayPowerCastAnim();
        if (IsUpgraded)
            await PowerCmd.Apply<LiuShuiXingYunUpgradedPower>(new BlockingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, this);
        else
            await PowerCmd.Apply<LiuShuiXingYunPower>(new BlockingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[CountKey].UpgradeValueBy(-1m);
    }
}


