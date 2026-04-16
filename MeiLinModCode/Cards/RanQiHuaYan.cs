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
public class RanQiHuaYan() : MeiLinModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private const string EmberKey = "Ember";
    private const string BonusPerQiKey = "BonusPerQi";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(EmberKey, 1m),
        new DynamicVar(BonusPerQiKey, 3m)
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
        foreach (var enemy in CombatState.HittableEnemies)
            await PowerCmd.Apply<EmberPower>(enemy, DynamicVars[EmberKey].BaseValue, Owner.Creature, this);

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        var qi = (int)(Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m);
        if (qi <= 0)
            return;

        await PowerCmd.Apply<QiPower>(Owner.Creature, -qi, Owner.Creature, this);
        var bonus = qi * DynamicVars[BonusPerQiKey].BaseValue;
        foreach (var enemy in CombatState.HittableEnemies)
            await PowerCmd.Apply<EmberPower>(enemy, bonus, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[EmberKey].UpgradeValueBy(1m);
        DynamicVars[BonusPerQiKey].UpgradeValueBy(1m);
    }
}
