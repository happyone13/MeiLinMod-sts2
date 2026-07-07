using System.Reflection;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Patches;
using MeiLinMod.MeiLinModCode.Vfx;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class GodotScriptRegistrationTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-godot-script-registration");
    }

    [Fact]
    public async Task Script_path_types_keep_entry_bridge_registration_and_existing_source_files()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var scriptPaths = typeof(MainFile)
            .Assembly
            .GetTypes()
            .SelectMany(type => type.GetCustomAttributesData()
                .Where(attribute => attribute.AttributeType.Name == "ScriptPathAttribute")
                .Select(attribute => (Type: type, Path: attribute.ConstructorArguments.Single().Value as string)))
            .ToArray();

        Assert.NotEmpty(scriptPaths);
        Assert.Contains(
            "ScriptManagerBridge.LookupScriptsInAssembly(assembly);",
            File.ReadAllText(RepoFile("MeiLinModCode", "Entry", "MeiLinModEntry.cs")));

        foreach (var (type, scriptPath) in scriptPaths)
        {
            Assert.False(string.IsNullOrWhiteSpace(scriptPath), $"{type.FullName} has an empty ScriptPath.");
            Assert.StartsWith("res://", scriptPath, StringComparison.Ordinal);
            Assert.True(
                File.Exists(RepoFile(scriptPath!["res://".Length..].Split('/'))),
                $"{type.FullName} points to missing script file: {scriptPath}");
        }
    }

    [Fact]
    public async Task Entry_initialization_keeps_visual_registration_order_and_existing_preload_assets()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var entrySource = File.ReadAllText(RepoFile("MeiLinModCode", "Entry", "MeiLinModEntry.cs"));
        AssertInOrder(
            entrySource,
            "MeiLinRitsuMigration.Initialize();",
            "ScriptManagerBridge.LookupScriptsInAssembly(assembly);",
            "MeiLinSharedSettings.EnsureSettingsLoaded();",
            "CardSpinePortraitPatch.PreloadDynamicPortraitScenes();",
            "MeiLinCommandVfxCoordinator.PreloadConfiguredScenes();",
            "MeiLinAttackMovementController.PreloadMovementEffects();",
            "MeiLinStanceVfxController.PreloadStanceEffects();");

        foreach (var scenePath in DynamicPortraitScenePaths())
            AssertResourcePathExists(scenePath);

        foreach (var scenePath in MeiLinAttackMovementController.GetPreloadScenePaths())
            AssertResourcePathExists(scenePath);

        foreach (var scenePath in MeiLinMod.MeiLinModCode.StanceVfx.MeiLinStanceVfxController.GetPreloadScenePaths())
            AssertResourcePathExists(scenePath);
    }

    private static void AssertInOrder(string source, params string[] tokens)
    {
        int previousIndex = -1;
        foreach (var token in tokens)
        {
            int index = source.IndexOf(token, StringComparison.Ordinal);
            Assert.True(index >= 0, $"Missing initialization call: {token}");
            Assert.True(index > previousIndex, $"Initialization call is out of order: {token}");
            previousIndex = index;
        }
    }

    private static IEnumerable<string> DynamicPortraitScenePaths()
    {
        var field = typeof(CardSpinePortraitPatch).GetField("DynamicPortraitScenePaths", BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException("CardSpinePortraitPatch.DynamicPortraitScenePaths is missing.");

        return Assert.IsType<string[]>(field.GetValue(null));
    }

    private static void AssertResourcePathExists(string scenePath)
    {
        Assert.StartsWith("res://", scenePath, StringComparison.Ordinal);
        Assert.True(
            File.Exists(RepoFile(scenePath["res://".Length..].Split('/'))),
            $"Preload resource path points to a missing file: {scenePath}");
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
