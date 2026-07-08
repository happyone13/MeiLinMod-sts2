using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Potions;
using MeiLinMod.MeiLinModCode.Powers;
using MeiLinMod.MeiLinModCode.Relics;
using STS2RitsuLib.Content;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class LocalizationTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-localization");
    }

    [Fact]
    public async Task Core_model_localization_keys_exist_in_english_and_chinese()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        foreach (var language in new[] { "eng", "zhs" })
        {
            LocManager.Instance.SetLanguage(language);
            AssertCoreKeysExist(language);
        }

        LocManager.Instance.SetLanguage("zhs");
    }

    private static void AssertCoreKeysExist(string language)
    {
        AssertLoc(language, "characters", "MEILINMOD_MEI_LIN_MOD.title");
        AssertLoc(language, "characters", "MEILINMOD_MEI_LIN_MOD.titleObject");
        AssertLoc(language, "characters", "MEILINMOD_MEI_LIN_MOD.description");

        foreach (var type in GetConcreteModTypes<MeiLinModCard>())
        {
            var entry = LegacyEntry(type);
            AssertLoc(language, "cards", $"{entry}.title");
            AssertLoc(language, "cards", $"{entry}.description");
        }

        foreach (var type in GetConcreteModTypes<MeiLinModRelic>())
        {
            var entry = LegacyEntry(type);
            AssertLoc(language, "relics", $"{entry}.title");
            AssertLoc(language, "relics", $"{entry}.description");
            AssertLoc(language, "relics", $"{entry}.flavor");
        }

        foreach (var type in GetConcreteModTypes<MeiLinModPotion>())
        {
            var entry = LegacyEntry(type);
            AssertLoc(language, "potions", $"{entry}.title");
            AssertLoc(language, "potions", $"{entry}.description");
        }

        foreach (var type in GetConcreteModTypes<MeiLinModPower>())
        {
            var entry = LegacyEntry(type);
            AssertLoc(language, "powers", $"{entry}.title");
            AssertLoc(language, "powers", $"{entry}.description");
            AssertLoc(language, "powers", $"{entry}.smartDescription");
        }
    }

    private static void AssertLoc(string language, string table, string key)
    {
        var text = new LocString(table, key).GetRawText();

        Assert.False(
            string.IsNullOrWhiteSpace(text) ||
            string.Equals(text, key, StringComparison.Ordinal) ||
            text.Contains("MISSING", StringComparison.OrdinalIgnoreCase),
            $"Missing or unresolved localization. language={language}, table={table}, key={key}, text={text}");
    }

    private static IEnumerable<Type> GetConcreteModTypes<TBase>()
    {
        return typeof(TBase)
            .Assembly
            .GetTypes()
            .Where(type => typeof(TBase).IsAssignableFrom(type) && type is { IsAbstract: false, IsInterface: false })
            .OrderBy(type => type.FullName, StringComparer.Ordinal);
    }

    private static string LegacyEntry(Type type)
    {
        return $"MEILINMOD_{ModContentRegistry.NormalizePublicStem(type.Name)}";
    }
}
