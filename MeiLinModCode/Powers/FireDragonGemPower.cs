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

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [MeiLinHoverTipFactory.Ember];

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;

        if (cardPlay.Card.Type != CardType.Attack)
            return;

        if (cardPlay.Target != null)
        {
            await PowerCmd.Apply<EmberPower>(cardPlay.Target, Amount, Owner, cardPlay.Card);
            return;
        }

        foreach (var enemy in CombatState.HittableEnemies)
            await PowerCmd.Apply<EmberPower>(enemy, Amount, Owner, cardPlay.Card);
    }
}
