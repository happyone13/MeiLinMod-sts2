using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class ShouJin() : MeiLinModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const string BonusKey = "Bonus";

    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(7m, ValueProp.Move),
        new DynamicVar(BonusKey, 3m)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var qi = (int)(Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m);
        var block = DynamicVars.Block.BaseValue + (qi * DynamicVars[BonusKey].BaseValue);
        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move | ValueProp.Unpowered, null, fast: true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[BonusKey].UpgradeValueBy(2m);
    }
}


