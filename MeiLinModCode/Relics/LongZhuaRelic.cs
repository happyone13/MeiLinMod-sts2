using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace MeiLinMod.MeiLinModCode.Relics;

[Pool(typeof(MeiLinModRelicPool))]
public class LongZhuaRelic : MeiLinModRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task BeforeCombatStartLate()
    {
        Flash();
        var combatState = Owner.Creature.CombatState;
        if (combatState == null)
            return;

        foreach (var enemy in combatState.HittableEnemies.ToList())
            await PowerCmd.Apply<EmberPower>(enemy, 2m, Owner.Creature, null);
    }
}
