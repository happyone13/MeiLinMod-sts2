using MegaCrit.Sts2.Core.Entities.Powers;
using MeiLinMod.MeiLinModCode.Extensions;

namespace MeiLinMod.MeiLinModCode.Powers;

public class QiPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    
}
