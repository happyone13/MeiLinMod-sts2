using BaseLib.Utils;
using System.Linq;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MeiLinMod.MeiLinModCode.Services;
using MeiLinMod.MeiLinModCode.StanceVfx;

namespace MeiLinMod.MeiLinModCode.Powers;

public enum XiangzuStance
{
    Guard,
    Attack
}

public class XiangzuLegacyPower : MeiLinModPower
{
    private int _stanceSwitchCount;
    private readonly MeiLinStanceVfxController _stanceVfx = new();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // Show hit/been-hit progress on the power counter.
    public override int DisplayAmount => (int)(Owner.GetPower<QiCounterPower>()?.Amount ?? 0m);
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiGauge
    ];
    
    public int Qi => GetQiAmount();
    public XiangzuStance Stance => GetCurrentStance();
    public int StanceSwitchCount => _stanceSwitchCount;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        _stanceSwitchCount = 0;
        await RefreshStanceVfx();
        await RefreshStanceDependentPowers(cardSource);
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await _stanceVfx.ClearAura();
        await PowerCmd.Remove<StanceGongPower>(oldOwner);
        await PowerCmd.Remove<StanceYuPower>(oldOwner);
    }

    public override async Task BeforeCombatStart()
    {
        await RefreshStanceVfx();
    }

    public override async Task AfterCombatEnd(MegaCrit.Sts2.Core.Rooms.CombatRoom room)
    {
        await _stanceVfx.ClearAura();
    }

    public void SetTriggerCount(int count)
    {
        // Deprecated: Qi gain threshold is now managed by QiCounterPower.
    }

    public async Task SetStance(XiangzuStance stance)
    {
        var hasAttack = Owner.HasPower<StanceGongPower>();
        var hasGuard = Owner.HasPower<StanceYuPower>();
        if ((stance == XiangzuStance.Attack && hasAttack && !hasGuard) ||
            (stance == XiangzuStance.Guard && hasGuard && !hasAttack))
            return;

        await PowerCmd.Remove<StanceGongPower>(Owner);
        await PowerCmd.Remove<StanceYuPower>(Owner);

        switch (stance)
        {
            case XiangzuStance.Guard:
                await PowerCmd.Apply<StanceYuPower>(Owner, 1m, Owner, null, silent: true);
                MeiLinAudioService.TryPlayGuardStanceSwitch(Owner.Player);
                break;
            default:
                await PowerCmd.Apply<StanceGongPower>(Owner, 1m, Owner, null, silent: true);
                MeiLinAudioService.TryPlayAttackStanceSwitch(Owner.Player);
                break;
        }

        _stanceSwitchCount++;
        await RefreshStanceVfx();
        await RefreshStanceDependentPowers(null);
        await TriggerStanceSwitchBonuses();
        if (!Owner.IsDead)
            await CreatureCmd.TriggerAnim(Owner, "Idle", 0f);
    }

    public async Task EnterGuardStance() => await SetStance(XiangzuStance.Guard);
    public async Task EnterAttackStance() => await SetStance(XiangzuStance.Attack);
    public async Task EnterOtherStance()
    {
        if (Owner.HasPower<StanceGongPower>())
            await EnterGuardStance();
        else
            await EnterAttackStance();
    }
    public async Task AddQiCounterProgress(int value) =>
        await XiangzuCombatState.AddQiProgress(Owner, Math.Max(0, value), Owner, null);

    public static bool IsInAttackStance(Creature creature)
    {
        return XiangzuCombatState.IsInAttackStance(creature);
    }

    public static bool IsInGuardStance(Creature creature)
    {
        return XiangzuCombatState.IsInGuardStance(creature);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await Task.CompletedTask;
    }

    public async Task RefreshFromStance()
    {
        await RefreshStanceVfx();
        await RefreshStanceDependentPowers(null);
    }

    public async Task TriggerVirtualStanceSwitch()
    {
        _stanceSwitchCount++;
        await TriggerStanceSwitchBonuses();
    }

    private XiangzuStance GetCurrentStance()
    {
        if (Owner.HasPower<StanceGongPower>())
            return XiangzuStance.Attack;

        if (Owner.HasPower<StanceYuPower>())
            return XiangzuStance.Guard;

        return XiangzuStance.Attack;
    }

    private int GetQiAmount() => XiangzuCombatState.GetQi(Owner);

    public override async Task AfterAttack(
        AttackCommand command)
    {
        if (command.Attacker != Owner || !command.DamageProps.HasFlag(ValueProp.Move))
            return;

        var hitCount = Math.Max(1, command.Results.Count());
        await XiangzuCombatState.AddQiProgress(Owner, hitCount, Owner, null);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || dealer == null || dealer == Owner || !props.HasFlag(ValueProp.Move))
            return;

        if (result.TotalDamage <= 0m && result.UnblockedDamage <= 0m && result.BlockedDamage <= 0m)
            return;

        await XiangzuCombatState.AddQiProgress(Owner, 1, Owner, cardSource);
    }

    private async Task TriggerStanceSwitchBonuses()
    {
        var qiProgress = (int)(Owner.GetPower<StanceSwitchQiProgressPower>()?.Amount ?? 0m);
        if (qiProgress > 0)
            await XiangzuCombatState.AddQiProgress(Owner, qiProgress, Owner, null);

        if (GetCurrentStance() == XiangzuStance.Attack)
        {
            var energy = (int)(Owner.GetPower<StanceSwitchEnergyPower>()?.Amount ?? 0m);
            if (energy > 0 && Owner.Player != null)
                await PlayerCmd.GainEnergy(energy, Owner.Player);
        }

        if (GetCurrentStance() == XiangzuStance.Guard)
        {
            var drawCount = (int)(Owner.GetPower<StanceSwitchDrawPower>()?.Amount ?? 0m);
            if (drawCount > 0 && Owner.Player != null)
                await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), drawCount, Owner.Player);
        }

        var ember = Owner.GetPower<StanceSwitchAllEnemiesEmberPower>()?.Amount ?? 0m;
        if (ember > 0)
        {
            foreach (var enemy in CombatState.HittableEnemies)
                await PowerCmd.Apply<EmberPower>(enemy, ember, Owner, null);
        }
    }

    private Task RefreshStanceVfx()
    {
        var auraPath = GetCurrentStance() switch
        {
            XiangzuStance.Guard => MeiLinStanceVfxController.GuardAuraScenePath,
            _ => MeiLinStanceVfxController.AttackAuraScenePath
        };

        return _stanceVfx.SetAura(Owner, auraPath);
    }

    private async Task RefreshStanceDependentPowers(CardModel? cardSource)
    {
        var dragonTail = Owner.GetPower<DragonTailStanceStatPower>();
        if (dragonTail != null)
            await dragonTail.RefreshFromState(cardSource);

        var qiPower = Owner.GetPower<QiPower>();
        if (qiPower != null)
            await qiPower.RefreshFromState(cardSource);
    }
}
