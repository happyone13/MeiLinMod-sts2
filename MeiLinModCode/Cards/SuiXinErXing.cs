using BaseLib.Utils;
using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class SuiXinErXing() : MeiLinModCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private const string ProgressKey = "Progress";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(ProgressKey, 1m)];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StanceSwitchQiProgressPower>(Owner.Creature, DynamicVars[ProgressKey].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[ProgressKey].UpgradeValueBy(1m);
    }
}


