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
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class JinZhongZhao() : MeiLinModCard(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(12m, ValueProp.Move), new CardsVar(2)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        MeiLinHoverTipFactory.Awakening,
        HoverTipFactory.FromPower<DrawCardsNextTurnPower>(),
        HoverTipFactory.FromPower<EnergyNextTurnPower>()
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<DrawCardsNextTurnPower>(Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);

        if (AwakeningHelper.IsAwakened(cardPlay))
            await PowerCmd.Apply<EnergyNextTurnPower>(Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}



