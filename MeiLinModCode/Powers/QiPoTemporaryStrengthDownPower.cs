using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MeiLinMod.MeiLinModCode.Cards;

namespace MeiLinMod.MeiLinModCode.Powers;

public class QiPoTemporaryStrengthDownPower : TemporaryStrengthPower, ICustomModel
{
    public override AbstractModel OriginModel => ModelDb.Card<QiPo>();
    protected override bool IsPositive => false;
}
