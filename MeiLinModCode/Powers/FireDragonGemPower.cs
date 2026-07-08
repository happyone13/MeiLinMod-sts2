using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MeiLinMod.MeiLinModCode.HoverTips;

namespace MeiLinMod.MeiLinModCode.Powers;

public class FireDragonGemPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [MeiLinHoverTipFactory.Ember];

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var ownerCreature = cardPlay.Card.Owner?.Creature;
        if (ownerCreature != Owner)
            return;

        if (cardPlay.Card.Type != CardType.Attack)
            return;

        if (cardPlay.Target != null)
        {
            await PowerCmd.Apply<EmberPower>(new BlockingPlayerChoiceContext(), cardPlay.Target, Amount, Owner, cardPlay.Card);
            return;
        }

        var combatState = CombatState;
        if (combatState == null)
            return;

        foreach (var enemy in combatState.HittableEnemies)
            await PowerCmd.Apply<EmberPower>(new BlockingPlayerChoiceContext(), enemy, Amount, Owner, cardPlay.Card);
    }
}
