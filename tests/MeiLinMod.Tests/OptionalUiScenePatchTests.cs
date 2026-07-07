using System.Reflection;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Patches;
using STS2RitsuLib.Patching.Models;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class OptionalUiScenePatchTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-optional-ui-scene-patches");
    }

    [Fact]
    public async Task Optional_ui_and_scene_patches_stay_non_critical_with_stable_descriptions()
    {
        await InitializeBattle();

        Type[] patchTypes =
        [
            typeof(StatsScreenMeiLinPatch),
            typeof(YukiSettingsPanelEmptyReadyCompatPatch),
            typeof(GameOverAnimationFallbackOnMegaStatePatch),
            typeof(MerchantCharacterAnimationFallbackPatch),
            typeof(MerchantCharacterPlayAnimationFallbackPatch),
            typeof(RestSiteCharacterAnimationFallbackPatch)
        ];

        foreach (var patchType in patchTypes)
        {
            Assert.False(ReadStatic<bool>(patchType, nameof(IPatchMethod.IsCritical)));
            Assert.False(string.IsNullOrWhiteSpace(ReadStatic<string>(patchType, nameof(IPatchMethod.PatchId))));
            Assert.False(string.IsNullOrWhiteSpace(ReadStatic<string>(patchType, nameof(IPatchMethod.Description))));
            Assert.NotEmpty(InvokeStatic<ModPatchTarget[]>(patchType, nameof(IPatchMethod.GetTargets)));
        }
    }

    [Fact]
    public async Task Scene_fallback_source_keeps_meilin_scope_and_safe_animation_order()
    {
        await InitializeBattle();

        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Patches", "CharacterAnimationFallbackPatch.cs"));
        Assert.Contains("path.Contains(\"MeiLinMod/scenes/\"", source);
        Assert.Contains("public static readonly string[] MerchantFallbacks = [\"relaxed_loop\", \"stop\", \"camping\", \"b_idle\", \"idle\"];", source);
        Assert.Contains("public static readonly string[] RestFallbacks = [\"overgrowth_loop\", \"hive_loop\", \"glory_loop\", \"camping\", \"b_idle\", \"idle\"];", source);
        Assert.Contains("anim = \"idle\";", source);
        Assert.Contains("name.Contains(\"MeiLin\"", source);
        Assert.Contains("catch", source);
        Assert.Contains("return false;", source);
    }

    [Fact]
    public async Task Game_over_fallback_only_retries_missing_die_animation_as_death()
    {
        await InitializeBattle();

        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Patches", "GameOverAnimationFallbackPatch.cs"));
        Assert.Contains("private static bool _fallbackInProgress;", source);
        Assert.Contains("Exception? __exception", source);
        Assert.Contains("if (__exception == null || _fallbackInProgress)", source);
        Assert.Contains("string.Equals(__0, \"die\", System.StringComparison.Ordinal)", source);
        Assert.Contains("__instance.SetAnimation(\"death\", __1, __2)", source);
        Assert.Contains("return null;", source);
        Assert.Contains("return __exception;", source);
        Assert.Contains("_fallbackInProgress = false;", source);
    }

    [Fact]
    public async Task Stats_screen_patch_keeps_duplicate_and_missing_stats_guards()
    {
        await InitializeBattle();

        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Patches", "StatsScreenMeiLinPatch.cs"));
        Assert.Contains("private const string MeiLinStatsNodeName = \"MeiLinStats\";", source);
        Assert.Contains("CharacterStatContainerRef", source);
        Assert.Contains("characterStatContainer.HasNode(MeiLinStatsNodeName)", source);
        Assert.Contains("if (meiLinStats == null)", source);
        Assert.Contains("SaveManager.Instance.Progress.GetStatsForCharacter(meiLinId)", source);
        Assert.Contains("statsNode.Name = MeiLinStatsNodeName;", source);
    }

    [Fact]
    public async Task Yuki_settings_compat_patch_only_suppresses_empty_yuki_mod_panel_ready_exception()
    {
        await InitializeBattle();

        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Patches", "YukiSettingsPanelEmptyReadyCompatPatch.cs"));
        Assert.Contains("private const string YukiModPanelName = \"XCskin_ModSettingsPanel\";", source);
        Assert.Contains("exception is not InvalidOperationException || exception.Message != \"Sequence contains no elements\"", source);
        Assert.Contains("panel.Name.ToString()", source);
        Assert.Contains("vbox == null || vbox.GetChildCount() == 0", source);
        Assert.Contains("return __exception;", source);
        Assert.Contains("return null;", source);
    }

    private async Task InitializeBattle()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);
    }

    private static T ReadStatic<T>(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(property);
        return Assert.IsType<T>(property.GetValue(null));
    }

    private static T InvokeStatic<T>(Type type, string methodName)
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<T>(method.Invoke(null, null));
    }

    private static string RepoFile(params string[] segments)
    {
        return Path.Combine(RepositoryRoot(), Path.Combine(segments));
    }

    private static string RepositoryRoot([System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
    {
        var testDirectory = Path.GetDirectoryName(sourcePath)
                            ?? throw new InvalidOperationException("CallerFilePath did not provide a source directory.");

        return Path.GetFullPath(Path.Combine(testDirectory, "..", ".."));
    }
}
