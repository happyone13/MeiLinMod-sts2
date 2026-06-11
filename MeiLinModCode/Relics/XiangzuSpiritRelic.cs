using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;

namespace MeiLinMod.MeiLinModCode.Relics;

public class XiangzuSpiritRelic : MeiLinModRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        MeiLinHoverTipFactory.XiangzuLegacy,
        MeiLinHoverTipFactory.Qi
    ];

    public override string PackedIconPath => "xiangzu_spirit_relic.png".RelicImagePath();
    protected override string PackedIconOutlinePath => "xiangzu_spirit_relic_outline.png".RelicImagePath();
    protected override string BigIconPath => "xiangzu_spirit_relic.png".RelicImagePath();

    public override async Task BeforeCombatStart()
    {
        Flash();
        await PowerCmd.Apply<XiangzuLegacyPower>(new BlockingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, null);
        await PowerCmd.Remove<StanceYuPower>(Owner.Creature);
        await PowerCmd.Apply<StanceGongPower>(new BlockingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, null, silent: true);
        await PowerCmd.Apply<XiangzuSpiritPower>(new BlockingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, null, silent: true);
    }
}
