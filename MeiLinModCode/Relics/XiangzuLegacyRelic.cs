using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace MeiLinMod.MeiLinModCode.Relics;

public class XiangzuLegacyRelic : MeiLinModRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        MeiLinHoverTipFactory.XiangzuLegacy,
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiGauge
    ];
    
    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<XiangzuLegacyPower>(Owner.Creature, 1m, Owner.Creature, null);
        await PowerCmd.Remove<StanceYuPower>(Owner.Creature);
        await PowerCmd.Apply<StanceGongPower>(Owner.Creature, 1m, Owner.Creature, null, silent: true);
    }
}
