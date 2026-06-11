using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class XiangzuSpiritCard() : MeiLinModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string PowerKey = "Power";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(PowerKey, 1m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];
    public override string? CustomSpinePortraitScenePath =>
        "res://MeiLinMod/scenes/cards/xiangzu_spirit_card_dynamic.tscn";
    public override SpinePortraitSlot CustomSpinePortraitSlot => SpinePortraitSlot.Ancient;
    public override bool UseCustomAncientFrame => true;
    public override bool UsesDynamicChaosFrame => true;
    public override string? CustomAncientFrameMaterialPath => ChaosAncientFrameMaterialPath;
    public override string? CustomAncientBannerMaterialPath => ChaosAncientBannerMaterialPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var power = await PowerCmd.Apply<XiangzuSpiritCardPower>(new BlockingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, this);
        if (power != null)
            power.DynamicVars.Strength.BaseValue = DynamicVars[PowerKey].BaseValue;
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}
