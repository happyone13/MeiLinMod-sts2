using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Powers;

public enum XiangzuStance
{
    Neutral,
    Guard,
    Attack
}

public class XiangzuLegacyPower : MeiLinModPower
{
    private int _progress;
    private int _triggerCount = 5;
    private int _appliedStrength;
    private int _appliedDexterity;
    private int _stanceSwitchCount;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // Show hit/been-hit progress on the power counter.
    public override int DisplayAmount => _progress;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.AttackStance,
        MeiLinHoverTipFactory.GuardStance
    ];
    
    public int Qi => GetQiAmount();
    public int TriggerCount => _triggerCount;
    public XiangzuStance Stance => GetCurrentStance();
    public int StanceSwitchCount => _stanceSwitchCount;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _progress = 0;
        _appliedStrength = 0;
        _appliedDexterity = 0;
        _stanceSwitchCount = 0;
        await EnsureDefaultStance();
        InvokeDisplayAmountChanged();
        await RecalculateStats();
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_appliedStrength != 0)
        {
            await PowerCmd.Apply<StrengthPower>(oldOwner, -_appliedStrength, oldOwner, null, silent: true);
            _appliedStrength = 0;
        }

        if (_appliedDexterity != 0)
        {
            await PowerCmd.Apply<DexterityPower>(oldOwner, -_appliedDexterity, oldOwner, null, silent: true);
            _appliedDexterity = 0;
        }

        await PowerCmd.Remove<StanceHengPower>(oldOwner);
        await PowerCmd.Remove<StanceGongPower>(oldOwner);
        await PowerCmd.Remove<StanceYuPower>(oldOwner);
        await PowerCmd.Remove<QiPower>(oldOwner);
    }

    // External interface for cards/powers to change the "5 hits" threshold.
    public void SetTriggerCount(int count)
    {
        _triggerCount = Math.Max(1, count);
    }

    public async Task SetStance(XiangzuStance stance)
    {
        if (GetCurrentStance() == stance)
            return;

        await PowerCmd.Remove<StanceHengPower>(Owner);
        await PowerCmd.Remove<StanceGongPower>(Owner);
        await PowerCmd.Remove<StanceYuPower>(Owner);

        switch (stance)
        {
            case XiangzuStance.Guard:
                await PowerCmd.Apply<StanceYuPower>(Owner, 1m, Owner, null, silent: true);
                break;
            case XiangzuStance.Attack:
                await PowerCmd.Apply<StanceGongPower>(Owner, 1m, Owner, null, silent: true);
                break;
            default:
                await PowerCmd.Apply<StanceHengPower>(Owner, 1m, Owner, null, silent: true);
                break;
        }

        _stanceSwitchCount++;
        await TriggerStanceSwitchBonuses();
        await RecalculateStats();
    }

    public async Task EnterNeutralStance() => await SetStance(XiangzuStance.Neutral);
    public async Task EnterGuardStance() => await SetStance(XiangzuStance.Guard);
    public async Task EnterAttackStance() => await SetStance(XiangzuStance.Attack);
    public async Task AddQiCounterProgress(int value) => await AddProgress(Math.Max(0, value));

    public override async Task AfterAttack(AttackCommand command)
    {
        if (command.Attacker != Owner)
            return;

        if (!command.DamageProps.HasFlag(ValueProp.Move))
            return;

        // Attack event happened, count once.
        await AddProgress(1);
    }

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

        if (dealer == null || dealer == Owner)
            return;

        // Being attacked event happened, count once.
        if (result.TotalDamage > 0 || result.UnblockedDamage > 0 || result.BlockedDamage > 0)
        {
            await AddProgress(1);
        }
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power.Owner != Owner || power is not QiPower)
            return;

        await RecalculateStats();
    }

    private async Task AddProgress(int value)
    {
        if (Owner.HasPower<QiProgressDoubleThisTurnPower>())
            value *= 2;

        _progress += value;

        var gainedQi = 0;
        while (_progress >= _triggerCount)
        {
            _progress -= _triggerCount;
            gainedQi++;
        }

        if (gainedQi <= 0)
        {
            InvokeDisplayAmountChanged();
            return;
        }

        await PowerCmd.Apply<QiPower>(Owner, gainedQi, Owner, null, silent: true);
        Flash();
        InvokeDisplayAmountChanged();
        await RecalculateStats();
    }

    private async Task RecalculateStats()
    {
        var targetStrength = 0;
        var targetDexterity = 0;
        var qi = GetQiAmount();

        switch (GetCurrentStance())
        {
            case XiangzuStance.Neutral:
                targetStrength = qi;
                targetDexterity = qi;
                break;
            case XiangzuStance.Guard:
                targetStrength = 0;
                targetDexterity = qi * 2;
                break;
            case XiangzuStance.Attack:
                targetStrength = qi * 2;
                targetDexterity = 0;
                break;
        }

        var deltaStrength = targetStrength - _appliedStrength;
        var deltaDexterity = targetDexterity - _appliedDexterity;

        if (deltaStrength != 0)
        {
            await PowerCmd.Apply<StrengthPower>(Owner, deltaStrength, Owner, null, silent: true);
            _appliedStrength = targetStrength;
        }

        if (deltaDexterity != 0)
        {
            await PowerCmd.Apply<DexterityPower>(Owner, deltaDexterity, Owner, null, silent: true);
            _appliedDexterity = targetDexterity;
        }
    }

    public async Task RefreshFromStance()
    {
        await EnsureDefaultStance();
        await RecalculateStats();
    }

    private XiangzuStance GetCurrentStance()
    {
        if (Owner.HasPower<StanceGongPower>())
            return XiangzuStance.Attack;

        if (Owner.HasPower<StanceYuPower>())
            return XiangzuStance.Guard;

        return XiangzuStance.Neutral;
    }

    private async Task EnsureDefaultStance()
    {
        if (!Owner.HasPower<StanceHengPower>() &&
            !Owner.HasPower<StanceGongPower>() &&
            !Owner.HasPower<StanceYuPower>())
        {
            await PowerCmd.Apply<StanceHengPower>(Owner, 1m, Owner, null, silent: true);
        }
    }

    private int GetQiAmount()
    {
        return (int)(Owner.GetPower<QiPower>()?.Amount ?? 0m);
    }

    private async Task TriggerStanceSwitchBonuses()
    {
        var qiProgress = (int)(Owner.GetPower<StanceSwitchQiProgressPower>()?.Amount ?? 0m);
        if (qiProgress > 0)
            await AddProgress(qiProgress);

        var energy = (int)(Owner.GetPower<StanceSwitchEnergyPower>()?.Amount ?? 0m);
        if (energy > 0 && Owner.Player != null)
            await PlayerCmd.GainEnergy(energy, Owner.Player);

        var ember = Owner.GetPower<StanceSwitchAllEnemiesEmberPower>()?.Amount ?? 0m;
        if (ember > 0)
        {
            foreach (var enemy in CombatState.HittableEnemies)
                await PowerCmd.Apply<EmberPower>(enemy, ember, Owner, null);
        }
    }
}
