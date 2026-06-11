using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Powers;

public class GenXingPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner)
            return;

        if (!props.HasFlag(ValueProp.Move))
            return;

        if (dealer == null || !dealer.IsMonster)
            return;

        if (result.TotalDamage <= 0m && result.BlockedDamage <= 0m && result.UnblockedDamage <= 0m)
            return;

        await PowerCmd.Apply<EmberPower>(new BlockingPlayerChoiceContext(), dealer, 1m, Owner, cardSource);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Combat.CombatSide side, System.Collections.Generic.IEnumerable<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants)
    {
        // Keep this effect active through the opponent's turn, then clear it.
        if (side == Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }
}
