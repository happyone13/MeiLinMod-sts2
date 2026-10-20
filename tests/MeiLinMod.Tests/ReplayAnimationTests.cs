using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
using MeiLinMod.MeiLinModCode.Services;
using MeiLinMod.MeiLinModCode.Vfx;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class ReplayAnimationTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle) => battle
        .Player<MeiLinCharacter>().AddEnemy<BigDummy>().WithSeed("meilin-replay-animation");

    [Fact]
    public async Task Replays_keep_all_damage_and_replaying_the_card_again_starts_a_new_animation()
    {
        var card = await AddToHand<StrikeMeilin>();
        card.BaseReplayCount = 2;
        decimal hpBefore = EnemyAt(0).CurrentHp;
        decimal damage = card.DynamicVars.Damage.BaseValue;
        using var probe = new ReplayProbe(Player.Creature);

        await Play(card, EnemyAt(0));
        await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top);
        await PlayerCmd.SetEnergy(3m, Player);
        await Play(card, EnemyAt(0));

        Assert.Equal(hpBefore - 6m * damage, (decimal)EnemyAt(0).CurrentHp);
        Assert.Equal(new[] { false, true, true, false, true, true }, probe.Plays.Select(p => p.Skip).ToArray());
        Assert.Equal(new[] { true, false, false, true, false, false }, probe.AttackerAnimations.ToArray());
        Assert.Equal(8, probe.ReplayHitCallbacks.Count);
        Assert.All(probe.ReplayHitCallbacks, cinematic => Assert.False(cinematic));
        Assert.False(MeiLinReplayVisuals.ShouldSkip(Player.Creature));
    }

    [Fact]
    public async Task Auto_played_children_have_their_own_animation_inside_a_replayed_parent()
    {
        await AddToHand<StrikeMeilin>();
        var parent = await AddToHand<ShenGongFangYiTi>();
        parent.BaseReplayCount = 1;
        bool previous = MeiLinSharedSettings.UltimateCinematicsEnabled;
        MeiLinSharedSettings.SetUltimateCinematicsEnabled(false, persist: false);
        try
        {
            using var probe = new ReplayProbe(Player.Creature);
            await Play(parent);
            Assert.Equal(new[] { false, true }, probe.Plays.Where(p => p.Card == parent).Select(p => p.Skip).ToArray());
            var children = probe.Plays.Where(p => p.Card != parent).ToArray();
            Assert.NotEmpty(children);
            Assert.All(children, child => Assert.False(child.Skip));
            Assert.False(MeiLinReplayVisuals.ShouldSkip(Player.Creature));
        }
        finally
        {
            MeiLinSharedSettings.SetUltimateCinematicsEnabled(previous, persist: false);
        }
    }

    private sealed class ReplayProbe : IDisposable
    {
        private static ReplayProbe? _active;
        private readonly Creature _owner;
        private readonly Harmony _harmony = new("MeiLinMod.Tests.ReplayVisualProbe");
        public List<(CardModel Card, bool Skip)> Plays { get; } = [];
        public List<bool> AttackerAnimations { get; } = [];
        public List<bool> ReplayHitCallbacks { get; } = [];

        public ReplayProbe(Creature owner)
        {
            _owner = owner;
            _active = this;
            _harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.BeforeCardPlayed), [typeof(CombatState), typeof(CardPlay)]),
                postfix: new HarmonyMethod(typeof(ReplayProbe), nameof(AfterBeforeCardPlayed)));
            _harmony.Patch(AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute)),
                prefix: new HarmonyMethod(typeof(ReplayProbe), nameof(BeforeAttack)) { priority = Priority.Last });
        }

        private static void AfterBeforeCardPlayed(CardPlay cardPlay, ref Task __result)
        {
            if (_active is { } probe && cardPlay.Card.Owner?.Creature == probe._owner)
                __result = probe.ObserveAsync(__result, cardPlay);
        }

        private async Task ObserveAsync(Task original, CardPlay play)
        {
            await original;
            await Task.Yield(); // Also verify the replay scope survives an async continuation.
            bool skip = MeiLinReplayVisuals.ShouldSkip(_owner);
            Plays.Add((play.Card, skip));
            if (!skip)
                return;
            Task Hit(bool cinematic)
            {
                ReplayHitCallbacks.Add(cinematic);
                return Task.CompletedTask;
            }
            await MeiLinUgPresentation.PlayAsync(_owner, [], Hit);
            await MeiLinUxPresentation.PlayAsync(_owner, [], Hit);
        }

        private static void BeforeAttack(AttackCommand __instance)
        {
            if (_active is { } probe && __instance.CardPlay?.Card.Owner?.Creature == probe._owner)
                probe.AttackerAnimations.Add((bool)AccessTools.Field(typeof(AttackCommand), "_shouldPlayAnimation").GetValue(__instance)!);
        }

        public void Dispose()
        {
            _harmony.UnpatchAll(_harmony.Id);
            _active = null;
        }
    }
}
