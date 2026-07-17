using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace MeiLinMod.MeiLinModCode.Monsters.Gloomy.Powers;

/// <summary>
///     野性：每层使持有者的攻击伤害 +1（机制与原版 StrengthPower 完全一致，仅名称与图标不同）。
/// </summary>
[RegisterPower]
public sealed class GloomyWildPower : ModPowerTemplate
{
    private const string PowerIconTexturePath = "res://MeiLinMod/images/powers/gloomy_power.png";

    public override PowerAssetProfile AssetProfile => new(PowerIconTexturePath, PowerIconTexturePath);
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool AllowNegative => true;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource, CardPlay? cardPlay)
    {
        if (Owner != dealer)
            return 0m;

        if (!props.IsPoweredAttack())
            return 0m;

        return Amount;
    }
}
