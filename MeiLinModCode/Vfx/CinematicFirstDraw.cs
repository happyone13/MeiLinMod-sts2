using Godot;

namespace MeiLinMod.MeiLinModCode.Vfx;

internal static class CinematicFirstDraw
{
    internal static async Task<bool> PrepareAsync(Node stage, Func<bool> isCurrent)
    {
        try
        {
            if (!Current())
                return false;
            stage.Call("prepare_first_draw");
            for (int i = 0; i < 2; i++)
            {
                if (DisplayServer.GetName() == "headless")
                    await stage.ToSignal(stage.GetTree(), SceneTree.SignalName.ProcessFrame);
                else
                    await stage.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (!Current())
                    return false;
            }
            return true;
        }
        finally
        {
            if (GodotObject.IsInstanceValid(stage) && !stage.IsQueuedForDeletion())
                stage.Call("reset_first_draw");
        }

        bool Current() => GodotObject.IsInstanceValid(stage) &&
            !stage.IsQueuedForDeletion() && stage.IsInsideTree() && isCurrent();
    }
}
