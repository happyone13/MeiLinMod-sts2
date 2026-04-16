using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Reflection;

namespace MeiLinMod.MeiLinModCode.Powers;

public class EmberPower : MeiLinModPower
{
    private const int TriggerStack = 5;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner)
            return 0m;

        if (!props.HasFlag(ValueProp.Move))
            return 0m;

        // Ember: +1 damage taken per stack.
        return Amount;
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || amount <= 0m)
            return;

        var previousAmount = Amount - amount;
        var prevTier = (int)decimal.Floor(previousAmount / TriggerStack);
        var newTier = (int)decimal.Floor(Amount / TriggerStack);

        if (newTier <= prevTier)
            return;

        var triggerCount = newTier - prevTier;
        if (triggerCount <= 0)
            return;

        var maxHealth = TryGetMaxHealth(Owner);
        if (maxHealth <= 0m)
            return;

        var perTriggerDamage = Math.Max(1m, Math.Ceiling(maxHealth * 0.05m));
        var burstDamage = perTriggerDamage * triggerCount;

        if (burstDamage <= 0m)
            return;

        Flash();
        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            Owner,
            burstDamage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            Applier,
            null);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        var shouldDecayThisTurnEnd = Owner.IsPlayer
            ? side != Owner.Side
            : side == Owner.Side;
        if (!shouldDecayThisTurnEnd)
            return;

        if (!Owner.IsPlayer && Owner.HasPower<EnemyEmberHalfDecayPower>())
            return;

        if (Owner.HasPower<EmberNoExpireThisTurnPower>())
        {
            await PowerCmd.Remove<EmberNoExpireThisTurnPower>(Owner);
            return;
        }

        var retain = (int)decimal.Floor(Amount / 2m);
        var remove = (int)Amount - retain;

        if (remove > 0)
            await PowerCmd.Apply<EmberPower>(Owner, -remove, Applier ?? Owner, null, silent: true);

        if (retain <= 0)
            await PowerCmd.Remove(this);
    }

    private static decimal TryGetMaxHealth(Creature creature)
    {
        var type = creature.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        string[] candidateNames = ["MaxHealth", "MaxHp", "MaxHP", "MaxHitPoints", "HealthMax"];

        foreach (var name in candidateNames)
        {
            var prop = type.GetProperty(name, flags);
            if (prop?.GetValue(creature) is { } value && TryConvertToDecimal(value, out var result))
                return result;

            var field = type.GetField(name, flags);
            if (field?.GetValue(creature) is { } fieldValue && TryConvertToDecimal(fieldValue, out result))
                return result;
        }

        return 0m;
    }

    private static bool TryConvertToDecimal(object value, out decimal result)
    {
        switch (value)
        {
            case decimal d:
                result = d;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case float f:
                result = (decimal)f;
                return true;
            case double db:
                result = (decimal)db;
                return true;
            default:
                result = 0m;
                return false;
        }
    }
}
