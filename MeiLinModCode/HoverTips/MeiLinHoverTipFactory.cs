using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MeiLinMod.MeiLinModCode.Powers;

namespace MeiLinMod.MeiLinModCode.HoverTips;

public static class MeiLinHoverTipFactory
{
    private static readonly IHoverTip AwakeningHoverTip = new HoverTip(
        new LocString("card_keywords", "AWAKENING.title"),
        new LocString("card_keywords", "AWAKENING.description"));

    private static readonly IHoverTip EmberHoverTip = new HoverTip(
        new LocString("card_keywords", "EMBER.title"),
        new LocString("card_keywords", "EMBER.description"));
    private static readonly IHoverTip QiHoverTip = new HoverTip(
        new LocString("card_keywords", "QI.title"),
        new LocString("card_keywords", "QI.description"));
    private static readonly IHoverTip QiGaugeHoverTip = new HoverTip(
        new LocString("card_keywords", "QI_GAUGE.title"),
        new LocString("card_keywords", "QI_GAUGE.description"));
    private static readonly IHoverTip QiConsumeHoverTip = new HoverTip(
        new LocString("card_keywords", "QI_CONSUME.title"),
        new LocString("card_keywords", "QI_CONSUME.description"));
    private static readonly IHoverTip XiangzuLegacyHoverTip = new HoverTip(
        new LocString("card_keywords", "XIANGZU_LEGACY.title"),
        new LocString("card_keywords", "XIANGZU_LEGACY.description"));
    private static readonly IHoverTip AttackStanceHoverTip = new HoverTip(
        new LocString("card_keywords", "ATTACK_STANCE.title"),
        new LocString("card_keywords", "ATTACK_STANCE.description"));
    private static readonly IHoverTip GuardStanceHoverTip = new HoverTip(
        new LocString("card_keywords", "GUARD_STANCE.title"),
        new LocString("card_keywords", "GUARD_STANCE.description"));

    public static IHoverTip Awakening => AwakeningHoverTip;

    public static IHoverTip Ember => EmberHoverTip;

    public static IHoverTip Qi => QiHoverTip;
    public static IHoverTip QiGauge => QiGaugeHoverTip;
    public static IHoverTip QiConsume => QiConsumeHoverTip;

    public static IHoverTip XiangzuLegacy => XiangzuLegacyHoverTip;

    public static IHoverTip AttackStance => AttackStanceHoverTip;

    public static IHoverTip GuardStance => GuardStanceHoverTip;
}
