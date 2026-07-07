using Godot;

namespace MeiLinMod.MeiLinModCode.Nodes;

[GlobalClass]
[ScriptPath("res://GodotScripts/Nodes/SpineAutoPlayer.cs")]
public partial class SpineAutoPlayer : Node
{
    [Export] public string AnimationName { get; set; } = "animation";
    [Export] public bool Loop { get; set; } = true;
    [Export] public string FollowUpAnimationName { get; set; } = "";
    [Export] public bool FollowUpLoop { get; set; } = true;
    [Export] public float FollowUpDelay { get; set; } = 0f;
    [Export] public string SkinName { get; set; } = "";
    [Export] public bool ResetSlotsToSetupPose { get; set; }
    [Export] public bool ResetSlotsBeforeFollowUp { get; set; }
    [Export] public string SlotFixProfile { get; set; } = "";

    private bool _played;
    private bool _setupPoseApplied;
    private bool _waitingForFollowUp;
    private GodotObject? _spineSprite;
    private GodotObject? _animationState;

    public override void _Ready()
    {
        TryPlay();
        ApplyConfiguredSlotFixes();
    }

    public override void _Process(double delta)
    {
        if (_played)
            TryPlayFollowUp();
        else
            TryPlay();

        ApplyConfiguredSlotFixes();
    }

    private void TryPlay()
    {
        if (string.IsNullOrWhiteSpace(AnimationName) || GetParent() is not GodotObject spineSprite)
            return;

        _spineSprite = spineSprite;
        ApplyInitialPose(spineSprite);

        GodotObject? animationState = GetAnimationState(spineSprite);
        if (animationState == null)
            return;

        _animationState = animationState;
        if (TrySetAnimation(animationState, AnimationName, Loop, 0))
        {
            if (!string.IsNullOrWhiteSpace(FollowUpAnimationName))
            {
                if (ResetSlotsBeforeFollowUp && !Loop)
                    _waitingForFollowUp = true;
                else
                    TryAddAnimation(animationState, FollowUpAnimationName, FollowUpLoop, 0, FollowUpDelay);
            }

            _played = true;
        }
    }

    private void TryPlayFollowUp()
    {
        if (!_waitingForFollowUp || _spineSprite == null || _animationState == null)
            return;

        if (!IsCurrentAnimationComplete(_animationState, 0))
            return;

        ApplyInitialPose(_spineSprite, force: true);
        TrySetAnimation(_animationState, FollowUpAnimationName, FollowUpLoop, 0);
        _waitingForFollowUp = false;
    }

    private void ApplyInitialPose(GodotObject spineSprite, bool force = false)
    {
        if (_setupPoseApplied && !force)
            return;

        if (string.IsNullOrWhiteSpace(SkinName) && !ResetSlotsToSetupPose)
        {
            _setupPoseApplied = true;
            return;
        }

        GodotObject? skeleton = GetSkeleton(spineSprite);
        if (skeleton == null)
            return;

        if (!string.IsNullOrWhiteSpace(SkinName))
            TryCall(skeleton, "set_skin_by_name", SkinName);

        if (ResetSlotsToSetupPose)
            TryCall(skeleton, "set_slots_to_setup_pose");

        _setupPoseApplied = true;
    }

    private void ApplyConfiguredSlotFixes()
    {
        if (_spineSprite == null || string.IsNullOrWhiteSpace(SlotFixProfile))
            return;

        GodotObject? skeleton = GetSkeleton(_spineSprite);
        if (skeleton == null)
            return;

        switch (SlotFixProfile.Trim().ToLowerInvariant())
        {
            case "attack_defense_unity":
                HideSlotAttachment(skeleton, "face_shadow_1");
                HideSlotAttachment(skeleton, "face_shadow_2");
                break;
        }
    }

    private static bool TrySetAnimation(GodotObject animationState, string animation, bool loop, int track)
    {
        return TryCall(animationState, "set_animation", animation, loop, track) ||
               TryCall(animationState, "SetAnimation", animation, loop, track);
    }

    private static bool TryAddAnimation(GodotObject animationState, string animation, bool loop, int track, float delay)
    {
        return TryCall(animationState, "add_animation", animation, delay, loop, track) ||
               TryCall(animationState, "AddAnimation", animation, delay, loop, track);
    }

    private static GodotObject? GetAnimationState(GodotObject spineSprite)
    {
        if (TryCall(spineSprite, "get_animation_state", out Variant snakeResult))
            return snakeResult.AsGodotObject();

        if (TryCall(spineSprite, "GetAnimationState", out Variant pascalResult))
            return pascalResult.AsGodotObject();

        return null;
    }

    private static GodotObject? GetSkeleton(GodotObject spineSprite)
    {
        if (TryCall(spineSprite, "get_skeleton", out Variant snakeResult))
            return snakeResult.AsGodotObject();

        if (TryCall(spineSprite, "GetSkeleton", out Variant pascalResult))
            return pascalResult.AsGodotObject();

        return null;
    }

    private static GodotObject? FindSlot(GodotObject skeleton, string slotName)
    {
        if (TryCall(skeleton, "find_slot", out Variant snakeResult, slotName))
            return snakeResult.AsGodotObject();

        if (TryCall(skeleton, "FindSlot", out Variant pascalResult, slotName))
            return pascalResult.AsGodotObject();

        return null;
    }

    private static void HideSlotAttachment(GodotObject skeleton, string slotName)
    {
        GodotObject? slot = FindSlot(skeleton, slotName);
        if (slot == null)
            return;

        Variant nullAttachment = default;
        TryCall(slot, "set_attachment", nullAttachment);
        TryCall(slot, "SetAttachment", nullAttachment);
    }

    private static bool IsCurrentAnimationComplete(GodotObject animationState, int track)
    {
        GodotObject? trackEntry = null;
        if (TryCall(animationState, "get_current", out Variant snakeResult, track))
            trackEntry = snakeResult.AsGodotObject();
        else if (TryCall(animationState, "GetCurrent", out Variant pascalResult, track))
            trackEntry = pascalResult.AsGodotObject();

        if (trackEntry == null)
            return false;

        if (TryCall(trackEntry, "is_complete", out Variant isCompleteResult))
            return isCompleteResult.AsBool();

        if (TryCall(trackEntry, "IsComplete", out Variant isCompletePascalResult))
            return isCompletePascalResult.AsBool();

        return false;
    }

    private static bool TryCall(GodotObject obj, string methodName, params Variant[] args)
    {
        if (!obj.HasMethod(methodName))
            return false;

        args ??= [];
        obj.Callv(methodName, new Godot.Collections.Array(args));
        return true;
    }

    private static bool TryCall(GodotObject obj, string methodName, out Variant result, params Variant[] args)
    {
        result = default;
        if (!obj.HasMethod(methodName))
            return false;

        args ??= [];
        result = obj.Callv(methodName, new Godot.Collections.Array(args));
        return true;
    }
}
