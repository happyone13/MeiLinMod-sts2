using System.Runtime.CompilerServices;
using Godot;

namespace MeiLinMod.MeiLinModCode.Patches;

internal static class DynamicPortraitActivity
{
    private sealed class State
    {
        public Node.ProcessModeEnum ProcessMode;
        public bool Paused;
    }

    private static readonly ConditionalWeakTable<SubViewport, State> States = new();

    public static bool ShouldRender(Control container)
    {
        if (!container.IsInsideTree() || !container.IsVisibleInTree())
            return false;

        Rect2 bounds = container.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, container.Size);
        if (bounds.HasArea() && !bounds.Intersects(container.GetViewport().GetVisibleRect()))
            return false;

        for (Node? current = container; current != null; current = current.GetParent())
        {
            if (current is CanvasItem canvas && canvas.Modulate.A <= 0.001f)
                return false;

            if (current != container && current is Control { ClipContents: true } clip && bounds.HasArea())
            {
                Rect2 clipBounds = clip.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, clip.Size);
                if (!bounds.Intersects(clipBounds))
                    return false;
                bounds = bounds.Intersection(clipBounds);
            }
        }

        return true;
    }

    public static void SetActive(SubViewport viewport, bool active)
    {
        State state = States.GetValue(viewport, node => new State { ProcessMode = node.ProcessMode });
        if (active)
        {
            if (state.Paused)
            {
                viewport.ProcessMode = state.ProcessMode;
                state.Paused = false;
            }
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        }
        else
        {
            if (!state.Paused)
            {
                state.ProcessMode = viewport.ProcessMode;
                state.Paused = true;
            }
            viewport.ProcessMode = Node.ProcessModeEnum.Disabled;
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        }
    }
}
