using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MeiLinMod.MeiLinModCode.Config;

namespace MeiLinMod.MeiLinModCode.Vfx;

public static class MeiLinAttackMovementController
{
    private const float AttackDistance = 170f;
    private const float MinimumMoveDistance = 8f;
    private const float ReturnPadSeconds = 0.05f;
    private const float MovementEffectScale = 1.3f;
    private const int AttackZIndex = 1000;
    private const string FootEffectMarkerName = "MeiLinFootEff";
    private const string MovementEffectFrontScenePath = "res://MeiLinMod/scenes/vfx/generated/ug_attack/meirin_1027_ug_attack_end_f.tscn";
    private const string MovementEffectBackScenePath = "res://MeiLinMod/scenes/vfx/generated/ug_attack/meirin_1027_ug_attack_end_b.tscn";

    private sealed class MovementSession
    {
        public bool HasOrigin;
        public bool Teleported;
        public Vector2 OriginGlobalPosition;
        public Vector2 AttackGlobalPosition;
        public bool HasOriginalLayer;
        public int OriginalZIndex;
        public bool OriginalZAsRelative;
        public int Version;
    }

    private static readonly ConditionalWeakTable<Creature, MovementSession> Sessions = new();

    public static IEnumerable<string> GetPreloadScenePaths()
    {
        yield return MovementEffectFrontScenePath;
        yield return MovementEffectBackScenePath;
    }

    public static void PreloadMovementEffects()
    {
        MeiLinVfxHelper.Prewarm(GetPreloadScenePaths());
    }

    public static async Task MoveToTargetIfNeededAsync(Creature caster, Creature? target)
    {
        if (!MeiLinModConfig.UseCombatEffects || target == null)
            return;

        if (!TryGetRoomAndNodes(caster, target, out _, out var casterNode, out var targetNode))
            return;

        var session = Sessions.GetOrCreateValue(caster);
        if (session.Teleported)
            return;

        if (!session.HasOrigin)
        {
            session.HasOrigin = true;
            session.OriginGlobalPosition = casterNode.GlobalPosition;
        }

        var targetFoot = GetCreatureFootAnchor(targetNode);
        if (targetFoot == Vector2.Zero)
            return;

        var planned = ComputeDesiredGlobalPositionByFoot(casterNode, targetFoot, AttackDistance, caster.Side);
        if (planned.DistanceTo(casterNode.GlobalPosition) < MinimumMoveDistance)
            return;

        session.Teleported = true;
        session.AttackGlobalPosition = planned;
        session.Version++;
        var version = session.Version;

        MainFile.Logger.Info($"[MeiLinMove] Move to target. origin={session.OriginGlobalPosition}, targetFoot={targetFoot}, planned={planned}, distance={AttackDistance:0.#}");
        PlayMovementEffectPair(casterNode, GetCreatureFootAnchor(casterNode), "leave_origin");
        RaiseAboveEnemies(casterNode, session);
        casterNode.GlobalPosition = planned;
        casterNode.GlobalPosition = planned;
        PlayMovementEffectPair(casterNode, GetCreatureFootAnchor(casterNode), "arrive_target");
        StartPositionLock(caster, version, planned, 0.8f);

        await Cmd.CustomScaledWait(0.01f, 0.01f);
    }

    public static void ScheduleReturnAfterSegment(Creature caster, string commandName, bool isFinalSegment)
    {
        var session = Sessions.GetOrCreateValue(caster);
        if (!session.Teleported)
            return;

        var duration = MathF.Max(0.2f, MeiLinCommandVfxCoordinator.GetCommandDurationSeconds(commandName));
        if (!isFinalSegment)
        {
            session.Version++;
            StartPositionLock(caster, session.Version, session.AttackGlobalPosition, duration + 0.35f);
            return;
        }

        session.Version++;
        _ = ReturnAfterDelayAsync(caster, session.Version, duration + ReturnPadSeconds);
    }

    private static async Task ReturnAfterDelayAsync(Creature caster, int version, float seconds)
    {
        try
        {
            await Cmd.CustomScaledWait(MathF.Max(0.01f, seconds), MathF.Max(0.01f, seconds));

            if (!Sessions.TryGetValue(caster, out var session) ||
                !session.Teleported ||
                session.Version != version)
            {
                return;
            }

            var room = NCombatRoom.Instance;
            var casterNode = room?.GetCreatureNode(caster);
            if (casterNode == null || !GodotObject.IsInstanceValid(casterNode))
            {
                ResetSession(session);
                return;
            }

            var origin = session.HasOrigin ? session.OriginGlobalPosition : casterNode.GlobalPosition;
            PlayMovementEffectPair(casterNode, GetCreatureFootAnchor(casterNode), "leave_target");
            RestoreLayer(casterNode, session);
            ResetSession(session);

            MainFile.Logger.Info($"[MeiLinMove] Return to origin. origin={origin}");
            casterNode.GlobalPosition = origin;
            casterNode.GlobalPosition = origin;
            PlayMovementEffectPair(casterNode, GetCreatureFootAnchor(casterNode), "arrive_origin");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinMove] Return failed. ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async void StartPositionLock(Creature caster, int version, Vector2 position, float seconds)
    {
        try
        {
            var endTicks = Time.GetTicksMsec() + (ulong)MathF.Round(MathF.Max(0.01f, seconds) * 1000f);
            while (Time.GetTicksMsec() < endTicks)
            {
                if (!Sessions.TryGetValue(caster, out var session) ||
                    !session.Teleported ||
                    session.Version != version)
                {
                    return;
                }

                var room = NCombatRoom.Instance;
                var casterNode = room?.GetCreatureNode(caster);
                if (casterNode == null || !GodotObject.IsInstanceValid(casterNode))
                    return;

                RaiseAboveEnemies(casterNode, session);
                casterNode.GlobalPosition = position;
                var tree = casterNode.GetTree();
                if (tree == null || !GodotObject.IsInstanceValid(tree))
                    return;

                await casterNode.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }
        catch
        {
        }
    }

    private static bool TryGetRoomAndNodes(
        Creature caster,
        Creature target,
        out NCombatRoom room,
        out NCreature casterNode,
        out NCreature targetNode)
    {
        room = null!;
        casterNode = null!;
        targetNode = null!;

        var currentRoom = NCombatRoom.Instance;
        if (currentRoom == null)
            return false;

        var currentCasterNode = currentRoom.GetCreatureNode(caster);
        var currentTargetNode = currentRoom.GetCreatureNode(target);
        if (currentCasterNode == null ||
            currentTargetNode == null ||
            !GodotObject.IsInstanceValid(currentCasterNode) ||
            !GodotObject.IsInstanceValid(currentTargetNode))
        {
            return false;
        }

        room = currentRoom;
        casterNode = currentCasterNode;
        targetNode = currentTargetNode;
        return casterNode != null &&
               targetNode != null &&
               GodotObject.IsInstanceValid(casterNode) &&
               GodotObject.IsInstanceValid(targetNode);
    }

    private static void PlayMovementEffectPair(NCreature casterNode, Vector2 footGlobalPosition, string phase)
    {
        if (!MeiLinModConfig.UseCombatEffects || footGlobalPosition == Vector2.Zero)
            return;

        try
        {
            var room = NCombatRoom.Instance;
            var parent = room?.CombatVfxContainer;
            if (parent == null || !GodotObject.IsInstanceValid(parent))
                return;

            MeiLinVfxHelper.PlayComposite(
                MovementEffectBackScenePath,
                parent,
                footGlobalPosition,
                uniformScale: MovementEffectScale);
            MeiLinVfxHelper.PlayComposite(
                MovementEffectFrontScenePath,
                parent,
                footGlobalPosition,
                uniformScale: MovementEffectScale);

            MainFile.Logger.Info($"[MeiLinMove] Play movement VFX. phase={phase}, foot={footGlobalPosition}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[MeiLinMove] Movement VFX failed. phase={phase}, ex={ex.GetType().Name}: {ex.Message}");
        }
    }

    private static Vector2 ComputeDesiredGlobalPositionByFoot(
        NCreature casterNode,
        Vector2 targetFoot,
        float distance,
        CombatSide side)
    {
        var casterFoot = GetCreatureFootAnchor(casterNode);
        var dir = targetFoot - casterFoot;
        if (dir.LengthSquared() < 0.001f)
            dir = side == CombatSide.Player ? Vector2.Right : Vector2.Left;
        else
            dir = dir.Normalized();

        var desiredFoot = targetFoot - dir * distance;
        var footOffset = casterFoot - casterNode.GlobalPosition;
        return Snap(desiredFoot - footOffset);
    }

    private static Vector2 GetCreatureFootAnchor(NCreature creatureNode)
    {
        var markerPosition = TryGetEffectMarkerPosition(creatureNode, FootEffectMarkerName);
        if (markerPosition != null)
            return markerPosition.Value;

        try
        {
            var hitbox = creatureNode.Hitbox;
            if (hitbox != null && GodotObject.IsInstanceValid(hitbox))
            {
                var rect = hitbox.GetGlobalRect();
                return new Vector2(rect.Position.X + rect.Size.X * 0.5f, rect.Position.Y + rect.Size.Y);
            }
        }
        catch
        {
        }

        try
        {
            return creatureNode.VfxSpawnPosition;
        }
        catch
        {
            return Vector2.Zero;
        }
    }

    private static Vector2? TryGetEffectMarkerPosition(NCreature creatureNode, string markerName)
    {
        try
        {
            Node2D? visualsRoot = creatureNode.Visuals;
            if (visualsRoot != null && GodotObject.IsInstanceValid(visualsRoot))
            {
                var marker = visualsRoot.GetNodeOrNull<Marker2D>($"Visuals/{markerName}");
                if (marker != null && GodotObject.IsInstanceValid(marker))
                    return marker.GlobalPosition;

                marker = visualsRoot.GetNodeOrNull<Marker2D>($"%{markerName}");
                if (marker != null && GodotObject.IsInstanceValid(marker))
                    return marker.GlobalPosition;
            }
        }
        catch
        {
        }

        try
        {
            var marker = creatureNode.GetNodeOrNull<Marker2D>($"%{markerName}");
            if (marker != null && GodotObject.IsInstanceValid(marker))
                return marker.GlobalPosition;
        }
        catch
        {
        }

        return null;
    }

    private static void RaiseAboveEnemies(NCreature casterNode, MovementSession session)
    {
        try
        {
            if (!session.HasOriginalLayer)
            {
                session.HasOriginalLayer = true;
                session.OriginalZIndex = casterNode.ZIndex;
                session.OriginalZAsRelative = casterNode.ZAsRelative;
            }

            casterNode.ZAsRelative = false;
            casterNode.ZIndex = AttackZIndex;
        }
        catch
        {
        }
    }

    private static void RestoreLayer(NCreature casterNode, MovementSession session)
    {
        if (!session.HasOriginalLayer)
            return;

        try
        {
            casterNode.ZAsRelative = session.OriginalZAsRelative;
            casterNode.ZIndex = session.OriginalZIndex;
        }
        catch
        {
        }
    }

    private static Vector2 Snap(Vector2 pos)
    {
        return new Vector2(Mathf.Round(pos.X), Mathf.Round(pos.Y));
    }

    private static void ResetSession(MovementSession session)
    {
        session.HasOrigin = false;
        session.Teleported = false;
        session.OriginGlobalPosition = Vector2.Zero;
        session.AttackGlobalPosition = Vector2.Zero;
        session.HasOriginalLayer = false;
        session.OriginalZIndex = 0;
        session.OriginalZAsRelative = true;
        session.Version++;
    }
}
