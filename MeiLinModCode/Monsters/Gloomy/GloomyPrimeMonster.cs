using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeiLinMod.MeiLinModCode.Monsters.Gloomy.Powers;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;
using STS2RitsuLib.Scaffolding.MonsterMoves;

namespace MeiLinMod.MeiLinModCode.Monsters.Gloomy;

/// <summary>
///     1006012 阴郁首领（gloomy_prime）。
///     时间轴：忧郁咒术（玩家易伤/虚弱 + 阴郁野兽野性）→ 猛击（4×3）→ 唤醒野性（阴郁野兽野性 + 召唤术）。
///     召唤术会在阴郁野兽死亡后的敌方回合自动触发补充，不占用首领当回合意图。
///     首领本身不携带 MinionPower；阴郁野兽均会被标记为 Minion，因此首领自动成为主目标（Primary Enemy）。
/// </summary>
[RegisterMonster]
public sealed class GloomyPrimeMonster : ModMonsterTemplate
{
    private const string VisualsScenePath = "res://MeiLinMod/scenes/monsters/gloomy/creature_visuals/1006012.tscn";

    public override int MinInitialHp => 85;
    public override int MaxInitialHp => 125;

    public override MonsterAssetProfile AssetProfile => new(VisualsScenePath);

    /// <summary>
    ///     在首领获得 GloomySummonPower 之前死亡的阴郁野兽数量。
    ///     用于保证首次获得召唤术前死亡的野兽也能在后续被补充。
    /// </summary>
    private int _pendingSummonCount;

    /// <summary>
    ///     让 RitsuLib 把普通 Godot 场景转换为游戏需要的 <see cref="NCreatureVisuals" />。
    /// </summary>
    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(VisualsScenePath);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState melancholyCurse = new("MELANCHOLY_CURSE", MelancholyCurseMove, new DebuffIntent(), new BuffIntent());
        MoveState heavyStrike = new("HEAVY_STRIKE", HeavyStrikeMove, new MultiAttackIntent(4, 3));
        MoveState awakenWild = new("AWAKEN_WILD", AwakenWildMove, new BuffIntent());

        // 循环：忧郁咒术 → 猛击 → 唤醒野性。
        // MonsterMoveStateMachine 的初始状态即为首回合执行的 MoveState。
        awakenWild.FollowUpState = melancholyCurse;
        melancholyCurse.FollowUpState = heavyStrike;
        heavyStrike.FollowUpState = awakenWild;

        List<MonsterState> states = [melancholyCurse, heavyStrike, awakenWild];
        return new MonsterMoveStateMachine(states, melancholyCurse);
    }

    /// <summary>
    ///     忧郁咒术：赋予所有玩家 2 层易伤/虚弱，赋予所有阴郁野兽 2 层野性。
    /// </summary>
    private async Task MelancholyCurseMove(IReadOnlyList<Creature> targets)
    {
        GloomyMonsterVfx.PlaySfx(GloomyMonsterVfx.PrimeBuffSfx);
        GloomyMonsterVfx.PlaySelf(Creature, GloomyMonsterVfx.PrimeBuff, 1.4f);
        await PlayAnimAsync("Buff", 1.67f);

        foreach (Creature player in CombatState.PlayerCreatures)
        {
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), player, 2m, Creature, null);
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), player, 2m, Creature, null);
        }

        foreach (Creature beast in GetGloomyBeasts())
        {
            await PowerCmd.Apply<GloomyWildPower>(new ThrowingPlayerChoiceContext(), beast, 2m, Creature, null);
        }
    }

    /// <summary>
    ///     猛击：造成 4 点伤害，连续 3 次。
    ///     动画拆分为准备（normal_attack_ready）+ 随机一段攻击（play1/play2 + end）。
    /// </summary>
    private async Task HeavyStrikeMove(IReadOnlyList<Creature> targets)
    {
        // 先播放准备动画。
        await CreatureCmd.TriggerAnim(Creature, "AttackReady", 0.47f);

        // 随机选择 play1 或 play2。
        bool usePlay1 = Rng.NextBool();
        string attackTrigger = usePlay1 ? "AttackPlay1" : "AttackPlay2";
        string selfVfx = usePlay1 ? GloomyMonsterVfx.PrimeAttackPlay1 : GloomyMonsterVfx.PrimeAttackPlay2;
        string attackSfx = usePlay1 ? GloomyMonsterVfx.PrimeAttackPlay1Sfx : GloomyMonsterVfx.PrimeAttackPlay2Sfx;
        float hitRotation = usePlay1 ? 240f : 35f;

        GloomyMonsterVfx.PlaySfx(attackSfx);
        GloomyMonsterVfx.PlaySelf(Creature, selfVfx, 1.4f);
        GloomyMonsterVfx.PlayTarget(targets.FirstOrDefault(), GloomyMonsterVfx.HitSlash, 0.7f, hitRotation);
        _ = GloomyMonsterVfx.ShakeAfter(0f);

        await DamageCmd.Attack(4).WithHitCount(3).FromMonster(this)
            .WithAttackerAnim(attackTrigger, 0.67f)
            .WithAttackerFx(null, null)
            .WithHitFx(null, null)
            .OnlyPlayAnimOnce()
            .Execute(null);
    }

    /// <summary>
    ///     唤醒野性：赋予所有阴郁野兽 3 层野性，自身获得 2 层召唤术。
    ///     召唤术会在后续阴郁野兽死亡后的敌方回合自动触发补充。
    ///     若此前已有阴郁野兽在首领未获得召唤术时死亡，会把累计的待补充数量一并转移给召唤术。
    /// </summary>
    private async Task AwakenWildMove(IReadOnlyList<Creature> targets)
    {
        GloomyMonsterVfx.PlaySfx(GloomyMonsterVfx.PrimeUniqueSfx);
        GloomyMonsterVfx.PlaySelf(Creature, GloomyMonsterVfx.PrimeUnique, 1.4f);
        foreach (Creature player in CombatState.PlayerCreatures)
            GloomyMonsterVfx.PlayTarget(player, GloomyMonsterVfx.PrimeUniqueTarget);
        _ = GloomyMonsterVfx.ShakeAfter(0.1f);
        _ = GloomyMonsterVfx.ShakeAfter(0.3f);
        _ = GloomyMonsterVfx.ShakeAfter(0.5f);
        _ = GloomyMonsterVfx.ShakeAfter(0.7f);

        await PlayAnimAsync("Unique", 1.53f);

        foreach (Creature beast in GetGloomyBeasts())
        {
            await PowerCmd.Apply<GloomyWildPower>(new ThrowingPlayerChoiceContext(), beast, 3m, Creature, null);
        }

        // 赋予首领自身召唤术 2 层；Power 的 AfterApplied 会立即检测场上阴郁野兽数量并补充 deficit。
        await PowerCmd.Apply<GloomySummonPower>(new ThrowingPlayerChoiceContext(), Creature, 2m, Creature, null);

        // AfterApplied 已经根据当前场上缺口立即召唤，预死亡计数已处理完毕，直接清零。
        _pendingSummonCount = 0;
    }

    /// <summary>
    ///     死亡检测：当己方阴郁野兽死亡时，通知首领身上的 GloomySummonPower 记录待补充数量。
    ///     若首领尚未获得召唤术，则先在本地累计，等到唤醒野性获得召唤术时一次性转移。
    /// </summary>
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        GD.Print($"[MeiLinMod.GloomyPrimeMonster] AfterDeath: deadLogName={creature.LogName}, combatId={creature.CombatId}, side={creature.Side}, monsterType={creature.Monster?.GetType().Name}, wasRemovalPrevented={wasRemovalPrevented}");

        if (wasRemovalPrevented)
            return Task.CompletedTask;
        if (creature == Creature)
            return Task.CompletedTask;
        if (creature.Side != Creature.Side)
            return Task.CompletedTask;
        if (creature.Monster is not GloomyBeastMonster)
            return Task.CompletedTask;

        GloomySummonPower? power = Creature.GetPower<GloomySummonPower>();
        if (power != null)
        {
            GD.Print($"[MeiLinMod.GloomyPrimeMonster] Power exists, queueing pending summon.");
            power.QueuePendingSummon();
        }
        else
        {
            _pendingSummonCount++;
            GD.Print($"[MeiLinMod.GloomyPrimeMonster] Power not yet acquired, local pending={_pendingSummonCount}.");
        }

        return Task.CompletedTask;
    }

    private IReadOnlyList<Creature> GetGloomyBeasts()
    {
        return CombatState.Enemies.Where(c => c.Monster is GloomyBeastMonster).ToList();
    }

    private async Task PlayAnimAsync(string trigger, float waitSeconds)
    {
        await CreatureCmd.TriggerAnim(Creature, trigger, waitSeconds);
    }

    /// <summary>
    ///     自定义 Spine 动画状态机。
    ///     待机 normal_idle；受击 normal_hit；死亡 normal_groggy。
    ///     Buff：normal_buff_1；大招：normal_unique_1。
    ///     攻击：准备 normal_attack_ready，随机二选一段 normal_attack_play1 / normal_attack_play2，收招 normal_attack_end。
    /// </summary>
    protected override CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
    {
        AnimState idle = new("normal_idle", isLooping: true);
        AnimState hit = new("normal_hit") { NextState = idle };
        AnimState dead = new("normal_groggy", isLooping: true);

        AnimState buff = new("normal_buff_1") { NextState = idle };
        AnimState unique = new("normal_unique_1") { NextState = idle };

        // 攻击准备，只播放一次，结束后回到 idle；随后由移动逻辑触发 AttackPlay1/2。
        AnimState attackReady = new("normal_attack_ready") { NextState = idle };

        AnimState attackEnd1 = new("normal_attack_end") { NextState = idle };
        AnimState attackPlay1 = new("normal_attack_play1") { NextState = attackEnd1 };

        AnimState attackEnd2 = new("normal_attack_end") { NextState = idle };
        AnimState attackPlay2 = new("normal_attack_play2") { NextState = attackEnd2 };

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Buff", buff);
        animator.AddAnyState("Unique", unique);
        animator.AddAnyState("AttackReady", attackReady);
        animator.AddAnyState("AttackPlay1", attackPlay1);
        animator.AddAnyState("AttackPlay2", attackPlay2);
        animator.AddAnyState("Attack", attackPlay1);
        return animator;
    }
}
