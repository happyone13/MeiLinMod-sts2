using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class TuNa() : MeiLinModCard(0, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
{
    private const string ProgressKey = "Progress";
    private const string EmberKey = "Ember";

    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(ProgressKey, 2m),
        new DynamicVar(EmberKey, 1m)
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MeiLinHoverTipFactory.Awakening, MeiLinHoverTipFactory.Ember];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var legacy = Owner.Creature.GetPower<XiangzuLegacyPower>();
        if (legacy != null)
            await legacy.AddQiCounterProgress(DynamicVars[ProgressKey].IntValue);

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        foreach (var enemy in CombatState.HittableEnemies)
            await PowerCmd.Apply<EmberPower>(enemy, DynamicVars[EmberKey].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[ProgressKey].UpgradeValueBy(2m);
    }
}



