using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Migration;
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
public class LongZhiQiShi() : MeiLinModCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private const string BurstEnergyKey = "BurstEnergy";

    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new EnergyVar(BurstEnergyKey, 2)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [EnergyHoverTip, MeiLinHoverTipFactory.Awakening, MeiLinHoverTipFactory.Ember];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayPowerCastAnim();
        await PowerCmd.Apply<LongZhiQiShiPower>(new BlockingPlayerChoiceContext(), Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        await PlayerCmd.GainEnergy(DynamicVars[BurstEnergyKey].BaseValue, Owner);
        await PowerCmd.Apply<LongZhiQiShiDrawPower>(new BlockingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
