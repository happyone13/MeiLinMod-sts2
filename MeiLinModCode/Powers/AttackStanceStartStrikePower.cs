using System.Linq;
using MeiLinMod.MeiLinModCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class AttackStanceStartStrikePower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
            return;

        for (var i = 0; i < Amount; i++)
        {
            var candidates = PileType.Draw.GetPile(player).Cards
                .Concat(PileType.Discard.GetPile(player).Cards)
                .Where(BasicStrikeDefendHelper.IsBasicStrike)
                .ToList();
            if (candidates.Count == 0)
                return;

            var selected = player.RunState.Rng.CombatTargets.NextItem(candidates);
            if (selected == null)
                return;

            await CardCmd.AutoPlay(choiceContext, selected, ResolveTargetFor(selected));
        }
    }

    private Creature? ResolveTargetFor(CardModel card)
    {
        if (card.TargetType != TargetType.AnyEnemy)
            return null;

        var enemies = CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0)
            return null;

        return Owner.Player?.RunState.Rng.CombatTargets.NextItem(enemies);
    }
}
