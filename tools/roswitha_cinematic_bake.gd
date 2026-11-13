extends SceneTree

var mode := "ug"
var stage: Node2D
var layers: Array = []
var sprites: Array = []
var count := 0
var total := 0.0

func _initialize() -> void:
	if OS.get_cmdline_user_args().size() > 0:
		mode = OS.get_cmdline_user_args()[0]
	root.size = Vector2i(960, 540)
	RenderingServer.set_default_clear_color(Color.BLACK)
	call_deferred("bake")

func collect(node: Node) -> void:
	node.set_process(false)
	if node is SpineSprite:
		node.update_mode = 2
		sprites.append(node)
	for child in node.get_children():
		collect(child)

func bake() -> void:
	if mode == "ug":
		stage = Node2D.new()
		stage.set_script(load("res://ug_stage.gd"))
		root.add_child(stage)
		collect(stage)
		collect(stage._canvas)
		total = 4.49
	else:
		stage = Node2D.new()
		root.add_child(stage)
		stage.position = Vector2(480, 270)
		stage.scale = Vector2.ONE * 0.465
		layers = JSON.parse_string(FileAccess.get_file_as_string("res://ux_layers.json"))
		for spec in layers:
			var sp := SpineSprite.new()
			sp.skeleton_data_res = load("res://ux_assets/" + spec.source + ".tres")
			sp.update_mode = 2
			sp.position = Vector2(float(spec.get("x", 0)), -float(spec.get("y", 0)))
			sp.scale = Vector2.ONE * float(spec.get("scale", 1))
			sp.rotation_degrees = -float(spec.get("rotate", 0))
			sp.z_index = clampi(int(spec.get("z", 0)), -4096, 4096)
			stage.add_child(sp)
			sprites.append(sp)
			total = maxf(total, float(spec.at) + float(spec.life))
	print("BAKE ", mode, " layers=", sprites.size(), " total=", total)
	await process_frame
	await process_frame
	for frame in range(int(ceil(total * 30))):
		var t := frame / 30.0
		if mode == "ug":
			stage._elapsed = t
			stage._process(0.0)
		for i in range(sprites.size()):
			var sp = sprites[i]
			var local_t := t
			var anim := "animation"
			if mode == "ux":
				local_t -= float(layers[i].at)
				anim = layers[i].animation
				sp.visible = local_t >= 0 and local_t <= float(layers[i].life)
			if not sp.visible:
				continue
			var state = sp.get_animation_state()
			if state == null:
				push_error("No animation state: " + sp.name)
				quit(1)
				return
			state.set_animation(anim, false, 0)
			state.update(maxf(local_t, 0))
			state.apply(sp.get_skeleton())
			sp.update_skeleton(0.0)
		await process_frame
		await RenderingServer.frame_post_draw
		root.get_texture().get_image().save_png("res://%s/frame_%04d.png" % [mode, frame])
	print("BAKE DONE ", mode)
	quit()
