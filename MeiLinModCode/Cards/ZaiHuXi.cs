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
using MegaCrit.Sts2.Core.Models.Powers;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class ZaiHuXi() : MeiLinModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string ProgressKey = "Progress";
    private const string VigorKey = "Vigor";
    private const string BurstDrawKey = "BurstDraw";
    private const string EmberKey = "Ember";

    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(ProgressKey, 2m),
        new DynamicVar(VigorKey, 3m),
        new DynamicVar(BurstDrawKey, 2m),
        new DynamicVar(EmberKey, 2m)
    ];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        MeiLinHoverTipFactory.Awakening,
        HoverTipFactory.FromPower<VigorPower>(),
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiGauge,
        MeiLinHoverTipFactory.QiConsume,
        MeiLinHoverTipFactory.Ember
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await QiCounterPower.AddProgress(Owner.Creature, DynamicVars[ProgressKey].IntValue, Owner.Creature, this);
        await PowerCmd.Apply<VigorPower>(new BlockingPlayerChoiceContext(), Owner.Creature, DynamicVars[VigorKey].BaseValue, Owner.Creature, this);

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        if (!await XiangzuCombatState.TryConsumeQi(Owner.Creature, 1, Owner.Creature, this))
            return;

        await CardPileCmd.Draw(choiceContext, DynamicVars[BurstDrawKey].BaseValue, Owner);

        var combatState = CombatState;
        if (combatState == null)
            return;

        foreach (var enemy in combatState.HittableEnemies)
            await PowerCmd.Apply<EmberPower>(new BlockingPlayerChoiceContext(), enemy, DynamicVars[EmberKey].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[ProgressKey].UpgradeValueBy(1m);
        DynamicVars[VigorKey].UpgradeValueBy(2m);
        DynamicVars[EmberKey].UpgradeValueBy(1m);
    }
}
