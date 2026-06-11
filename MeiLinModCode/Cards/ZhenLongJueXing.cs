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

[Pool(typeof(MeiLinModCardPool))]
public class ZhenLongJueXing() : MeiLinModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private const string DrawKey = "Draw";
    private const string EmberKey = "Ember";
    private const string ProgressKey = "Progress";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(DrawKey, 3m),
        new DynamicVar(EmberKey, 3m),
        new DynamicVar(ProgressKey, 3m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<EnergyNextTurnPower>(),
        HoverTipFactory.FromPower<DrawCardsNextTurnPower>(),
        MeiLinHoverTipFactory.Awakening,
        MeiLinHoverTipFactory.Ember
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<NextPowerCardCostDownPower>(new BlockingPlayerChoiceContext(), Owner.Creature, 99m, Owner.Creature, this);
        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        var awakenedValue = IsUpgraded ? 4m : 3m;
        await PlayerCmd.LoseEnergy(3, Owner);
        await PowerCmd.Apply<EnergyNextTurnPower>(new BlockingPlayerChoiceContext(), Owner.Creature, awakenedValue, Owner.Creature, this);
        await PowerCmd.Apply<DrawCardsNextTurnPower>(new BlockingPlayerChoiceContext(), Owner.Creature, awakenedValue, Owner.Creature, this);
        await PowerCmd.Apply<ZhenLongAwakeningEnemyEmberNextTurnPower>(new BlockingPlayerChoiceContext(), Owner.Creature, awakenedValue, Owner.Creature, this);
        await PowerCmd.Apply<ZhenLongAwakeningQiProgressNextTurnPower>(new BlockingPlayerChoiceContext(), Owner.Creature, awakenedValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[DrawKey].UpgradeValueBy(1m);
        DynamicVars[EmberKey].UpgradeValueBy(1m);
        DynamicVars[ProgressKey].UpgradeValueBy(1m);
    }
}
