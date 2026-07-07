using System.Linq;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Services;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class AttackDefenseUnity() : MeiLinModCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override string? CustomSpinePortraitScenePath =>
        "res://MeiLinMod/scenes/cards/attack_defense_unity_dynamic.tscn";
    public override SpinePortraitSlot CustomSpinePortraitSlot => SpinePortraitSlot.Ancient;
    public override bool UseCustomAncientFrame => true;
    public override bool UsesDynamicChaosFrame => true;
    public override string? CustomAncientBorderMaterialPath => ChaosAncientFrameMaterialPath;
    public override string? CustomAncientBannerMaterialPath => ChaosAncientBannerMaterialPath;
    protected override string? CombatTimelineName => "u3_buff";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        MeiLinAudioService.SuppressNextDefaultCastSfx(Owner);
        MeiLinAudioService.TryPlayCustomCardClip("attack_defense_unity", Owner);

        var candidates = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(IsStrikeOrBasicDefend)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.CombatCardSelection)
            .Take(2)
            .ToList();

        foreach (var card in candidates)
        {
            await CardPileCmd.Add(card, PileType.Hand);
            card.EnergyCost.SetUntilPlayed(0);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private static bool IsStrikeOrBasicDefend(CardModel card)
    {
        return BasicStrikeDefendHelper.IsStrikeCard(card) ||
               BasicStrikeDefendHelper.IsBasicDefend(card);
    }
}
