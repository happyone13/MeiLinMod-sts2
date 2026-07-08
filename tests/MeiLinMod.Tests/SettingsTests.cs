using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class SettingsTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-shared-settings");
    }

    [Fact]
    public async Task Shared_settings_round_trip_and_clamp_values()
    {
        var originalOverlay = MeiLinSharedSettings.BattleReadyOverlayEnabled;
        var originalEffects = MeiLinSharedSettings.CombatEffectsEnabled;
        var originalDynamicCards = MeiLinSharedSettings.DynamicCardPortraitsEnabled;
        var originalVoice = MeiLinSharedSettings.VoiceVolume;
        var originalScale = MeiLinSharedSettings.BattleReadyScale;
        var originalOffsetX = MeiLinSharedSettings.BattleReadyOffsetX;
        var originalOffsetY = MeiLinSharedSettings.BattleReadyOffsetY;

        try
        {
            var defend = await AddToHand<DefendMeilin>();
            await Play(defend);

            MeiLinSharedSettings.SetBattleReadyOverlayEnabled(false, persist: false);
            MeiLinSharedSettings.SetCombatEffectsEnabled(false, persist: false);
            MeiLinSharedSettings.SetDynamicCardPortraitsEnabled(false, persist: false);
            MeiLinSharedSettings.SetVoiceVolume(0.35f, persist: false);
            MeiLinSharedSettings.SetBattleReadyScale(1.4f, persist: false);
            MeiLinSharedSettings.SetBattleReadyOffsetX(123f, persist: false);
            MeiLinSharedSettings.SetBattleReadyOffsetY(-45f, persist: false);

            Assert.False(MeiLinSharedSettings.BattleReadyOverlayEnabled);
            Assert.False(MeiLinSharedSettings.CombatEffectsEnabled);
            Assert.False(MeiLinSharedSettings.DynamicCardPortraitsEnabled);
            AssertNearly(0.35f, MeiLinSharedSettings.VoiceVolume);
            AssertNearly(1.4f, MeiLinSharedSettings.BattleReadyScale);
            AssertNearly(123f, MeiLinSharedSettings.BattleReadyOffsetX);
            AssertNearly(-45f, MeiLinSharedSettings.BattleReadyOffsetY);

            MeiLinSharedSettings.SetVoiceVolume(2f, persist: false);
            MeiLinSharedSettings.SetBattleReadyScale(0.1f, persist: false);
            MeiLinSharedSettings.SetBattleReadyOffsetX(999f, persist: false);
            MeiLinSharedSettings.SetBattleReadyOffsetY(-999f, persist: false);

            AssertNearly(1f, MeiLinSharedSettings.VoiceVolume);
            AssertNearly(0.5f, MeiLinSharedSettings.BattleReadyScale);
            AssertNearly(400f, MeiLinSharedSettings.BattleReadyOffsetX);
            AssertNearly(-400f, MeiLinSharedSettings.BattleReadyOffsetY);
        }
        finally
        {
            MeiLinSharedSettings.SetBattleReadyOverlayEnabled(originalOverlay, persist: false);
            MeiLinSharedSettings.SetCombatEffectsEnabled(originalEffects, persist: false);
            MeiLinSharedSettings.SetDynamicCardPortraitsEnabled(originalDynamicCards, persist: false);
            MeiLinSharedSettings.SetVoiceVolume(originalVoice, persist: false);
            MeiLinSharedSettings.SetBattleReadyScale(originalScale, persist: false);
            MeiLinSharedSettings.SetBattleReadyOffsetX(originalOffsetX, persist: false);
            MeiLinSharedSettings.SetBattleReadyOffsetY(originalOffsetY, persist: false);
        }
    }

    [Fact]
    public async Task Shared_settings_schema_stays_compatible_with_yuki_chaos()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        Assert.Equal("chaosmod", PrivateStaticString("SharedSettingsDirName"));
        Assert.Equal("xcskin_settings.json", PrivateStaticString("SharedSettingsFileName"));
        Assert.Equal("CHAOSMOD_XCSKIN_", PrivateStaticString("SharedDomainKeyPrefix"));

        var expectedAppDomainKeys = new Dictionary<string, string>
        {
            ["SharedVoiceVolumeKey"] = "CHAOSMOD_XCSKIN_VOICE_VOLUME",
            ["SharedBattleReadyScaleKey"] = "CHAOSMOD_XCSKIN_BATTLE_READY_SCALE",
            ["SharedBattleReadyOffsetXKey"] = "CHAOSMOD_XCSKIN_BATTLE_READY_OFFSET_X",
            ["SharedBattleReadyOffsetYKey"] = "CHAOSMOD_XCSKIN_BATTLE_READY_OFFSET_Y",
            ["SharedBattleReadyOverlayEnabledKey"] = "CHAOSMOD_XCSKIN_PORTRAITS_ENABLED",
            ["SharedCombatEffectsEnabledKey"] = "CHAOSMOD_XCSKIN_ACTION_VFX_ENABLED",
            ["SharedDynamicCardPortraitsKey"] = "CHAOSMOD_XCSKIN_DYNAMIC_CARD_PORTRAITS_ENABLED",
            ["LegacyDynamicCardPortraitsKey"] = "CHAOSMOD_XCSKIN_DYNAMIC_CARD_PORTRAITS"
        };

        foreach (var (fieldName, expectedKey) in expectedAppDomainKeys)
            Assert.Equal(expectedKey, PrivateStaticString(fieldName));

        var settingsType = typeof(MeiLinSharedSettings).GetNestedType("XCskinSettings", BindingFlags.NonPublic);
        Assert.NotNull(settingsType);

        var jsonProperties = settingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "ActionVfxEnabled",
                "BattleReadyOffsetX",
                "BattleReadyOffsetY",
                "BattleReadyScale",
                "DynamicCardPortraitsEnabled",
                "PortraitsEnabled",
                "Volume"
            ],
            jsonProperties);

        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Mechanics", "Settings", "MeiLinSharedSettings.cs"));
        Assert.Contains("\"BattleReadyOverlayEnabled\"", source);
        Assert.Contains("\"CombatEffectsEnabled\"", source);
        Assert.Contains("\"UseDynamicCardPortraits\"", source);
    }

    [Fact]
    public async Task Ritsu_settings_page_declares_expected_shared_controls_and_bindings()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Migration", "MeiLinRitsuMigration.cs"));

        Assert.Contains("private const string SettingsPageId = \"meilin-settings\";", source);
        Assert.Contains("RitsuLibFramework.RegisterModSettings(", source);
        Assert.Contains("SettingsPageId);", source);
        Assert.False(File.Exists(RepoFile("MeiLinModCode", "Mechanics", "Settings", "MeiLinSharedSettingsUiPatch.cs")));
        Assert.Contains("ModSettingsText.Literal(\"美铃设置\")", source);
        Assert.Contains("ModSettingsText.Literal(\"通过 RitsuLib 注册的 MeiLinMod 设置页。相关设置继续复用 Yuki/Chaos 共享配置。\")", source);
        Assert.Contains("ModSettingsText.Literal(\"显示\")", source);
        Assert.Contains("ModSettingsText.Literal(\"背身立绘\")", source);
        Assert.Contains("ModSettingsText.Literal(\"战斗特效\")", source);
        Assert.Contains("ModSettingsText.Literal(\"动态卡图\")", source);
        Assert.Contains("ModSettingsText.Literal(\"背身立绘调整\")", source);
        Assert.Contains("ModSettingsText.Literal(\"立绘缩放\")", source);
        Assert.Contains("ModSettingsText.Literal(\"立绘 X 偏移\")", source);
        Assert.Contains("ModSettingsText.Literal(\"立绘 Y 偏移\")", source);
        Assert.Contains("ModSettingsText.Literal(\"音频\")", source);
        Assert.Contains("ModSettingsText.Literal(\"语音音量\")", source);

        Assert.Contains("page.AddSection(\"visuals\"", source);
        Assert.Contains("page.AddSection(\"portrait_transform\"", source);
        Assert.Contains("page.AddSection(\"audio\"", source);

        var expectedControls = new Dictionary<string, string>
        {
            ["battle_ready_overlay"] = "MeiLinSharedSettings.BattleReadyOverlayEnabled",
            ["combat_effects"] = "MeiLinSharedSettings.CombatEffectsEnabled",
            ["dynamic_card_portraits"] = "MeiLinSharedSettings.DynamicCardPortraitsEnabled",
            ["battle_ready_scale"] = "MeiLinSharedSettings.BattleReadyScale",
            ["battle_ready_offset_x"] = "MeiLinSharedSettings.BattleReadyOffsetX",
            ["battle_ready_offset_y"] = "MeiLinSharedSettings.BattleReadyOffsetY",
            ["voice_volume"] = "MeiLinSharedSettings.VoiceVolume"
        };

        foreach (var (controlId, sharedGetter) in expectedControls)
        {
            Assert.Contains($"\"{controlId}\"", source);
            Assert.Contains(sharedGetter, source);
            Assert.True(
                CountOccurrences(source, $"\"{controlId}\"") >= 2,
                $"Settings control '{controlId}' should be used both as the control id and binding key.");
        }

        Assert.Contains("MeiLinSharedSettings.SetBattleReadyOverlayEnabled(value, persist: true);", source);
        Assert.Contains("MeiLinSharedSettings.SetCombatEffectsEnabled(value, persist: true)", source);
        Assert.Contains("MeiLinSharedSettings.SetDynamicCardPortraitsEnabled(value, persist: true)", source);
        Assert.Contains("MeiLinSharedSettings.SetBattleReadyScale((float)value, persist: true);", source);
        Assert.Contains("MeiLinSharedSettings.SetBattleReadyOffsetX((float)value, persist: true);", source);
        Assert.Contains("MeiLinSharedSettings.SetBattleReadyOffsetY((float)value, persist: true);", source);
        Assert.Contains("MeiLinSharedSettings.SetVoiceVolume((float)value, persist: true)", source);

        Assert.Contains("MeiLinBattleReadyOverlay.ApplyTransformFromSettings();", source);
        Assert.Contains("MeiLinBattleReadyOverlay.NotifyCombatEnded();", source);
        Assert.Contains("minValue: 0.5d", source);
        Assert.Contains("maxValue: 2d", source);
        Assert.Contains("minValue: -400d", source);
        Assert.Contains("maxValue: 400d", source);
        Assert.Contains("minValue: 0d", source);
        Assert.Contains("maxValue: 1d", source);
    }

    private static void AssertNearly(float expected, float actual)
    {
        Assert.True(Math.Abs(expected - actual) < 0.0001f, $"Expected {expected}, got {actual}.");
    }

    private static int CountOccurrences(string source, string token)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

    private static string PrivateStaticString(string fieldName)
    {
        var field = typeof(MeiLinSharedSettings).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException($"Missing MeiLinSharedSettings field {fieldName}.");

        object? value = field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null);
        return Assert.IsType<string>(value);
    }

    private static string RepoFile(params string[] segments)
    {
        return Path.Combine(RepositoryRoot(), Path.Combine(segments));
    }

    private static string RepositoryRoot([CallerFilePath] string sourcePath = "")
    {
        var testDirectory = Path.GetDirectoryName(sourcePath)
                            ?? throw new InvalidOperationException("CallerFilePath did not provide a source directory.");

        return Path.GetFullPath(Path.Combine(testDirectory, "..", ".."));
    }
}
