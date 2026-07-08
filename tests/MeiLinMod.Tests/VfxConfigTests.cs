using System.Runtime.CompilerServices;
using System.Text.Json;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Vfx;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class VfxConfigTests : CombatTestSuite
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-vfx-config");
    }

    [Fact]
    public async Task Vfx_command_config_contains_basic_meilin_skill_flows()
    {
        await InitializeBattle();

        var config = LoadVfxConfig();

        Assert.Contains("u1_buff", config.CommandSets.Keys);
        Assert.Contains("u2_attack", config.CommandSets.Keys);
        Assert.Contains("u3_buff", config.CommandSets.Keys);
        Assert.Contains("u4_buff", config.CommandSets.Keys);

        Assert.Contains("attack_play1", config.Commands.Keys);
        Assert.Contains("attack_play2", config.Commands.Keys);
        Assert.Contains("attack_end", config.Commands.Keys);
        Assert.Contains("u2_attack_play", config.Commands.Keys);
        Assert.Contains("u2_attack_end", config.Commands.Keys);
        Assert.Contains("u1_buff_ready", config.Commands.Keys);
        Assert.Contains("u1_buff_play", config.Commands.Keys);
        Assert.Contains("u3_buff_ready", config.Commands.Keys);
        Assert.Contains("u3_buff_play", config.Commands.Keys);
        Assert.Contains("u4_buff_ready", config.Commands.Keys);
        Assert.Contains("u4_buff_play", config.Commands.Keys);
        Assert.Contains("debuff_ready", config.Commands.Keys);
        Assert.Contains("debuff_play", config.Commands.Keys);
    }

    [Fact]
    public async Task Vfx_command_sets_reference_existing_required_commands()
    {
        await InitializeBattle();

        var config = LoadVfxConfig();

        foreach (var (setName, commandSet) in config.CommandSets)
        {
            if (string.IsNullOrWhiteSpace(setName))
                continue;

            AssertCommandExists(config, setName, "ready", commandSet.Ready);
            AssertCommandExists(config, setName, "play_ready", commandSet.PlayReady);
            AssertCommandExists(config, setName, "play", commandSet.Play);

            if (!string.IsNullOrWhiteSpace(commandSet.End) && !config.Commands.ContainsKey(commandSet.End))
                Assert.Contains((setName, commandSet.End), OptionalMissingEndCommands);
        }
    }

    [Fact]
    public async Task Attack_sequence_vfx_commands_have_runtime_durations()
    {
        await InitializeBattle();

        var attackCommands = MeiLinBattleAnimationService.BuildAttackCommands(5);

        foreach (var commandName in attackCommands.Append("u2_attack_end"))
        {
            Assert.True(MeiLinCommandVfxCoordinator.TryGetCommand(commandName, out _), $"Missing VFX command: {commandName}");
            Assert.True(MeiLinCommandVfxCoordinator.GetCommandDurationSeconds(commandName) > 0f, $"VFX command has no duration: {commandName}");
        }
    }

    private static void AssertCommandExists(
        MeiLinCommandVfxConfig config,
        string setName,
        string phaseName,
        string? commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
            return;

        Assert.True(
            config.Commands.ContainsKey(commandName),
            $"Command set '{setName}' references missing {phaseName} command '{commandName}'.");
    }

    private static readonly HashSet<(string SetName, string CommandName)> OptionalMissingEndCommands =
    [
        ("u1_buff", "u1_buff_end"),
        ("u3_buff", "u3_buff_end"),
        ("u4_buff", "u4_buff_end")
    ];

    private static MeiLinCommandVfxConfig LoadVfxConfig()
    {
        var configPath = RepoFile("MeiLinMod", "vfx_configs", "1027", "generated", "meilin_vfx_commands.json");
        var configJson = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<MeiLinCommandVfxConfig>(configJson, JsonOptions);

        Assert.NotNull(config);
        return config;
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

    private async Task InitializeBattle()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);
    }
}
