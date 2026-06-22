using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeiLinMod.MeiLinModCode.Vfx;

public static class MeiLinTimelineCameraShake
{
    private static readonly (float Time, float X, float Y)[] Keyframes =
    [
        (0.000f, 0f, 0f),
        (0.030f, 12f, 0f),
        (0.060f, -9f, 1f),
        (0.095f, 6f, -1f),
        (0.130f, -3f, 0f),
        (0.170f, 0f, 0f)
    ];

    public static async Task PlayAsync(NCombatRoom room)
    {
        var target = GetShakeTarget(room);
        if (target == null || !GodotObject.IsInstanceValid(target))
            return;

        var originalPosition = target.Get("position").AsVector2();

        try
        {
            using var tween = target.CreateTween();
            if (tween == null)
                return;

            for (int i = 1; i < Keyframes.Length; i++)
            {
                var previous = Keyframes[i - 1];
                var current = Keyframes[i];
                var duration = MathF.Max(0f, current.Time - previous.Time);

                tween.TweenProperty(
                        target,
                        "position",
                        originalPosition + new Vector2(current.X, current.Y),
                        duration)
                    .SetTrans(Tween.TransitionType.Sine)
                    .SetEase(Tween.EaseType.InOut)
                    .SetDelay(previous.Time);
            }

            await target.ToSignal(tween, Tween.SignalName.Finished);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinCameraShake] Failed: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(target))
                target.Set("position", originalPosition);
        }
    }

    private static Node? GetShakeTarget(NCombatRoom room)
    {
        try
        {
            var container = room.GetNodeOrNull("%CombatSceneContainer");
            if (container != null && GodotObject.IsInstanceValid(container))
                return container;
        }
        catch
        {
        }

        return room.CombatVfxContainer;
    }
}
