using System.Collections.Generic;
using System.Linq;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class LongZhiBeiFu() : MeiLinModCard(1, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner?.Creature == null)
            return;

        var otherAllies = CombatState.GetTeammatesOf(Owner.Creature)
            .Where(creature => creature is { IsAlive: true, IsPlayer: true, Player: not null } && creature.Player != Owner)
            .Select(creature => creature.Player!)
            .Distinct()
            .ToList();

        foreach (var ally in otherAllies)
        {
            var cardsToTake = GetStrikeAndDefendCards(ally).ToList();
            foreach (var card in cardsToTake)
                await TakeCardToDiscard(card);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
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
        RemoveLocalHandHolderIfPresent(card);
        await CardPileCmd.GiveToAnotherPlayer(
            card,
            Owner,
            PileType.Discard,
            CardPilePosition.Random,
            this);
    }

    private static void RemoveLocalHandHolderIfPresent(CardModel card)
    {
        if (card.Pile?.Type != PileType.Hand)
            return;

        var hand = NCombatRoom.Instance?.Ui.Hand;
        if (hand?.GetCardHolder(card) == null)
            return;

        hand.Remove(card);
        hand.ForceRefreshCardIndices();
    }
}
