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
public class BaoQi() : MeiLinModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private const string EmberKey = "Ember";
    private const string ProgressKey = "Progress";

    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded
        ? [CardKeyword.Innate, CardKeyword.Exhaust]
        : [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MeiLinHoverTipFactory.Awakening, MeiLinHoverTipFactory.Ember, MeiLinHoverTipFactory.Qi];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(EmberKey, 2m),
        new DynamicVar(ProgressKey, 5m)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await QiCounterPower.AddProgress(Owner.Creature, DynamicVars[ProgressKey].IntValue, Owner.Creature, this);

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        await PowerCmd.Apply<EmberPower>(Owner.Creature, DynamicVars[EmberKey].BaseValue, Owner.Creature, this);
        await QiCounterPower.AddProgress(Owner.Creature, DynamicVars[ProgressKey].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
