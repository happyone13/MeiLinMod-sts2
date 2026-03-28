using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class YangJingXuRui() : MeiLinModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string CardsKey = "Cards";
    private const string EnergyKey = "Energy";

    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new DynamicVar(EnergyKey, 1m)
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [EnergyHoverTip, MeiLinHoverTipFactory.Awakening, HoverTipFactory.FromPower<DrawCardsNextTurnPower>()];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DrawCardsNextTurnPower>(Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);
        var energy = DynamicVars[EnergyKey].BaseValue + (AwakeningHelper.IsAwakened(cardPlay) ? 1m : 0m);
        await PowerCmd.Apply<EnergyNextTurnPower>(Owner.Creature, energy, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}



