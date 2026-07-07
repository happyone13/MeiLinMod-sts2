using System.Collections.Generic;
using Godot;

namespace MeiLinMod.MeiLinModCode.Character;

[GlobalClass]
[ScriptPath("res://GodotScripts/Character/MeilinCharacterAnimBridge.cs")]
public partial class MeilinCharacterAnimBridge : Node2D
{
    [Export] public bool CampMode { get; set; } = false;

    private const string IdleState = "idle";
    private const string DeadState = "dead";
    private const string CampingState = "camping";

    private sealed record SpineMapping(string SpineAnimation, bool Loop);
    private sealed record TriggerSpec(string CanonicalState, bool Loop);

    private static readonly Dictionary<string, SpineMapping> CanonicalStateToSpine = new()
    {
        [IdleState] = new("idle", true),
        ["attack"] = new("attack_play1", false),
        ["cast"] = new("buff_play", false),
        ["hit"] = new("hit", false),
        [DeadState] = new("death", false),
        ["relaxed"] = new("idle", true),
        [CampingState] = new("camping", true)
    };

    private static readonly Dictionary<string, TriggerSpec> TriggerToSpec = new()
    {
        // Engine trigger casing from CreatureCmd / CharacterModel
        ["idle"] = new(IdleState, true),
        ["idle_loop"] = new(IdleState, true),
        ["idleloop"] = new(IdleState, true),
        ["attack"] = new("attack", false),
        ["attack_loop"] = new("attack", true),
        ["attackloop"] = new("attack", true),
        ["cast"] = new("cast", false),
        ["cast_loop"] = new("cast", true),
        ["castloop"] = new("cast", true),
        ["hit"] = new("hit", false),
        ["hurt"] = new("hit", false),
        ["hit_loop"] = new("hit", true),
        ["hitloop"] = new("hit", true),
        ["dead"] = new(DeadState, false),
        ["death"] = new(DeadState, false),
        ["die"] = new(DeadState, false),
        ["dead_loop"] = new(DeadState, true),
        ["deadloop"] = new(DeadState, true),
        ["relaxed"] = new("relaxed", true),
        ["relaxed_loop"] = new("relaxed", true),
        ["relaxedloop"] = new("relaxed", true),
        ["camping"] = new(CampingState, true)
    };

    private Node _visuals = null!;
    private AnimationPlayer _animationPlayer = null!;
    private string _lastState = "";

    public override void _Ready()
    {
        _visuals = GetNode<Node>("%Visuals");
        _animationPlayer = EnsureAnimationPlayer();
        EnsureStateAnimations();

        string initialState = CampMode ? CampingState : "Idle";
        _animationPlayer.Play(initialState);
        ApplyState(initialState);
    }

    public override void _Process(double delta)
    {
        string current = _animationPlayer.CurrentAnimation;
        if (!string.IsNullOrEmpty(current))
        {
            ApplyState(current);
        }
    }

    private AnimationPlayer EnsureAnimationPlayer()
    {
        AnimationPlayer? player = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        if (player != null)
        {
            return player;
        }

        player = new AnimationPlayer { Name = "AnimationPlayer" };
        AddChild(player);
        return player;
    }

    private void ApplyState(string state)
    {
        string normalized = NormalizeState(state);
        if (_lastState == normalized)
        {
            return;
        }

        TriggerSpec spec = TriggerToSpec.TryGetValue(normalized, out TriggerSpec? mappedSpec)
            ? mappedSpec
            : TriggerToSpec[IdleState];
        SpineMapping mapping = CanonicalStateToSpine.TryGetValue(spec.CanonicalState, out SpineMapping? mappedState)
            ? mappedState
            : CanonicalStateToSpine[IdleState];

        _lastState = normalized;

        GodotObject? animationState = GetAnimationState();
        if (animationState == null)
        {
            return;
        }

        if (!TryCall(animationState, "set_animation", mapping.SpineAnimation, mapping.Loop, 0))
        {
            TryCall(animationState, "SetAnimation", mapping.SpineAnimation, mapping.Loop, 0);
        }

        // Match base-game CreatureAnimator behavior: one-shots return to idle.
        if (!spec.Loop && spec.CanonicalState != DeadState)
        {
            string idleSpine = CampMode ? CanonicalStateToSpine[CampingState].SpineAnimation : CanonicalStateToSpine[IdleState].SpineAnimation;
            if (!TryCall(animationState, "add_animation", idleSpine, 0f, true, 0))
            {
                TryCall(animationState, "AddAnimation", idleSpine, 0f, true, 0);
            }
        }
    }

    private static string NormalizeState(string state)
    {
        return state.Trim().ToLowerInvariant().Replace("-", "_");
    }

    private void EnsureStateAnimations()
    {
        AnimationLibrary? library = _animationPlayer.GetAnimationLibrary("");
        if (library == null)
        {
            library = new AnimationLibrary();
            _animationPlayer.AddAnimationLibrary("", library);
        }

        foreach (KeyValuePair<string, TriggerSpec> pair in TriggerToSpec)
        {
            if (library.HasAnimation(pair.Key))
            {
                continue;
            }

            Animation animation = new()
            {
                ResourceName = pair.Key,
                Length = 0.05f,
                LoopMode = pair.Value.Loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None
            };
            library.AddAnimation(pair.Key, animation);
        }

        // Add upper-camel aliases because combat code often triggers "Attack"/"Hit"/etc.
        AddAliasAnimation(library, "Idle", IdleState, loop: true);
        AddAliasAnimation(library, "Attack", "attack");
        AddAliasAnimation(library, "Cast", "cast");
        AddAliasAnimation(library, "Hit", "hit");
        AddAliasAnimation(library, "Dead", DeadState);
        AddAliasAnimation(library, "Relaxed", "relaxed", loop: true);
        AddAliasAnimation(library, "Camping", CampingState, loop: true);
    }

    private static void AddAliasAnimation(AnimationLibrary library, string alias, string targetState, bool? loop = null)
    {
        if (library.HasAnimation(alias) || !TriggerToSpec.TryGetValue(targetState, out TriggerSpec? spec))
        {
            return;
        }

        Animation animation = new()
        {
            ResourceName = alias,
            Length = 0.05f,
            LoopMode = (loop ?? spec.Loop) ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None
        };
        library.AddAnimation(alias, animation);
    }

    private GodotObject? GetAnimationState()
    {
        if (TryCall(_visuals, "get_animation_state", out Variant snakeResult))
        {
            return snakeResult.AsGodotObject();
        }

        if (TryCall(_visuals, "GetAnimationState", out Variant pascalResult))
        {
            return pascalResult.AsGodotObject();
        }

        return null;
    }

    private static bool TryCall(GodotObject obj, string methodName, params Variant[] args)
    {
        if (!obj.HasMethod(methodName))
        {
            return false;
        }

        obj.Callv(methodName, new Godot.Collections.Array(args));
        return true;
    }

    private static bool TryCall(GodotObject obj, string methodName, out Variant result, params Variant[] args)
    {
        result = default;
        if (!obj.HasMethod(methodName))
        {
            return false;
        }

        result = obj.Callv(methodName, new Godot.Collections.Array(args));
        return true;
    }
}
