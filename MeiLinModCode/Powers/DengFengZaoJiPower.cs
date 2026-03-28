using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class DengFengZaoJiPower : MeiLinModPower
{
    private decimal _strikeBonus;
    private decimal _defendBonus;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner)
            return Task.CompletedTask;

        if (cardPlay.Card.Tags.Contains(CardTag.Strike))
        {
            _strikeBonus += Amount;
            BuffAllStrikeCards(Amount);
        }
        else if (cardPlay.Card.Tags.Contains(CardTag.Defend))
        {
            _defendBonus += Amount;
            BuffAllDefendCards(Amount);
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner.Creature != Owner)
            return Task.CompletedTask;

        if (_strikeBonus > 0m && card.Tags.Contains(CardTag.Strike))
        {
            ApplyStrikeBonus(card, _strikeBonus);
        }

        if (_defendBonus > 0m && card.Tags.Contains(CardTag.Defend))
        {
            ApplyDefendBonus(card, _defendBonus);
        }

        return Task.CompletedTask;
    }

    private void BuffAllStrikeCards(decimal bonus)
    {
        var player = Owner.Player;
        if (player?.PlayerCombatState == null)
            return;

        foreach (var card in player.PlayerCombatState.AllCards.Where(c => c.Tags.Contains(CardTag.Strike)))
        {
            ApplyStrikeBonus(card, bonus);
        }
    }

    private void BuffAllDefendCards(decimal bonus)
    {
        var player = Owner.Player;
        if (player?.PlayerCombatState == null)
            return;

        foreach (var card in player.PlayerCombatState.AllCards.Where(c => c.Tags.Contains(CardTag.Defend)))
        {
            ApplyDefendBonus(card, bonus);
        }
    }

    private static void ApplyStrikeBonus(CardModel card, decimal bonus)
    {
        if (!card.DynamicVars.ContainsKey("Damage"))
            return;

        card.DynamicVars.Damage.BaseValue += bonus;
    }

    private static void ApplyDefendBonus(CardModel card, decimal bonus)
    {
        if (!card.DynamicVars.ContainsKey("Block"))
            return;

        card.DynamicVars.Block.BaseValue += bonus;
    }
}
