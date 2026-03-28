using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MeiLinMod.MeiLinModCode.HoverTips;

namespace MeiLinMod.MeiLinModCode.Powers;

public class FireDragonGemPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [MeiLinHoverTipFactory.Ember];

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return;

        if (!props.HasFlag(ValueProp.Move))
            return;

        // "Attack hit" only: card-based attacks count, skill/power damage does not.
        if (cardSource?.Type != CardType.Attack)
            return;

        // A blocked hit still counts as a hit.
        if (result.TotalDamage <= 0m && result.BlockedDamage <= 0m && result.UnblockedDamage <= 0m)
            return;

        await PowerCmd.Apply<EmberPower>(
            target,
            Amount,
            Owner,
            cardSource
        );
    }
}
