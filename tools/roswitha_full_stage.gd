extends SceneTree

var mode := "ug"
var config: Dictionary
var stage: Node2D
var entries: Array = []
var sample_mode := false
var pma: ShaderMaterial
var particle_entries: Array = []
var cutin: Sprite2D
var cutin_frames: Array = []
var actor: SpineSprite

func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() > 0: mode = args[0]
	sample_mode = args.has("samples")
	root.size = Vector2i(1280, 720)
	RenderingServer.set_default_clear_color(Color.BLACK)
	call_deferred("run")

func number(d: Dictionary, key: String, fallback: float = 0.0) -> float:
	var v = d.get(key, fallback)
	return fallback if str(v).is_empty() else float(v)

func run() -> void:
	config = JSON.parse_string(FileAccess.get_file_as_string("res://config.json"))[mode]
	stage = Node2D.new()
	root.add_child(stage)
	# All source layers share this authored screen-space origin. Never multiply
	# the embedded Spine camera animation by another guessed world camera.
	stage.position = Vector2(640,360)
	var shader := Shader.new()
	shader.code = "shader_type canvas_item; render_mode blend_premul_alpha; varying vec4 tint; void vertex(){ tint=COLOR; } void fragment(){ COLOR = texture(TEXTURE, UV) * tint; }"
	pma = ShaderMaterial.new()
	pma.shader = shader
	for spec in config.layers:
		if spec.format != "spine":
			var particles := Node2D.new()
			particles.set_script(load("res://particles.gd"))
			particles.setup(spec)
			particles.z_index = clampi(int(number(spec,"z")),-4000,4000)
			stage.add_child(particles)
			particle_entries.append({"node":particles,"spec":spec})
			continue
		var sp := SpineSprite.new()
		sp.name = spec.source
		sp.skeleton_data_res = load("res://assets/" + spec.source + ".tres")
		sp.update_mode = 2
		sp.position = Vector2(number(spec,"x"),-number(spec,"y"))
		if spec.group == "target": sp.position += Vector2(250,140)
		sp.rotation_degrees = -number(spec,"rotate")
		sp.scale = Vector2.ONE * number(spec,"scale",1) * float(spec.event_scale)
		sp.modulate.a = number(spec,"opacity",255)/255.0
		sp.z_index = clampi(int(number(spec,"z")), -4000,4000)
		if spec.blend == "pma": sp.normal_material = pma
		stage.add_child(sp)
		sp.visible = false
		entries.append({"sprite":sp,"spec":spec})
	if mode == "ux":
		cutin = Sprite2D.new()
		cutin.z_index = 4095
		stage.add_child(cutin)
		for i in range(int(config.cutin_count)):
			cutin_frames.append(load("res://cutin/%03d.png" % i))
		actor = SpineSprite.new()
		actor.skeleton_data_res = load("res://ArtWorks/roswitha/character/regent_skel_data.tres")
		actor.position = Vector2(-330,210)
		actor.scale = Vector2.ONE*.35
		actor.update_mode = 2
		stage.add_child(actor)
	await process_frame
	var samples := [0.3,0.6,1.0,1.4,2.0,2.8,3.4,4.0] if mode=="ug" else [0.8,1.5,2.8,3.5,4.0,4.7,5.5,6.5,7.2,8.2,9.0,10.0]
	var frames := samples.size() if sample_mode else int(ceil(float(config.total)*30))
	for f in range(frames):
		var t: float = samples[f] if sample_mode else f/30.0
		if is_instance_valid(cutin):
			cutin.visible = t >= .7667 and t < .7667+float(config.pause)
			if cutin.visible:
				cutin.texture = cutin_frames[mini(cutin_frames.size()-1,int((t-.7667)*float(config.cutin_fps)))]
				var fit := maxf(1280.0/cutin.texture.get_width(),720.0/cutin.texture.get_height())
				cutin.scale = Vector2.ONE * fit
		if is_instance_valid(actor):
			actor.visible = t < .7667
			if actor.visible:
				var st = actor.get_animation_state()
				st.set_animation("ux_attack_1",false,0)
				st.update(t)
				st.apply(actor.get_skeleton())
				actor.update_skeleton(0)
		for entry in entries:
			var sp = entry.sprite
			var s: Dictionary = entry.spec
			var local_t: float = t-float(s.at)
			sp.visible = local_t >= 0 and local_t < float(s.life)
			if not sp.visible: continue
			var state = sp.get_animation_state()
			if state == null:
				push_error("Missing Spine state " + sp.name)
				quit(1)
				return
			var anim: String = s.animation
			if not str(s.next_animation).is_empty() and local_t >= float(s.intro_duration):
				local_t -= float(s.intro_duration)
				anim = s.next_animation
			state.set_animation(anim, not str(s.next_animation).is_empty(),0)
			state.update(local_t)
			state.apply(sp.get_skeleton())
			sp.update_skeleton(0.0)
		for entry in particle_entries:
			var node = entry.node
			var s: Dictionary = entry.spec
			node.elapsed = t-float(s.at)
			node.position = Vector2(number(s,"x"),-number(s,"y"))
			if s.group == "target": node.position += Vector2(250,140)
			var sc := str(s.get("scale","1"))
			if sc.contains(","):
				var parts := sc.split(",")
				node.scale = Vector2(float(parts[0]),float(parts[1]))
			else: node.scale = Vector2.ONE*number(s,"scale",1)
			var attach := str(s.get("attach",""))
			if not attach.is_empty():
				var parts := attach.split("/")
				for sp_entry in entries:
					if sp_entry.spec.source != parts[0]: continue
					var sp = sp_entry.sprite
					var bone = sp.get_skeleton().find_bone(parts[1])
					if bone != null: node.position += sp.position + Vector2(bone.get_world_x(),-bone.get_world_y())
			node.queue_redraw()
		await process_frame
		await RenderingServer.frame_post_draw
		var dest := "res://%s/%s_%04d.png" % [mode,"sample" if sample_mode else "frame",f]
		root.get_texture().get_image().save_png(dest)
	print("DONE ", mode, " frames=",frames)
	quit()
