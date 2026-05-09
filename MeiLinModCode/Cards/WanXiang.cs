using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class WanXiang() : MeiLinModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => IsUpgraded
        ? [CardKeyword.Innate, CardKeyword.Exhaust]
        : [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new EnergyVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);

        for (var i = 0; i < 13; i++)
        {
            if (CombatManager.Instance.IsOverOrEnding)
                break;

            var card = PileType.Hand.GetPile(Owner).Cards.FirstOrDefault(c => c.CanPlay());
            if (card == null)
                break;

            var target = ResolveTarget(card);
            await card.SpendResources();
            await CardCmd.AutoPlay(choiceContext, card, target, AutoPlayType.Default, skipXCapture: true);
        }
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner != Owner)
            return true;

        if (Pile?.Type != PileType.Hand)
            return true;

        if (card is WanXiang)
            return true;

        if (autoPlayType != AutoPlayType.None)
            return true;

        return false;
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }

    private Creature? ResolveTarget(CardModel card)
    {
        var combatState = CombatState;
        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState?.HittableEnemies.FirstOrDefault(),
            TargetType.AnyPlayer => Owner.Creature,
            _ => null
        };
    }
}
