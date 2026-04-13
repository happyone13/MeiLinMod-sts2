using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Powers;

public class YanLongChuDongPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

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

        if (!props.HasFlag(ValueProp.Move) || cardSource?.Type != CardType.Attack)
            return;

        if (result.TotalDamage <= 0m && result.BlockedDamage <= 0m && result.UnblockedDamage <= 0m)
            return;

        if (XiangzuLegacyPower.IsInAttackStance(Owner))
            await PowerCmd.Apply<EmberPower>(target, Amount, Owner, cardSource);

        if (!XiangzuLegacyPower.IsInGuardStance(Owner))
            return;

        var legacy = Owner.GetPower<XiangzuLegacyPower>();
        if (legacy != null)
            await legacy.AddQiCounterProgress((int)Amount);
    }
}
