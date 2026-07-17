using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MeiLinMod.MeiLinModCode.Monsters.Gloomy.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
///     1006017 阴郁野兽（gloomy_beast）。
///     时间轴：咆哮（格挡）→ 下劈→ 野兽一击 循环。
/// </summary>
[RegisterMonster]
public sealed class GloomyBeastMonster : ModMonsterTemplate
{
    private const string VisualsScenePath = "res://MeiLinMod/scenes/monsters/gloomy/creature_visuals/1006017.tscn";

    public override int MinInitialHp => 25;
    public override int MaxInitialHp => 30;

    public override MonsterAssetProfile AssetProfile => new(VisualsScenePath);

    /// <summary>
    ///     让 RitsuLib 把普通 Godot 场景转换为游戏需要的 <see cref="NCreatureVisuals" />。
    /// </summary>
    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(VisualsScenePath);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<MinionPower>(new ThrowingPlayerChoiceContext(), Creature, 1m, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState roar = new("ROAR", RoarMove, new DefendIntent());
        MoveState slash = new("SLASH", SlashMove, new SingleAttackIntent(3));
        MoveState beastStrike = new("BEAST_STRIKE", BeastStrikeMove, new SingleAttackIntent(6));

        // 循环：咆哮 → 下劈 → 野兽一击。
        // MonsterMoveStateMachine 的初始状态即为首回合执行的 MoveState。
        beastStrike.FollowUpState = roar;
        roar.FollowUpState = slash;
        slash.FollowUpState = beastStrike;

        List<MonsterState> states = [roar, slash, beastStrike];
        return new MonsterMoveStateMachine(states, roar);
    }

    private async Task RoarMove(IReadOnlyList<Creature> targets)
    {
        // 咆哮：播放 normal_buff_1 动画，然后获得 10 点格挡。
        GloomyMonsterVfx.PlaySelf(Creature, GloomyMonsterVfx.BeastBuff);
        GloomyMonsterVfx.PlaySfx(GloomyMonsterVfx.BeastBuffSfx);
        await PlayAnimAsync("Buff", 1.53f);
        await CreatureCmd.GainBlock(Creature, 10m, ValueProp.Move, null);
    }

    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        GloomyMonsterVfx.PlaySfx(GloomyMonsterVfx.BeastAttack01Sfx);
        GloomyMonsterVfx.PlaySelf(Creature, GloomyMonsterVfx.BeastAttack01);
        GloomyMonsterVfx.PlayTarget(targets.FirstOrDefault(), GloomyMonsterVfx.HitSlash, 0.6f, -100f);
        _ = GloomyMonsterVfx.ShakeAfter(0.532f);

        await DamageCmd.Attack(3).FromMonster(this)
            .WithAttackerAnim("Attack01", 1.2f)
            .WithAttackerFx(null, null)
            .WithHitFx(null, null)
            .Execute(null);
    }

    private async Task BeastStrikeMove(IReadOnlyList<Creature> targets)
    {
        GloomyMonsterVfx.PlaySfx(GloomyMonsterVfx.BeastAttack02Sfx);
        GloomyMonsterVfx.PlaySelf(Creature, GloomyMonsterVfx.BeastAttack02);
        GloomyMonsterVfx.PlayTarget(targets.FirstOrDefault(), GloomyMonsterVfx.HitBlunt, 0.6f);
        _ = GloomyMonsterVfx.ShakeAfter(0.532f);

        await DamageCmd.Attack(6).FromMonster(this)
            .WithAttackerAnim("Attack02", 1.47f)
            .WithAttackerFx(null, null)
            .WithHitFx(null, null)
            .Execute(null);
    }

    /// <summary>
    ///     播放指定触发器动画并等待固定时长。
    ///     等待时间取自解包时间轴中的动画时长（单位：秒）。
    /// </summary>
    private async Task PlayAnimAsync(string trigger, float waitSeconds)
    {
        await CreatureCmd.TriggerAnim(Creature, trigger, waitSeconds);
    }

    /// <summary>
    ///     自定义 Spine 动画状态机。
    ///     待机：normal_idle；受击：normal_hit；死亡：normal_groggy（循环）。
    ///     Buff：normal_buff_1；攻击 1：normal_m_attack_01；攻击 2：normal_m_attack_02。
    /// </summary>
    protected override CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
    {
        AnimState idle = new("normal_idle", isLooping: true);

        AnimState hit = new("normal_hit") { NextState = idle };
        AnimState dead = new("normal_groggy", isLooping: true);

        AnimState buff = new("normal_buff_1") { NextState = idle };
        AnimState attack01 = new("normal_m_attack_01") { NextState = idle };
        AnimState attack02 = new("normal_m_attack_02") { NextState = idle };

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Buff", buff);
        animator.AddAnyState("Attack01", attack01);
        animator.AddAnyState("Attack02", attack02);
        // 兜底：原版调用 "Attack" 时仍然播放攻击 1。
        animator.AddAnyState("Attack", attack01);
        return animator;
    }
}
