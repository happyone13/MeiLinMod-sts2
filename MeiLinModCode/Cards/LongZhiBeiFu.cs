using System.Collections.Generic;
using System.Linq;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class LongZhiBeiFu() : MeiLinModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cardsToTake = GetAllyStrikeAndDefendCards().ToList();
        foreach (var card in cardsToTake)
            await TakeCardToDiscard(card);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private IEnumerable<CardModel> GetAllyStrikeAndDefendCards()
    {
        var combatState = CombatState;
        if (combatState == null)
            return [];

        return combatState.GetTeammatesOf(Owner.Creature)
            .Where(creature => creature is { IsAlive: true, IsPlayer: true })
            .Select(creature => creature.Player)
            .OfType<Player>()
            .SelectMany(GetStrikeAndDefendCards)
            .Distinct();
    }

    private static IEnumerable<CardModel> GetStrikeAndDefendCards(Player player)
    {
        return new[] { PileType.Hand, PileType.Draw, PileType.Discard }
            .SelectMany(pile => pile.GetPile(player).Cards)
            .Where(BasicStrikeDefendHelper.IsStrikeOrDefendCard)
            .Distinct();
    }

    private async Task TakeCardToDiscard(CardModel card)
    {
        card.RemoveFromCurrentPile(false);
        card.GiveToAnotherPlayer(Owner);
        await CardPileCmd.Add(
            [card],
            PileType.Discard.GetPile(Owner),
            CardPilePosition.Random,
            this,
            skipVisuals: false,
            isChangingOwners: true);
    }
}
