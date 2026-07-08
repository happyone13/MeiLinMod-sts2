using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class MigrationDependencyTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-migration-dependencies");
    }

    [Fact]
    public async Task Main_manifest_uses_ritsulib_dependency_without_baselib()
    {
        await InitializeBattle();

        using var manifest = JsonDocument.Parse(File.ReadAllText(RepoFile("MeiLinMod.json")));
        var root = manifest.RootElement;

        Assert.Equal("MeiLinMod", root.GetProperty("id").GetString());
        Assert.Equal("v0.3.1", root.GetProperty("version").GetString());
        Assert.Equal("v0.108.0", root.GetProperty("min_game_version").GetString());

        var dependencies = root.GetProperty("dependencies").EnumerateArray().ToArray();

        var ritsuDependency = Assert.Single(dependencies);
        Assert.Equal(JsonValueKind.Object, ritsuDependency.ValueKind);
        Assert.Equal("STS2-RitsuLib", ritsuDependency.GetProperty("id").GetString());
        Assert.Equal("0.4.50", ritsuDependency.GetProperty("min_version").GetString());
        Assert.DoesNotContain(dependencies, dependency =>
            dependency.ValueKind == JsonValueKind.String && dependency.GetString() == "BaseLib" ||
            dependency.ValueKind == JsonValueKind.Object && dependency.TryGetProperty("id", out var id) && id.GetString() == "BaseLib");
    }

    [Fact]
    public async Task Main_project_has_no_baselib_or_old_analyzer_package_references()
    {
        await InitializeBattle();

        var project = XDocument.Load(RepoFile("MeiLinMod.csproj"));
        var packageReferences = PackageReferenceIncludes(project).ToArray();

        Assert.Contains("STS2.RitsuLib", packageReferences);
        Assert.DoesNotContain("Alchyr.Sts2.BaseLib", packageReferences);
        Assert.DoesNotContain("Alchyr.Sts2.ModAnalyzers", packageReferences);
    }

    [Fact]
    public async Task Export_configuration_excludes_development_paths_and_uses_msil_architecture()
    {
        await InitializeBattle();

        var exportPreset = File.ReadAllText(RepoFile("export_presets.cfg"));
        var project = XDocument.Load(RepoFile("MeiLinMod.csproj"));

        Assert.Contains("binary_format/architecture=\"msil\"", exportPreset);
        Assert.Contains("LocalModsDirName", project.ToString());
        Assert.Contains("$(Sts2Path)/mods2", project.ToString());
        Assert.Contains("$(Sts2Path)/$(LocalModsDirName)/", project.ToString());
        Assert.Contains("LegacyModsPath", project.ToString());
        Assert.Contains("MirrorGodotPackToLegacyModsFolder", project.ToString());
        Assert.Contains("packages/**", exportPreset);
        Assert.Contains("tmp/**", exportPreset);
        Assert.Contains("tests/**", exportPreset);
        Assert.Contains("tools/**", exportPreset);
        Assert.Contains("docs/**", exportPreset);
        Assert.Contains("logs/**", exportPreset);
        Assert.Contains("MeiLinMod/newcard/**", exportPreset);
        Assert.Contains("MeiLinModCode/**", exportPreset);
        Assert.Contains("**/~*.TMP", exportPreset);

        var noneRemoves = project.Descendants("None")
            .Select(item => item.Attribute("Remove")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);
        var noneIncludes = project.Descendants("None")
            .Select(item => (Include: item.Attribute("Include")?.Value, Exclude: item.Attribute("Exclude")?.Value))
            .ToArray();

        Assert.Contains("tmp/**", noneRemoves);
        Assert.Contains("tests/**", noneRemoves);
        Assert.Contains("MeiLinMod/newcard/**", noneRemoves);
        Assert.Contains(noneIncludes, item => item.Include == "MeiLinMod/**" && item.Exclude == "MeiLinMod/newcard/**");
    }

    [Fact]
    public async Task Installed_main_pck_excludes_development_and_discarded_assets()
    {
        await InitializeBattle();

        var pckPath = InstalledMainModFile("MeiLinMod.pck");

        Assert.True(File.Exists(pckPath), $"Expected installed pck to exist at {pckPath}.");

        var pckBytes = File.ReadAllBytes(pckPath);

        AssertBinaryContains(pckBytes, "MeiLinMod/vfx_configs/1027/generated/meilin_vfx_commands.json");
        AssertBinaryContains(pckBytes, "MeiLinMod/scenes/meilin_icon.tscn");
        AssertBinaryContains(pckBytes, "GodotScripts/Character/MeilinCharacterAnimBridge.cs");
        AssertBinaryContains(pckBytes, "GodotScripts/Nodes/SpineAutoPlayer.cs");
        AssertBinaryContains(pckBytes, "GodotScripts/StanceVfx/MeiLinAuraBlobEmitter.cs");
        AssertBinaryContains(pckBytes, "GodotScripts/StanceVfx/MeiLinCalmFrostStreakSpawner.cs");
        AssertBinaryContains(pckBytes, "GodotScripts/StanceVfx/MeiLinWrathActivationBurst.cs");
        AssertBinaryContains(pckBytes, "GodotScripts/StanceVfx/MeiLinWrathGlowSparkSpawner.cs");

        foreach (var forbiddenMarker in new[]
                 {
                     "MeiLinMod/newcard",
                     "Theresa",
                     "packages/alchyr.sts2.baselib",
                     "addons/auto_spine_skel_data",
                     "addons/godot_mcp_server",
                     "addons/spine_speed_inspector",
                     ".codex_tmp",
                     "tests/MeiLinMod.Tests",
                     "docs/ritsulib",
                     "tools/RepackFromSavepack.gd",
                     "~libspine_godot",
                     "MeiLinModCode/",
                     "MeiLinModCode/Cards/AttackDefenseUnity.cs",
                     "MeiLinModCode/Powers/EmberPower.cs",
                     "MeiLinModCode/Migration/MeiLinRitsuMigration.cs",
                     "MeiLinModCode/Patches/CardSpinePortraitPatch.cs",
                     "MeiLinModCode/Telemetry/MeiLinTelemetryBootstrap.cs",
                     "MeiLinModCode/Vfx/MeiLinCommandVfxCoordinator.cs",
                     "MeiLinModCode/Entry/MeiLinModEntry.cs",
                     "MeiLinModCode/Entry/GlobalUsings.cs"
                 })
        {
            AssertBinaryDoesNotContain(pckBytes, forbiddenMarker);
        }
    }

    [Fact]
    public async Task Installed_manifest_and_dll_match_the_current_ritsulib_build()
    {
        await InitializeBattle();

        var installedManifestPath = InstalledMainModFile("MeiLinMod.json");
        var installedDllPath = InstalledMainModFile("MeiLinMod.dll");
        var loadedDllPath = typeof(global::MeiLinMod.MainFile).Assembly.Location;

        Assert.True(File.Exists(installedManifestPath), $"Expected installed manifest at {installedManifestPath}.");
        Assert.True(File.Exists(installedDllPath), $"Expected installed dll at {installedDllPath}.");
        Assert.True(File.Exists(loadedDllPath), $"Expected loaded MeiLinMod dll at {loadedDllPath}.");

        Assert.Equal(
            File.ReadAllText(RepoFile("MeiLinMod.json")),
            File.ReadAllText(installedManifestPath));

        Assert.Equal(FileSha256(installedDllPath), FileSha256(loadedDllPath));

        var loadedDllBytes = File.ReadAllBytes(loadedDllPath);
        AssertBinaryContains(loadedDllBytes, "MeiLinRitsuMigration");
        AssertBinaryDoesNotContain(loadedDllBytes, "Alchyr.Sts2.BaseLib");
    }

    [Fact]
    public async Task Spine_extension_keeps_msil_windows_library_aliases_for_export()
    {
        await InitializeBattle();

        var extension = File.ReadAllText(RepoFile("addons", "spine", "spine_godot_extension.gdextension"));

        Assert.Contains("windows.editor.msil = \"windows/libspine_godot.windows.editor.x86_64.dll\"", extension);
        Assert.Contains("windows.debug.msil = \"windows/libspine_godot.windows.template_debug.x86_64.dll\"", extension);
        Assert.Contains("windows.release.msil = \"windows/libspine_godot.windows.template_release.x86_64.dll\"", extension);
    }

    [Fact]
    public async Task Test_project_rewrites_generated_manifests_to_versioned_dependencies()
    {
        await InitializeBattle();

        var project = XDocument.Load(RepoFile("tests", "MeiLinMod.Tests", "MeiLinMod.Tests.csproj"));
        var targetNames = project.Descendants("Target")
            .Select(target => target.Attribute("Name")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);
        var projectText = File.ReadAllText(RepoFile("tests", "MeiLinMod.Tests", "MeiLinMod.Tests.csproj"));

        Assert.Contains("RewriteMeiLinSts2TestManifestWithVersionedDependencies", targetNames);
        Assert.Contains("RewriteMeiLinSts2TestDependencyManifestsWithVersionedDependencies", targetNames);
        Assert.Contains("MirrorMeiLinSts2Mods2DependenciesForTests", targetNames);
        Assert.Contains("&quot;min_version&quot;", projectText);
        Assert.Contains("<Sts2TestDisabledModIds>BaseLib;", projectText);
        Assert.Contains("Sts2RitsuLibWorkshopPath", projectText);
        Assert.Contains("$(Sts2RitsuLibSourcePath)/**/*", projectText);
    }

    [Fact]
    public async Task Main_sources_do_not_reintroduce_old_baselib_or_harmony_patch_entrypoints()
    {
        await InitializeBattle();

        string[] forbiddenTokens =
        [
            "[HarmonyPatch",
            "[HarmonyPrefix",
            "[HarmonyPostfix",
            "[HarmonyTargetMethods",
            "harmony.PatchAll",
            "new Harmony(",
            "BaseLib.Abstracts",
            "Alchyr.Sts2.BaseLib",
            "Alchyr.Sts2.ModAnalyzers",
            "ModConfigRegistry",
            "SimpleModConfig",
            "ICustomModel",
            "ExtraHoverTips",
            "CustomPackedIconPath",
            "CustomBigIconPath"
        ];

        var violations = MainSourceFiles()
            .SelectMany(path =>
            {
                var text = File.ReadAllText(path);
                return forbiddenTokens
                    .Where(token => text.Contains(token, StringComparison.Ordinal))
                    .Select(token => $"{Path.GetRelativePath(RepositoryRoot(), path)} contains {token}");
            })
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    private static IEnumerable<string> PackageReferenceIncludes(XContainer document)
    {
        return document.Descendants("PackageReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))!;
    }

    private static string RepoFile(params string[] segments)
    {
        return Path.Combine(RepositoryRoot(), Path.Combine(segments));
    }

    private static string InstalledMainModFile(string fileName)
    {
        var executablePath = Environment.ProcessPath
                             ?? throw new InvalidOperationException("The STS2 executable path is unavailable.");
        var sts2Path = Path.GetDirectoryName(executablePath)
                       ?? throw new InvalidOperationException($"Could not determine STS2 root from executable path: {executablePath}");

        var legacyMainModPath = Path.Combine(sts2Path, "mods", "MeiLinMod", fileName);
        if (File.Exists(legacyMainModPath))
            return legacyMainModPath;

        return Path.Combine(sts2Path, "mods2", "MeiLinMod", fileName);
    }

    private static void AssertBinaryContains(byte[] haystack, string marker)
    {
        Assert.True(ContainsUtf8Marker(haystack, marker), $"Expected installed pck to contain marker: {marker}");
    }

    private static void AssertBinaryDoesNotContain(byte[] haystack, string marker)
    {
        Assert.False(ContainsUtf8Marker(haystack, marker), $"Installed pck unexpectedly contains marker: {marker}");
    }

    private static bool ContainsUtf8Marker(byte[] haystack, string marker)
    {
        return haystack.AsSpan().IndexOf(Encoding.UTF8.GetBytes(marker)) >= 0;
    }

    private static string FileSha256(string path)
    {
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    }

    private static IEnumerable<string> MainSourceFiles()
    {
        foreach (var path in Directory.EnumerateFiles(RepoFile("MeiLinModCode"), "*.cs", SearchOption.AllDirectories))
            yield return path;

        yield return RepoFile("MeiLinModCode", "Entry", "GlobalUsings.cs");
        yield return RepoFile("MeiLinModCode", "Entry", "MeiLinModEntry.cs");
    }

    private static string RepositoryRoot([CallerFilePath] string sourcePath = "")
    {
        var testDirectory = Path.GetDirectoryName(sourcePath)
                            ?? throw new InvalidOperationException("CallerFilePath did not provide a source directory.");

        return Path.GetFullPath(Path.Combine(testDirectory, "..", ".."));
    }

    private async Task InitializeBattle()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);
    }
}
