using System.Collections.Generic;
using BaseLib.Utils;
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
public class QiPo() : MeiLinModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private const string EmberKey = "Ember";
    private const string ProgressKey = "Progress";
    private const string StrengthLossKey = "StrengthLoss";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(ProgressKey, 2m),
        new DynamicVar(StrengthLossKey, 6m),
        new DynamicVar(EmberKey, 2m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        MeiLinHoverTipFactory.Awakening,
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiConsume,
        MeiLinHoverTipFactory.Ember
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await QiCounterPower.AddProgress(Owner.Creature, DynamicVars[ProgressKey].IntValue, Owner.Creature, this);

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        if ((Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m) < 1m)
            return;

        await PowerCmd.Apply<QiPower>(Owner.Creature, -1m, Owner.Creature, this);
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<QiPoTemporaryStrengthDownPower>(enemy, DynamicVars[StrengthLossKey].BaseValue, Owner.Creature, this);
            await PowerCmd.Apply<EmberPower>(enemy, DynamicVars[EmberKey].BaseValue, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars[ProgressKey].UpgradeValueBy(1m);
        DynamicVars[StrengthLossKey].UpgradeValueBy(2m);
        DynamicVars[EmberKey].UpgradeValueBy(1m);
    }
}
