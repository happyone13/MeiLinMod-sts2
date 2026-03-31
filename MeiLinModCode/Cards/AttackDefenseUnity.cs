using System.Linq;
using BaseLib.Extensions;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Services;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class AttackDefenseUnity() : MeiLinModCard(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override string PortraitPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();

    public override string CustomPortraitPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        MeiLinAudioService.SuppressNextDefaultCastSfx();
        MeiLinAudioService.TryPlayCustomCardClip("attack_defense_unity");

        var candidates = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(BasicStrikeDefendHelper.IsBasicStrikeOrDefend)
            .ToList()
            .StableShuffle(Owner.RunState.Rng.CombatCardSelection)
            .Take(2)
            .ToList();

        foreach (var card in candidates)
        {
            await CardPileCmd.Add(card, PileType.Hand);
            card.EnergyCost.SetThisTurn(0);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
