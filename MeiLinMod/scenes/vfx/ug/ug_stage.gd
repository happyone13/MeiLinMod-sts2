extends CanvasLayer
## One isolated 1280x720 presentation. All borrowed visuals are restored on exit.
@export var config_path: String
var config: Dictionary
var elapsed: float = 0.0
var running: bool = false
var event_index: int = 0
var borrowed: Array = []
var actor_original: CanvasItem
var actor_was_visible: bool
var actor_origin := Vector2(-320, 175)
var actor_home_scale := Vector2.ONE
var actor_started: bool = false
var fx: Array = []
var camera_event: Dictionary = {}
var actor_motion: Dictionary = {}
var enemy_motion: Dictionary = {}
var started_usec: int
var restored: bool = false
var preview_mode: bool = false

func _ready() -> void:
    config = JSON.parse_string(FileAccess.get_file_as_string(config_path))
    fit_viewport()
    for i in range(config.effects.size()):
        var e: Dictionary = config.effects[i]
        var parent: Node = $Stage if e.type == "SCREEN" else $Stage/World
        var node: Node2D = parent.get_node("Fx%d" % i)
        suspend_spines(node)
        fx.append({"node": node, "event": e, "started": false})
    set_process(false)
    if "--ug-preview" in OS.get_cmdline_user_args():
        call_deferred("begin_preview")

func fit_viewport() -> void:
    var size: Vector2 = get_viewport().get_visible_rect().size
    $Stage.position = size * 0.5
    $Stage.scale = Vector2.ONE * maxf(size.x / 1280.0, size.y / 720.0)

func begin(actor: CanvasItem, enemies: Array) -> void:
    actor_original = actor
    if is_instance_valid(actor):
        actor_was_visible = actor.visible
        actor_origin = $Stage.to_local(actor.get_global_transform_with_canvas().origin)
        actor.visible = false
    actor_home_scale = $Stage/World/Actor.scale
    $Stage/World/Actor.position = actor_origin
    $Stage/World/Actor.visible = false
    for enemy in enemies:
        if not is_instance_valid(enemy):
            continue
        var screen: Transform2D = enemy.get_global_transform_with_canvas()
        var snapshot: Dictionary = {"node": enemy, "parent": enemy.get_parent(), "index": enemy.get_index(),
            "transform": enemy.transform, "modulate": enemy.modulate, "visible": enemy.visible,
            "z": enemy.z_index, "relative": enemy.z_as_relative}
        enemy.reparent($Stage/World, false)
        enemy.transform = $Stage.global_transform.affine_inverse() * screen
        enemy.z_index = 0
        enemy.z_as_relative = true
        snapshot["home"] = enemy.transform
        borrowed.append(snapshot)
    begin_clock()

func begin_preview() -> void:
    # Preview uses the same scene, clock, event routing, and cleanup as combat.
    preview_mode = true
    begin_clock()

func begin_clock() -> void:
    running = true
    started_usec = Time.get_ticks_usec()
    set_process(true)
    _process(0)

func _process(_delta: float) -> void:
    if not running:
        return
    # Movie Maker can render faster than wall-clock time; previews advance by the
    # recorded frame delta while combat remains tied to real authored seconds.
    if preview_mode:
        elapsed += _delta
    else:
        elapsed = (Time.get_ticks_usec() - started_usec) / 1000000.0
    fit_viewport()
    while event_index < config.events.size() and config.events[event_index].at <= elapsed:
        dispatch(config.events[event_index])
        event_index += 1
    apply_motion()
    for item in fx:
        var e: Dictionary = item.event
        var node: Node2D = item.node
        if not item.started and elapsed >= e.at:
            item.started = true
            node.visible = true
            node.scale = Vector2.ONE * float(e.scale)
            node.rotation_degrees = -float(e.rotation)
            node.z_index = clampi(int(float(e.zorder) / 10.0), -2000, 2000)
            if e.type == "SCREEN":
                node.z_index = 1000
            elif e.type == "CENTER":
                node.z_index = -100 if e.file_name.ends_with("bg_eff") else clampi(int(float(e.zorder) / 10.0), -2000, 2000)
            start_spines(node)
        if item.started:
            if elapsed >= e.at + e.life:
                node.visible = false
            elif e.type == "SELF":
                node.position = $Stage/World/Actor.position + offset(e)
                if e.get("inherit_scale", false):
                    node.scale = $Stage/World/Actor.scale * float(e.scale)
            elif e.type == "TARGET":
                # MeiLin's UG is single-target; Yuki's UG has no TARGET CFX.
                if not borrowed.is_empty() and is_instance_valid(borrowed[0].node):
                    node.position = borrowed[0].node.position + offset(e)
                else:
                    node.position = Vector2(220, 150) + offset(e)
    if elapsed >= config.total:
        finish()

func offset(e: Dictionary) -> Vector2:
    var parts: PackedStringArray = str(e.get("offset_xy", "0,0")).split(",")
    return Vector2(float(parts[0]), -float(parts[1]))

func dispatch(e: Dictionary) -> void:
    match e.etty:
        "ANI":
            actor_started = true
            $Stage/World/Actor.visible = true
            play_animation($Stage/World/Actor, e.animation_name)
        "IDLE":
            if e.get("target", "SELF") != "TARGET":
                actor_started = true
                $Stage/World/Actor.visible = true
                $Stage/World/Actor.position = actor_origin
                actor_motion = {}
                play_animation($Stage/World/Actor, "b_idle", true)
        "CAM_SPINE":
            camera_event = e
        "NODE_ANI":
            if e.target == "SELF":
                actor_motion = e
            else:
                enemy_motion = e
        "MOVE":
            if e.get("our_target_type") == "SELF" and e.get("dest_type") == "NONE":
                actor_motion = {}
                $Stage/World/Actor.position = actor_origin

func apply_motion() -> void:
    var actor: Node2D = $Stage/World/Actor
    var world: Node2D = $Stage/World
    if not camera_event.is_empty():
        var pose: Transform2D = track_pose(camera_event.file_name, elapsed - camera_event.at)
        world.transform = pose
    if not actor_motion.is_empty():
        var pose: Transform2D = track_pose(actor_motion.file_name, elapsed - actor_motion.at)
        actor.transform = pose
        # MeiLin's char-node track contains a cinematic zoom and its event adds
        # another 1.5x scale. Keep the portrait actor at its authored base size.
        if config.character == "meilin":
            actor.scale = actor_home_scale
        else:
            actor.scale *= float(actor_motion.get("scale", 1))
        if actor_motion.get("pivot") == "SELF":
            actor.position += actor_origin
    for i in range(borrowed.size()):
        var snapshot: Dictionary = borrowed[i]
        var node: Node2D = snapshot.node
        if not is_instance_valid(node):
            continue
        if not enemy_motion.is_empty():
            var pose: Transform2D = track_pose(enemy_motion.file_name, elapsed - enemy_motion.at)
            node.position = pose.origin + Vector2((i - (borrowed.size() - 1) * 0.5) * 145, 0)
            node.scale = snapshot.home.get_scale() * pose.get_scale()
            node.rotation = pose.get_rotation()
        if config.character == "yuki" and config.get("presentation", "ug") == "ux":
            node.visible = snapshot.visible and (elapsed < 0.6 or elapsed >= 4.066)
            if elapsed >= 4.066:
                node.transform = snapshot.home
        elif config.character == "yuki":
            node.visible = snapshot.visible and (elapsed < 0.6 or elapsed >= 1.6)
            node.modulate = snapshot.modulate * (Color(0.12, 0.3, 0.95, 1) if elapsed >= 1.933 and elapsed < 4.3 else Color.WHITE)
            if elapsed >= 4.3:
                node.transform = snapshot.home
        else:
            node.visible = snapshot.visible and (elapsed < 0.5 or elapsed >= 1.7)
            if elapsed >= 2.1:
                node.transform = snapshot.home
    if config.character == "yuki" and config.get("presentation", "ug") == "ux":
        actor.visible = actor_started and elapsed < 1.4
        $Stage/Blackout.color.a = 0.0 if elapsed < 0.6 else (minf((elapsed - 0.6) / 0.15, 1.0) if elapsed < 3.733 else maxf(0.0, 1.0 - (elapsed - 3.733) / 0.333))
    elif config.character == "yuki":
        actor.visible = actor_started and (elapsed < 1.5663 or elapsed >= 4.3)
        $Stage/Blackout.color.a = minf(elapsed / 0.3, 1) if elapsed < 4.62 else maxf(0, 1 - (elapsed - 4.62) / 0.3)
    elif config.get("presentation", "ug") == "ux":
        actor.modulate = Color.WHITE
        $Stage/Blackout.color.a = 1.0 if elapsed < 2.867 else maxf(0.0, 1.0 - (elapsed - 2.867) / 0.3)
    else:
        actor.modulate = Color(0, 0, 0, 1) if elapsed >= 0.5 and elapsed < 1.3 else Color.WHITE
        $Stage/Blackout.color.a = minf(elapsed / 0.5, 1) if elapsed < 2.1 else maxf(0, 1 - (elapsed - 2.1) / 0.02)
        if elapsed >= 2.116 and elapsed < 2.9999:
            actor.position = borrowed[0].home.origin + Vector2(-100, 0) if not borrowed.is_empty() else Vector2(120, 175)
    # Camera poses are local to the source cinematic; release them before return.
    if (config.character == "meilin" and config.get("presentation", "ug") == "ux" and elapsed >= 2.867) or (config.character == "meilin" and config.get("presentation", "ug") == "ug" and elapsed >= 2.1) or (config.character == "yuki" and config.get("presentation", "ug") == "ux" and elapsed >= 3.733) or (config.character == "yuki" and config.get("presentation", "ug") == "ug" and elapsed >= 4.3):
        world.transform = Transform2D.IDENTITY

func track_pose(name: String, t: float) -> Transform2D:
    var data: Dictionary = config.tracks[name]
    var animation: Dictionary = data.animations.values()[0]
    var poses: Dictionary = {}
    for bone in data.bones:
        var timeline: Dictionary = animation.get("bones", {}).get(bone.name, {})
        var p: Vector2 = Vector2(float(bone.get("x", 0)), float(bone.get("y", 0)))
        p += sample_pair(timeline.get("translate", []), t, Vector2.ZERO)
        var s: Vector2 = Vector2(float(bone.get("scaleX", 1)), float(bone.get("scaleY", 1)))
        s *= sample_pair(timeline.get("scale", []), t, Vector2.ONE)
        var r: float = float(bone.get("rotation", 0)) + sample_value(timeline.get("rotate", []), t, "angle", 0)
        var pose := Transform2D(deg_to_rad(-r), s, 0, Vector2(p.x, -p.y))
        poses[bone.name] = poses.get(bone.get("parent", ""), Transform2D.IDENTITY) * pose
    return poses.get("node", Transform2D.IDENTITY)

func sample_pair(keys: Array, t: float, fallback: Vector2) -> Vector2:
    return Vector2(sample_value(keys, t, "x", fallback.x), sample_value(keys, t, "y", fallback.y))

func sample_value(keys: Array, t: float, key: String, fallback: float) -> float:
    if keys.is_empty() or t < float(keys[0].get("time", 0)):
        return fallback
    for i in range(keys.size() - 1):
        var a: Dictionary = keys[i]
        var b: Dictionary = keys[i + 1]
        if t >= float(b.get("time", 0)):
            continue
        var weight: float = (t - float(a.get("time", 0))) / maxf(0.00001, float(b.get("time", 0)) - float(a.get("time", 0)))
        var curve = a.get("curve", "linear")
        if curve is String and curve == "stepped":
            weight = 0
        elif curve is float or curve is int:
            var low: float = 0
            var high: float = 1
            for j in range(16):
                var u: float = (low + high) * 0.5
                if bezier(u, float(curve), float(a.get("c3", 1))) < weight:
                    low = u
                else:
                    high = u
            weight = bezier((low + high) * 0.5, float(a.get("c2", 0)), float(a.get("c4", 1)))
        return lerpf(float(a.get(key, fallback)), float(b.get(key, fallback)), weight)
    return float(keys[-1].get(key, fallback))

func bezier(t: float, a: float, b: float) -> float:
    return 3 * (1-t) * (1-t) * t * a + 3 * (1-t) * t * t * b + t*t*t

func suspend_spines(node: Node) -> void:
    if node.has_method("get_animation_state"):
        var state = node.get_animation_state()
        if state != null:
            state.set_time_scale(0)
    if node is GPUParticles2D:
        node.emitting = false
    for child in node.get_children():
        suspend_spines(child)

func start_spines(node: Node) -> void:
    if node.has_method("get_animation_state"):
        var anim: String = str(node.get("preview_animation"))
        play_animation(node, anim if not anim.is_empty() else "animation")
    if node is GPUParticles2D:
        node.restart()
        node.emitting = true
    for child in node.get_children():
        start_spines(child)

func play_animation(node: Node, anim: String, loop: bool = false) -> void:
    var state = node.get_animation_state()
    if state == null:
        return
    state.set_time_scale(1)
    state.set_animation(anim, loop)

func finish() -> void:
    running = false
    set_process(false)
    restore()
    $Stage.visible = false

func restore() -> void:
    if restored:
        return
    restored = true
    if is_instance_valid(actor_original):
        actor_original.visible = actor_was_visible
    for snapshot in borrowed:
        var node: Node2D = snapshot.node
        if not is_instance_valid(node):
            continue
        if is_instance_valid(snapshot.parent) and node.get_parent() == $Stage/World:
            node.reparent(snapshot.parent, false)
            snapshot.parent.move_child(node, mini(snapshot.index, snapshot.parent.get_child_count() - 1))
            node.transform = snapshot.transform
            node.modulate = snapshot.modulate
            node.visible = snapshot.visible
            node.z_index = snapshot.z
            node.z_as_relative = snapshot.relative
    borrowed.clear()

func _exit_tree() -> void:
    restore()
