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
using MegaCrit.Sts2.Core.Models.Powers;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(NoneCardPool))]
public class WeiHe() : MeiLinModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    private const string WeakKey = "Weak";
    private const string VulnerableKey = "Vulnerable";
    private const string EmberKey = "Ember";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(WeakKey, 2m),
        new DynamicVar(VulnerableKey, 1m),
        new DynamicVar(EmberKey, 1m)
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MeiLinHoverTipFactory.Awakening, MeiLinHoverTipFactory.Ember];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = CombatState;
        if (combatState == null)
            return;

        foreach (var enemy in combatState.HittableEnemies)
            await PowerCmd.Apply<WeakPower>(enemy, DynamicVars[WeakKey].BaseValue, Owner.Creature, this);

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        await PowerCmd.Apply<EmberPower>(Owner.Creature, DynamicVars[EmberKey].BaseValue, Owner.Creature, this);
        foreach (var enemy in combatState.HittableEnemies)
            await PowerCmd.Apply<VulnerablePower>(enemy, DynamicVars[VulnerableKey].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[VulnerableKey].UpgradeValueBy(1m);
    }
}

