using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Compat;
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
#if STS2_104
        var enemies = Owner.Creature.CombatState?.HittableEnemies.ToList() ?? [];
#else
        var enemies = CombatStateCompat.GetHittableEnemies(Owner.PlayerCombatState);
#endif
        if (enemies.Count == 0)
            return;

        foreach (var enemy in enemies)
            await PowerCmd.Apply<EmberPower>(enemy, 2m, Owner.Creature, null);
    }
}
