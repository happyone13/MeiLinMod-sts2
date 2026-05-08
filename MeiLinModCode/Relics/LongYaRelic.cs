using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace MeiLinMod.MeiLinModCode.Relics;

public class LongYaRelic : MeiLinModRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await QiCounterPower.AddProgress(Owner.Creature, 2, Owner.Creature, null);
    }
}
