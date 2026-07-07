using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MeiLinMod.MeiLinModCode.HoverTips;

namespace MeiLinMod.MeiLinModCode.Powers;

public class StanceGongPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [MeiLinHoverTipFactory.Qi];
}
