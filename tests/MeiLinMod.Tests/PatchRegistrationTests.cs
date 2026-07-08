using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
using MeiLinMod.MeiLinModCode.Patches;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Patching.Models;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class PatchRegistrationTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-patch-registration");
    }

    [Fact]
    public async Task All_ritsu_patch_targets_resolve()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var patchTypes = GetPatchTypes().ToArray();

        Assert.NotEmpty(patchTypes);

        var duplicatePatchIds = patchTypes
            .Select(type => (Type: type, PatchId: GetPatchId(type)))
            .GroupBy(item => item.PatchId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(item => item.Type.FullName))}")
            .ToArray();

        Assert.Empty(duplicatePatchIds);

        var unresolvedTargets = new List<string>();
        var resolvedTargetCount = 0;

        foreach (var patchType in patchTypes)
        {
            var patchId = GetPatchId(patchType);
            var targets = GetTargets(patchType);

            Assert.NotEmpty(targets);

            foreach (var target in targets)
            {
                try
                {
                    Assert.NotNull(PatchTargetMethodResolver.ResolveRequired(target));
                    resolvedTargetCount++;
                }
                catch (Exception ex)
                {
                    unresolvedTargets.Add($"{patchId} -> {target}: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        Assert.Empty(unresolvedTargets);
        Assert.Equal(39, patchTypes.Length);
        Assert.Equal(41, resolvedTargetCount);
    }

    [Fact]
    public async Task All_ritsu_patch_methods_are_registered_by_migration()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var patchTypeNames = GetPatchTypes()
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var registeredPatchTypeNames = Regex
            .Matches(File.ReadAllText(RepoFile("MeiLinModCode", "Migration", "MeiLinRitsuMigration.cs")), @"RegisterPatch<(?<type>[^>]+)>")
            .Select(match => match.Groups["type"].Value.Trim().Split('.').Last())
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(patchTypeNames, registeredPatchTypeNames);
    }

    [Fact]
    public async Task Ritsu_patch_methods_stay_in_expected_optional_patcher_groups()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var groups = RegisteredPatchGroupsByPatcher().ToDictionary(
            group => group.PatcherId,
            group => group,
            StringComparer.Ordinal);

        Assert.Equal(
            [
                "optional-audio",
                "optional-card-visual",
                "optional-combat-animation",
                "optional-content",
                "optional-overlay",
                "optional-scene",
                "optional-ui"
            ],
            groups.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray());

        AssertPatchGroup(
            groups,
            "optional-ui",
            "optional UI",
            [
                "StatsScreenMeiLinPatch",
                "YukiSettingsPanelEmptyReadyCompatPatch"
            ]);
        AssertPatchGroup(groups, "optional-audio", "optional audio", ["SfxCmdMeiLinAudioPatch"]);
        AssertPatchGroup(
            groups,
            "optional-overlay",
            "optional battle ready overlay",
            [
                "MeiLinBattleReadyAfterCombatVictoryPatch",
                "MeiLinBattleReadyAfterDeathPatch",
                "MeiLinBattleReadyBeforeCardPlayedPatch",
                "MeiLinBattleReadyBeforeCombatStartPatch",
                "MeiLinBattleReadyCancelPlayCardPatch",
                "MeiLinBattleReadyControllerCardPlayStartPatch",
                "MeiLinBattleReadyHandFocusPatch",
                "MeiLinBattleReadyHandHoverEffectsPatch",
                "MeiLinBattleReadyHandMousePressedPatch",
                "MeiLinBattleReadyHandUnfocusPatch",
                "MeiLinBattleReadyMouseCardPlayStartPatch"
            ]);
        AssertPatchGroup(
            groups,
            "optional-combat-animation",
            "optional combat animation",
            [
                "MeiLinBattleAnimationGenerateAnimatorPatch",
                "MeiLinBattleAnimationSetAnimationPostfixPatch",
                "MeiLinBattleAnimationSetAnimationPrefixPatch",
                "MeiLinTriggerAnimPatch"
            ]);
        AssertPatchGroup(
            groups,
            "optional-scene",
            "optional scene",
            [
                "GameOverAnimationFallbackOnMegaStatePatch",
                "MerchantCharacterAnimationFallbackPatch",
                "MerchantCharacterPlayAnimationFallbackPatch",
                "RestSiteCharacterAnimationFallbackPatch"
            ]);
        AssertPatchGroup(
            groups,
            "optional-card-visual",
            "optional card visuals",
            [
                "CardCustomAncientFrameEnterTreePatch",
                "CardCustomAncientFrameFreedToPoolPatch",
                "CardCustomAncientFrameReadyPatch",
                "CardCustomAncientFrameReloadPatch",
                "CardCustomAncientFrameUpdateVisualsPatch",
                "CardSpinePortraitEnterTreePatch",
                "CardSpinePortraitReloadPatch",
                "CardSpinePortraitUpdateVisualsPatch"
            ]);
        AssertPatchGroup(
            groups,
            "optional-content",
            "optional content",
            [
                "ArchaicToothAfterObtainedMeiLinPatch",
                "ArchaicToothSetupForPlayerMeiLinPatch",
                "ColorfulPhilosophersMeiLinPatch",
                "DustyTomeAfterObtainedMeiLinPatch",
                "DustyTomeSetupForPlayerMeiLinPatch",
                "HuoYongYuXiaCiAfterCombatPatch",
                "OrobasSeaGlassMeiLinPatch",
                "PrismaticGemMeiLinPatch",
                "TouchOfOrobasMeiLinPatch"
            ]);

        var registeredPatchTypes = groups.Values
            .SelectMany(group => group.PatchTypes)
            .OrderBy(type => type, StringComparer.Ordinal)
            .ToArray();
        var allPatchTypes = GetPatchTypes()
            .Select(type => type.Name)
            .OrderBy(type => type, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(allPatchTypes, registeredPatchTypes);
    }

    [Fact]
    public async Task Audio_patch_does_not_hook_monster_death_sfx()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);

        var targetDescriptions = SfxCmdMeiLinAudioPatch
            .GetTargets()
            .Select(target => target.ToString())
            .ToArray();

        Assert.Equal(3, targetDescriptions.Length);
        Assert.Contains("SfxCmd.Play(String, Single)", targetDescriptions);
        Assert.Contains("SfxCmd.Play(String, String, Single, Single)", targetDescriptions);
        Assert.Contains("SfxCmd.PlayDeath(Player)", targetDescriptions);
        Assert.DoesNotContain(targetDescriptions, target => target.Contains(nameof(SfxCmd.PlayDeath), StringComparison.Ordinal) && target.Contains("Monster", StringComparison.Ordinal));
    }

    private static IEnumerable<Type> GetPatchTypes()
    {
        return typeof(MainFile)
            .Assembly
            .GetTypes()
            .Where(type =>
                typeof(IPatchMethod).IsAssignableFrom(type) &&
                type is { IsAbstract: false, IsInterface: false })
            .OrderBy(type => type.FullName, StringComparer.Ordinal);
    }

    private static string GetPatchId(Type patchType)
    {
        var property = patchType.GetProperty(
            nameof(IPatchMethod.PatchId),
            BindingFlags.Public | BindingFlags.Static);

        var value = property?.GetValue(null) as string;
        Assert.False(string.IsNullOrWhiteSpace(value), $"{patchType.FullName} does not expose a non-empty PatchId.");
        return value;
    }

    private static ModPatchTarget[] GetTargets(Type patchType)
    {
        var method = patchType.GetMethod(
            nameof(IPatchMethod.GetTargets),
            BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);
        return method.Invoke(null, null) as ModPatchTarget[]
               ?? throw new InvalidOperationException($"{patchType.FullName}.GetTargets() did not return ModPatchTarget[].");
    }

    private static IEnumerable<RegisteredPatchGroup> RegisteredPatchGroupsByPatcher()
    {
        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Migration", "MeiLinRitsuMigration.cs"));
        var groupMatches = Regex.Matches(
            source,
            @"var (?<var>\w+) = RitsuLibFramework\.CreatePatcher\(MainFile\.ModId, ""(?<id>[^""]+)"", ""(?<description>[^""]+)""\);\s*(?<body>.*?)\s*\k<var>\.PatchAll\(\);",
            RegexOptions.Singleline);

        foreach (Match match in groupMatches)
        {
            var patchTypes = Regex.Matches(match.Groups["body"].Value, @"RegisterPatch<(?<type>[^>]+)>")
                .Select(patchMatch => patchMatch.Groups["type"].Value.Trim().Split('.').Last())
                .OrderBy(type => type, StringComparer.Ordinal)
                .ToArray();

            yield return new RegisteredPatchGroup(
                match.Groups["id"].Value,
                match.Groups["description"].Value,
                patchTypes);
        }
    }

    private static void AssertPatchGroup(
        IReadOnlyDictionary<string, RegisteredPatchGroup> groups,
        string patcherId,
        string description,
        string[] patchTypes)
    {
        Assert.True(groups.TryGetValue(patcherId, out var group), $"Missing patcher group: {patcherId}");
        Assert.Equal(description, group.Description);
        Assert.Equal(
            patchTypes.OrderBy(type => type, StringComparer.Ordinal).ToArray(),
            group.PatchTypes);
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

    private sealed record RegisteredPatchGroup(string PatcherId, string Description, string[] PatchTypes);
}
