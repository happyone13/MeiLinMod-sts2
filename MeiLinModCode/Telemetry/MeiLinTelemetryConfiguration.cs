using STS2RitsuLib.Telemetry;

namespace MeiLinMod.MeiLinModCode.Telemetry;

internal static class MeiLinTelemetryConfiguration
{
    internal const string ApplicantId = MainFile.ModId;
    internal const string DisplayName = "MeiLinMod";
    internal const string BackendReference = "PostHog";
    internal const string PostHogHost = "https://us.i.posthog.com";
    internal const string PostHogProjectApiKey = "phc_o5iGnWUGF5vbLrsh8X8MaDrPzjQMWLrFZmQpcrx2q25k";
    internal const string IngestEndpoint = PostHogHost + "/batch";
    internal const string EventStorageShape = "PostHog event properties plus RitsuLib payload JSON";

    internal static ITelemetryAdapter CreateAdapter()
    {
        return new PostHogTelemetryAdapter(PostHogHost, PostHogProjectApiKey);
    }
}
