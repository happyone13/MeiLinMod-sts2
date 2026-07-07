using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Patches;
using MeiLinMod.MeiLinModCode.Services;
using STS2RitsuLib.Patching.Models;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class OptionalAudioPatchTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-optional-audio-patches");
    }

    [Fact]
    public async Task Audio_patch_stays_non_critical_with_expected_targets()
    {
        await InitializeBattle();

        Assert.False(SfxCmdMeiLinAudioPatch.IsCritical);
        Assert.Equal("MeiLinMod.Audio.SfxCmd", SfxCmdMeiLinAudioPatch.PatchId);
        Assert.Equal("Redirect MeiLin SfxCmd voice events", SfxCmdMeiLinAudioPatch.Description);

        var targetDescriptions = SfxCmdMeiLinAudioPatch
            .GetTargets()
            .Select(target => target.ToString())
            .OrderBy(target => target, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "SfxCmd.Play(String, Single)",
                "SfxCmd.Play(String, String, Single, Single)",
                "SfxCmd.PlayDeath(Player)"
            ],
            targetDescriptions);
        Assert.DoesNotContain(targetDescriptions, target => target.Contains(nameof(SfxCmd.PlayDeath), StringComparison.Ordinal) && target.Contains("Monster", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Audio_service_resolves_only_meilin_sfx_keys_and_custom_card_clips()
    {
        await InitializeBattle();

        Assert.True(TryResolvePath("meilin_attack", out var attackPath));
        Assert.True(TryResolvePath("meilin_cast", out var castPath));
        Assert.True(TryResolvePath("meilin_die", out var diePath));
        Assert.True(TryResolvePath("meilin_select", out var selectPath));
        Assert.False(TryResolvePath("slime_attack", out _));
        Assert.False(TryResolvePath("event:/char/slime_attack", out _));

        Assert.StartsWith("res://MeiLinMod/sound/meilin_attack", attackPath, StringComparison.Ordinal);
        Assert.StartsWith("res://MeiLinMod/sound/meilin_cast", castPath, StringComparison.Ordinal);
        Assert.Equal("res://MeiLinMod/sound/meilin_die.mp3", diePath);
        Assert.Equal("res://MeiLinMod/sound/meilin_select.mp3", selectPath);

        foreach (var resourcePath in EnumerateAudioResourcePaths())
            AssertResourcePathExists(resourcePath);
    }

    [Fact]
    public async Task Default_sfx_suppression_is_meilin_scoped_and_single_use()
    {
        await InitializeBattle();

        MeiLinAudioService.SuppressNextDefaultAttackSfx(null);
        MeiLinAudioService.SuppressNextDefaultCastSfx(null);

        Assert.False(MeiLinAudioService.ShouldSuppressDefaultSfx("meilin_attack"));
        Assert.False(MeiLinAudioService.ShouldSuppressDefaultSfx("meilin_cast"));
        Assert.False(MeiLinAudioService.ShouldSuppressDefaultSfx("slime_attack"));
    }

    [Fact]
    public async Task Audio_patch_source_keeps_player_death_scope_and_fmod_guard()
    {
        await InitializeBattle();

        var patchSource = File.ReadAllText(RepoFile("MeiLinModCode", "Patches", "SfxCmdMeiLinAudioPatch.cs"));
        var serviceSource = File.ReadAllText(RepoFile("MeiLinModCode", "Services", "MeiLinAudioService.cs"));

        Assert.Contains("PatchTarget.Method(typeof(SfxCmd), nameof(SfxCmd.PlayDeath), typeof(Player))", patchSource);
        Assert.DoesNotContain("typeof(Monster)", patchSource, StringComparison.Ordinal);
        Assert.Contains("if (!IsMeiLinPlayer(player))", serviceSource);
        Assert.Contains("lower.StartsWith(prefix, StringComparison.Ordinal) && !lower.Contains(\"meilin\")", serviceSource);
        Assert.Contains("key == \"meilin_attack\"", serviceSource);
        Assert.Contains("key == \"meilin_cast\"", serviceSource);
        Assert.Contains("private const float MeiLinVoiceGain = 2f;", serviceSource);
        Assert.Contains("MeiLinSharedSettings.VoiceVolume * MeiLinVoiceGain", serviceSource);
    }

    private async Task InitializeBattle()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);
    }

    private static bool TryResolvePath(string key, out string path)
    {
        var method = typeof(MeiLinAudioService).GetMethod("TryResolvePath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        object?[] args = [key, null];
        var result = Assert.IsType<bool>(method.Invoke(null, args));
        path = args[1] as string ?? string.Empty;
        return result;
    }

    private static IEnumerable<string> EnumerateAudioResourcePaths()
    {
        var source = File.ReadAllText(RepoFile("MeiLinModCode", "Services", "MeiLinAudioService.cs"));
        return System.Text.RegularExpressions.Regex
            .Matches(source, @"""(?<path>res://MeiLinMod/sound/[^""]+\.mp3)""")
            .Select(match => match.Groups["path"].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal);
    }

    private static void AssertResourcePathExists(string resourcePath)
    {
        Assert.StartsWith("res://", resourcePath, StringComparison.Ordinal);
        Assert.True(
            File.Exists(RepoFile(resourcePath["res://".Length..].Split('/'))),
            $"Resource path points to a missing file: {resourcePath}");
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
