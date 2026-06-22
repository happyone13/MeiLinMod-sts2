using Godot;

namespace MeiLinMod.MeiLinModCode.Vfx;

public static class MeiLinTimelineHitstop
{
    private static bool _isPlaying;

    private static readonly (float Duration, float TimeScale)[] Phases =
    [
        (0.055f, 0.05f),
        (0.045f, 0.35f),
        (0.060f, 0.7f)
    ];

    public static async Task PlayAsync(SceneTree tree)
    {
        if (_isPlaying)
            return;

        _isPlaying = true;
        var originalTimeScale = Engine.TimeScale;
        try
        {
            MainFile.Logger.Info($"[MeiLinHitstop] Start. originalTimeScale={originalTimeScale:0.###}");
            foreach (var (duration, timeScale) in Phases)
            {
                Engine.TimeScale = timeScale;
                var timer = tree.CreateTimer(duration, processAlways: true, ignoreTimeScale: true);
                await tree.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinHitstop] Failed: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            Engine.TimeScale = originalTimeScale > 0f ? originalTimeScale : 1f;
            _isPlaying = false;
            MainFile.Logger.Info($"[MeiLinHitstop] End. restoredTimeScale={Engine.TimeScale:0.###}");
        }
    }
}
