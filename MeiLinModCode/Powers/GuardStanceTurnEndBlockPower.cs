using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace MeiLinMod.MeiLinModCode.Powers;

public class GuardStanceTurnEndBlockPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, System.Collections.Generic.IEnumerable<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants)
    {
        if (side != Owner.Side)
            return;

        if (XiangzuCombatState.IsInGuardStance(Owner))
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move | ValueProp.Unpowered, null, fast: true);

        if (!XiangzuCombatState.IsInAttackStance(Owner))
            return;

        var enemies = CombatState.HittableEnemies.ToList();
        if (enemies.Count == 0)
            return;

        var player = Owner.Player;
        if (player == null)
            return;

        var target = player.RunState.Rng.CombatTargets.NextItem(enemies);
        if (target == null)
            return;

        await CreatureCmd.Damage(choiceContext, target, Amount + 1m, ValueProp.Move | ValueProp.Unpowered, Owner, null);
    }
}
