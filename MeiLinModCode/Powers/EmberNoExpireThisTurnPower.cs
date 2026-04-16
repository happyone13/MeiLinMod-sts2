using MegaCrit.Sts2.Core.Entities.Powers;

namespace MeiLinMod.MeiLinModCode.Powers;

public class EmberNoExpireThisTurnPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
}
