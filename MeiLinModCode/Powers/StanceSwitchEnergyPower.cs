using MegaCrit.Sts2.Core.Entities.Powers;

namespace MeiLinMod.MeiLinModCode.Powers;

public class StanceSwitchEnergyPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
