using System.Reflection;
using Godot;
using Godot.Bridge;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace MeiLinMod.MeiLinModCode.Character;

[GlobalClass]
[ScriptPath("res://GodotScripts/Character/MeiLinRestSiteCharacter.cs")]
public partial class MeiLinRestSiteCharacter : NRestSiteCharacter
{
    private static readonly FieldInfo ControlRootField =
        typeof(NRestSiteCharacter).GetField("_controlRoot", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo HitboxField =
        typeof(NRestSiteCharacter).GetField("<Hitbox>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo SelectionReticleField =
        typeof(NRestSiteCharacter).GetField("_selectionReticle", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo LeftThoughtAnchorField =
        typeof(NRestSiteCharacter).GetField("_leftThoughtAnchor", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo RightThoughtAnchorField =
        typeof(NRestSiteCharacter).GetField("_rightThoughtAnchor", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public override void _Ready()
    {
        ControlRootField.SetValue(this, GetNode<Control>("ControlRoot"));
        HitboxField.SetValue(this, GetNode<Control>("%Hitbox"));
        SelectionReticleField.SetValue(this, GetNode<NSelectionReticle>("%SelectionReticle"));
        LeftThoughtAnchorField.SetValue(this, GetNode<Control>("%ThoughtBubbleLeft"));
        RightThoughtAnchorField.SetValue(this, GetNode<Control>("%ThoughtBubbleRight"));

        foreach (Node2D spineNode in GetChildSpineNodes())
        {
            MegaSprite sprite = new(spineNode);
            this.RunWhenSpineReady(sprite, animState =>
            {
                if (!TrySetFirstAvailable(animState, sprite, "camping", "idle", "b_idle"))
                    MainFile.Logger.Warn("[MeiLinRestSiteCharacter] No rest-site fallback animation found.");
            });
        }

        Control hitbox = GetNode<Control>("%Hitbox");
        hitbox.Connect(Control.SignalName.FocusEntered, Callable.From(OnFocus));
        hitbox.Connect(Control.SignalName.FocusExited, Callable.From(OnUnfocus));
        hitbox.Connect(Control.SignalName.MouseEntered, Callable.From(OnFocus));
        hitbox.Connect(Control.SignalName.MouseExited, Callable.From(OnUnfocus));
    }

    private void OnFocus()
    {
        if (!NTargetManager.Instance.IsInSelection || !NTargetManager.Instance.AllowedToTargetNode(this))
            return;

        NTargetManager.Instance.OnNodeHovered(this);
        GetNode<NSelectionReticle>("%SelectionReticle").OnSelect();
        NRun.Instance?.GlobalUi.MultiplayerPlayerContainer.HighlightPlayer(Player);
    }

    private void OnUnfocus()
    {
        if (NTargetManager.Instance.IsInSelection && NTargetManager.Instance.AllowedToTargetNode(this))
            NTargetManager.Instance.OnNodeUnhovered(this);

        Deselect();
    }

    private IEnumerable<Node2D> GetChildSpineNodes()
    {
        foreach (Node child in GetChildren())
        {
            if (child is Node2D node2D && node2D.GetClass() == "SpineSprite")
                yield return node2D;
        }
    }

    private static bool TrySetFirstAvailable(MegaAnimationState animState, MegaSprite sprite, params string[] animations)
    {
        foreach (string animation in animations)
        {
            try
            {
                if (!sprite.HasAnimation(animation))
                    continue;

                animState.SetAnimation(animation, loop: true);
                return true;
            }
            catch
            {
            }
        }

        return false;
    }
}
