using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace MeiLinMod.MeiLinModCode.Relics;

public class LongZhuaRelic : MeiLinModRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task BeforeCombatStartLate()
    {
        Flash();
        var enemies = Owner.Creature.CombatState?.HittableEnemies.ToList() ?? [];
        if (enemies.Count == 0)
            return;

        foreach (var enemy in enemies)
            await PowerCmd.Apply<EmberPower>(new BlockingPlayerChoiceContext(), enemy, 2m, Owner.Creature, null);
    }
}
