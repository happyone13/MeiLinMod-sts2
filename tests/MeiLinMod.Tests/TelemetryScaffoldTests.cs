using System.IO;
using System.Reflection;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using STS2RitsuLib.Telemetry;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class TelemetryScaffoldTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-telemetry-scaffold");
    }

    [Fact]
    public async Task Telemetry_is_registered_for_posthog_run_history()
    {
        await InitializeBattle();

        Assert.Contains(
            TelemetryRegistry.GetApplicants(),
            applicant => applicant.ApplicantId == "MeiLinMod" && applicant.OwnerModId == "MeiLinMod");

        var applicant = CreateApplicantForInspection();
        var requestedCategories = applicant.Requests.Select(request => request.Category).ToHashSet();
        var requestIds = applicant.Requests.Select(request => request.RequestId).ToHashSet(StringComparer.Ordinal);

        Assert.Equal("MeiLinMod", applicant.ApplicantId);
        Assert.Equal("MeiLinMod", applicant.OwnerModId);
        Assert.Equal("MeiLinMod", applicant.DisplayName);
        var postHogAdapter = Assert.IsType<PostHogTelemetryAdapter>(applicant.Adapter);
        Assert.Equal("https://us.i.posthog.com/", postHogAdapter.Host.ToString());
        Assert.Equal("posthog", postHogAdapter.AdapterId);
        Assert.Equal(4, applicant.Requests.Count);
        Assert.Contains(TelemetryDataCategory.BasicUsage, requestedCategories);
        Assert.Contains(TelemetryDataCategory.ModInventory, requestedCategories);
        Assert.Contains(TelemetryDataCategory.Diagnostics, requestedCategories);
        Assert.Contains(TelemetryDataCategory.RunHistory, requestedCategories);
        Assert.DoesNotContain(TelemetryDataCategory.Custom, requestedCategories);
        Assert.Contains("run_history", requestIds);
        Assert.DoesNotContain("meilin_balance", requestIds);

        var entrySource = File.ReadAllText(RepoFile("MeiLinModCode", "Entry", "MeiLinModEntry.cs"));
        var bootstrapSource = File.ReadAllText(RepoFile("MeiLinModCode", "Telemetry", "MeiLinTelemetryBootstrap.cs"));
        var configurationSource = File.ReadAllText(RepoFile("MeiLinModCode", "Telemetry", "MeiLinTelemetryConfiguration.cs"));
        var overviewSource = File.ReadAllText(RepoFile("docs", "project-overview.zh-CN.md"));
        var migrationPlanSource = File.ReadAllText(RepoFile("docs", "ritsulib-migration-plan.zh-CN.md"));

        Assert.Contains("MeiLinTelemetryBootstrap.Initialize();", entrySource);

        Assert.Contains("internal static class MeiLinTelemetryBootstrap", bootstrapSource);
        Assert.Contains("private static bool _initialized;", bootstrapSource);
        Assert.Contains("TelemetryRegistry.RegisterApplicant(CreateApplicant());", bootstrapSource);
        Assert.Contains("internal static TelemetryApplicant CreateApplicant()", bootstrapSource);
        Assert.Contains("ApplicantId = MeiLinTelemetryConfiguration.ApplicantId", bootstrapSource);
        Assert.Contains("OwnerModId = MainFile.ModId", bootstrapSource);
        Assert.Contains("DisplayName = MeiLinTelemetryConfiguration.DisplayName", bootstrapSource);
        Assert.Contains("Adapter = MeiLinTelemetryConfiguration.CreateAdapter()", bootstrapSource);
        Assert.Contains("TelemetryRequest.BasicUsage", bootstrapSource);
        Assert.Contains("TelemetryRequest.ModInventory", bootstrapSource);
        Assert.Contains("TelemetryRequest.Diagnostics", bootstrapSource);
        Assert.Contains("TelemetryRequest.RunHistory", bootstrapSource);
        Assert.Contains("captureFilter: IsMeiLinRun", bootstrapSource);
        Assert.Contains("internal static bool IsMeiLinRun(RunEndedEvent evt)", bootstrapSource);
        Assert.DoesNotContain("TelemetryRequest.Custom", bootstrapSource);
        Assert.DoesNotContain("MeiLinTelemetryRunSummary", bootstrapSource);
        Assert.DoesNotContain("meilin_balance", bootstrapSource);

        Assert.Contains("internal const string ApplicantId = MainFile.ModId;", configurationSource);
        Assert.Contains("internal const string DisplayName = \"MeiLinMod\";", configurationSource);
        Assert.Contains("internal const string BackendReference = \"PostHog\";", configurationSource);
        Assert.Contains("internal const string PostHogHost = \"https://us.i.posthog.com\";", configurationSource);
        Assert.Contains("internal const string IngestEndpoint = PostHogHost + \"/batch\";", configurationSource);
        Assert.Contains("internal const string EventStorageShape = \"PostHog event properties plus RitsuLib payload JSON\";", configurationSource);
        Assert.Contains("internal static ITelemetryAdapter CreateAdapter()", configurationSource);
        Assert.Contains("new PostHogTelemetryAdapter(PostHogHost, PostHogProjectApiKey)", configurationSource);
        Assert.DoesNotContain("HttpJsonTelemetryAdapter", configurationSource);
        Assert.DoesNotContain("DisabledTelemetryAdapter", configurationSource);

        Assert.Contains("STSVWB", overviewSource);
        Assert.Contains("STSVLogs", overviewSource);
        Assert.Contains("TelemetryRegistry.RegisterApplicant", overviewSource + migrationPlanSource);
        Assert.Contains("PostHog", overviewSource + migrationPlanSource);
        Assert.Contains("RunHistory", overviewSource + migrationPlanSource);
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

    private async Task InitializeBattle()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);
    }

    private static TelemetryApplicant CreateApplicantForInspection()
    {
        var bootstrapType = typeof(global::MeiLinMod.MainFile).Assembly.GetType(
                                "MeiLinMod.MeiLinModCode.Telemetry.MeiLinTelemetryBootstrap",
                                throwOnError: true)
                            ?? throw new InvalidOperationException("Telemetry bootstrap type was not found.");

        var createApplicant = bootstrapType.GetMethod(
                                  "CreateApplicant",
                                  BindingFlags.Static | BindingFlags.NonPublic)
                              ?? throw new MissingMethodException(bootstrapType.FullName, "CreateApplicant");

        return Assert.IsType<TelemetryApplicant>(createApplicant.Invoke(null, null));
    }
}
