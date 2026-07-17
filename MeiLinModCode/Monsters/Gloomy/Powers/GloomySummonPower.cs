using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeiLinMod.MeiLinModCode.Monsters.Gloomy;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MeiLinMod.MeiLinModCode.Monsters.Gloomy.Powers;

/// <summary>
///     召唤术：阴郁首领持有的计数器，表示还可以召唤多少只阴郁野兽参战。
///     当己方阴郁野兽死亡时，会在敌方回合开始时自动消耗层数补充阴郁野兽，
///     直到场上阴郁野兽数量达到上限或层数耗尽；该召唤不占用首领当回合的意图。
/// </summary>
[RegisterPower]
public sealed class GloomySummonPower : ModPowerTemplate
{
    private const string PowerIconTexturePath = "res://MeiLinMod/images/powers/gloomy_power.png";

    // 场上阴郁野兽数量上限。
    private const int MaxGloomyBeasts = 3;

    public override PowerAssetProfile AssetProfile => new(PowerIconTexturePath, PowerIconTexturePath);
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    private class InternalData
    {
        public int PendingSummons;
    }

    protected override object InitInternalData() => new InternalData();

    /// <summary>
    ///     记录一次待补充的阴郁野兽（由 GloomyPrimeMonster 在 AfterDeath 中调用）。
    /// </summary>
    public void QueuePendingSummon()
    {
        InternalData data = GetInternalData<InternalData>();
        data.PendingSummons++;
        GD.Print($"[MeiLinMod.GloomySummonPower] QueuePendingSummon called. Pending={data.PendingSummons}, Amount={Amount}");
    }

    /// <summary>
    ///     批量增加待补充的阴郁野兽数量（用于把首领在获得召唤术前累计的死亡数转移到 Power 上）。
    /// </summary>
    public void AddPendingSummons(int count)
    {
        if (count > 0)
        {
            InternalData data = GetInternalData<InternalData>();
            data.PendingSummons += count;
            GD.Print($"[MeiLinMod.GloomySummonPower] AddPendingSummons({count}). Pending={data.PendingSummons}, Amount={Amount}");
        }
    }

    /// <summary>
    ///     获得召唤术时立即检测场上阴郁野兽数量；若不足上限，则立刻消耗层数补充。
    ///     这保证了首领在获得召唤术的瞬间就能把阵亡的野兽拉回场上。
    /// </summary>
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null! || Owner.IsDead)
            return;
        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
            return;
        if (Amount <= 0)
            return;

        int currentBeasts = combatState.Enemies.Count(c => c.IsAlive && c.Monster is GloomyBeastMonster);
        int deficit = MaxGloomyBeasts - currentBeasts;
        int toSummon = System.Math.Min(deficit, Amount);

        GD.Print($"[MeiLinMod.GloomySummonPower] AfterApplied: CurrentBeasts={currentBeasts}, Deficit={deficit}, Amount={Amount}, ToSummon={toSummon}");

        if (toSummon <= 0)
            return;

        await SummonBeasts(combatState, toSummon);
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -toSummon, Owner, null);
        GD.Print($"[MeiLinMod.GloomySummonPower] AfterApplied summon complete. Amount={Amount}");
    }

    /// <summary>
    ///     敌方回合开始时（意图结算之前），根据待补充数量和当前层数召唤阴郁野兽。
    ///     使用 BeforeSideTurnStart 确保召唤发生在敌方行动之前、不占用任何具体意图。
    /// </summary>
    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Enemy)
            return;
        if (Owner == null! || Owner.IsDead)
            return;
        if (!participants.Contains(Owner))
            return;

        InternalData data = GetInternalData<InternalData>();
        if (data.PendingSummons <= 0)
            return;
        if (Amount <= 0)
            return;

        int currentBeasts = combatState.Enemies.Count(c => c.IsAlive && c.Monster is GloomyBeastMonster);
        int capRemaining = MaxGloomyBeasts - currentBeasts;
        int toSummon = System.Math.Min(data.PendingSummons, System.Math.Min(Amount, capRemaining));

        GD.Print($"[MeiLinMod.GloomySummonPower] BeforeSideTurnStart: Pending={data.PendingSummons}, CurrentBeasts={currentBeasts}, CapRemaining={capRemaining}, Amount={Amount}, ToSummon={toSummon}");

        if (toSummon <= 0)
            return;

        await SummonBeasts(combatState, toSummon);

        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -toSummon, Owner, null);
        data.PendingSummons -= toSummon;
        GD.Print($"[MeiLinMod.GloomySummonPower] After summon: Pending={data.PendingSummons}, Amount={Amount}");
    }

    private async Task SummonBeasts(ICombatState combatState, int count)
    {
        if (count <= 0)
            return;

        Flash();

        for (int i = 0; i < count; i++)
        {
            string? slot = combatState.Encounter?.GetNextSlot(combatState);
            if (string.IsNullOrEmpty(slot))
            {
                GD.Print($"[MeiLinMod.GloomySummonPower] No more encounter slots available at summon index {i}.");
                break;
            }

            GD.Print($"[MeiLinMod.GloomySummonPower] Summoning GloomyBeastMonster into slot '{slot}'.");
            await CreatureCmd.Add<GloomyBeastMonster>(combatState, slot!);
        }
    }
}
