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

    [Fact]
    public async Task Fire_dragon_heart_english_text_matches_current_card_effect()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        LocManager.Instance.SetLanguage("eng");

        var description = new LocString("cards", "MEILINMOD_HUO_LONG_XIN_ZANG.description").GetRawText();
        Assert.Equal(
            "Lose 1 [gold]Qi[/gold] and gain {Energy:energyIcons()}. Return this card to your hand.",
            description);
        Assert.DoesNotContain("draw", description, StringComparison.OrdinalIgnoreCase);

        LocManager.Instance.SetLanguage("zhs");
    }

    [Fact]
    public async Task Combo_link_english_text_only_references_registered_dynamic_vars()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        LocManager.Instance.SetLanguage("eng");

        var card = await AddToHand<LianXie>();
        var description = new LocString("cards", "MEILINMOD_LIAN_XIE.description").GetRawText();

        Assert.Contains("{Damage:diff()}", description, StringComparison.Ordinal);
        Assert.DoesNotContain("{Block", description, StringComparison.Ordinal);
        Assert.True(card.DynamicVars.ContainsKey("Damage"));
        Assert.False(card.DynamicVars.ContainsKey("Block"));

        LocManager.Instance.SetLanguage("zhs");
    }

    private static void AssertCoreKeysExist(string language)
    {
        foreach (var suffix in new[]
                 {
                     "aromaPrinciple",
                     "banter.alive.endTurnPing",
                     "banter.dead.endTurnPing",
                     "cardsModifierDescription",
                     "cardsModifierTitle",
                     "description",
                     "eventDeathPrevention",
                     "goldMonologue",
                     "possessiveAdjective",
                     "pronounObject",
                     "pronounPossessive",
                     "pronounSubject",
                     "title",
                     "titleObject",
                     "unlockText"
                 })
        {
            AssertLoc(language, "characters", $"MEILINMOD_MEI_LIN_MOD.{suffix}");
        }

        foreach (var key in new[]
                 {
                     "MEILINMOD_RITSU_PAGE.title",
                     "MEILINMOD_RITSU_PAGE.description",
                     "MEILINMOD_RITSU_SECTION_VISUALS.title",
                     "MEILINMOD_RITSU_BATTLE_READY_OVERLAY.title",
                     "MEILINMOD_RITSU_COMBAT_EFFECTS.title",
                     "MEILINMOD_RITSU_DYNAMIC_CARD_PORTRAITS.title",
                     "MEILINMOD_RITSU_SECTION_PORTRAIT_TRANSFORM.title",
                     "MEILINMOD_RITSU_BATTLE_READY_SCALE.title",
                     "MEILINMOD_RITSU_BATTLE_READY_OFFSET_X.title",
                     "MEILINMOD_RITSU_BATTLE_READY_OFFSET_Y.title",
                     "MEILINMOD_RITSU_SECTION_AUDIO.title",
                     "MEILINMOD_RITSU_VOICE_VOLUME.title",
                     "MEILINMOD_RITSU_SECTION_GAMEPLAY.title",
                     "MEILINMOD_RITSU_GLOOMY_ENCOUNTER.title",
                     "MEILINMOD_RITSU_GLOOMY_ENCOUNTER.description"
                 })
        {
            AssertLoc(language, "settings_ui", key);
        }

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
