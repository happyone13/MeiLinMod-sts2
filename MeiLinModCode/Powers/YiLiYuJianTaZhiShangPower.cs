using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Powers;

public class YiLiYuJianTaZhiShangPower : MeiLinModPower
{
    private decimal _requiredQi = 1m;
    private bool _protectedThisTurn;
    private bool _consumePending;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m)
            return amount;

        if (_protectedThisTurn)
        {
            var maxLossProtected = decimal.Max(0m, target.CurrentHp - 1m);
            return decimal.Min(amount, maxLossProtected);
        }

        var maxLoss = decimal.Max(0m, target.CurrentHp - 1m);
        if (amount <= maxLoss)
            return amount;

        if (!XiangzuCombatState.HasQi(Owner, _requiredQi))
            return amount;

        _protectedThisTurn = true;
        _consumePending = true;
        return maxLoss;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || !_consumePending)
            return;

        _consumePending = false;
        await XiangzuCombatState.TryConsumeQi(Owner, _requiredQi, Owner, cardSource);
        _requiredQi += 1m;
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        _protectedThisTurn = false;
        return Task.CompletedTask;
    }
}
