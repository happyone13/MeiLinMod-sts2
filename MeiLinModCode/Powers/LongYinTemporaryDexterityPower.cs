using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MeiLinMod.MeiLinModCode.Cards;

namespace MeiLinMod.MeiLinModCode.Powers;

public class LongYinTemporaryDexterityPower : TemporaryDexterityPower, ICustomModel
{
    public override AbstractModel OriginModel => ModelDb.Card<LongYin>();
}
