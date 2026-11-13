extends Node2D
## Native visual stage. Host owns eligibility, original gameplay commands and cancellation.
var mode := "skin_ug"
var config: Dictionary
var stage: Node2D
var entries: Array = []
var pma: ShaderMaterial
var multiply_pma: ShaderMaterial
var particle_entries: Array = []
var cutin: Sprite2D
var cutin_frames: Array = []
var actor: SpineSprite
var blackout: ColorRect
var attack := "ug"
var intro: SpineSprite
var elapsed := 0.0
var total := 0.0
var running := false
var started_usec := 0
var building := false
var canvas: CanvasLayer
var audio: AudioStreamPlayer
func number(d: Dictionary,key: String,fallback: float=0.0)->float:
	var v=d.get(key,fallback)
	return fallback if str(v).is_empty() else float(v)
func begin(name: String)->bool:
	if running or building or name not in ["base_ug","base_ux","skin_ug","skin_ux"]: return false
	mode=name
	config=JSON.parse_string(FileAccess.get_file_as_string("res://RoswithaV2/config.json"))[mode]
	total=float(config.total)
	building=true
	call_deferred("build")
	return true
func build()->void:
	config = JSON.parse_string(FileAccess.get_file_as_string("res://RoswithaV2/config.json"))[mode]
	canvas = CanvasLayer.new()
	canvas.layer = 4090
	add_child(canvas)
	stage = Node2D.new()
	canvas.add_child(stage)
	# All source layers share this authored screen-space origin. Never multiply
	# the embedded Spine camera animation by another guessed world camera.
	stage.position = Vector2(640,360)
	attack = config.attack
	blackout = ColorRect.new()
	blackout.position = Vector2(-640,-360)
	blackout.size = Vector2(1280,720)
	blackout.color = Color.BLACK
	blackout.mouse_filter = Control.MOUSE_FILTER_IGNORE
	blackout.z_index = -4096
	stage.add_child(blackout)
	pma = ShaderMaterial.new()
	pma.shader = load("res://RoswithaV2/spine_pma.gdshader")
	var multiply_shader := Shader.new()
	multiply_shader.code = "shader_type canvas_item; render_mode blend_mul,unshaded; varying vec4 tint; void vertex(){tint=COLOR;} void fragment(){vec4 t=texture(TEXTURE,UV); float a=t.a*tint.a; COLOR=vec4(t.rgb*tint.rgb*tint.a+vec3(1.0-a),1.0);}"
	multiply_pma=ShaderMaterial.new()
	multiply_pma.shader=multiply_shader
	for spec in config.layers:
		if spec.format != "spine":
			var particles := Node2D.new()
			particles.set_script(load("res://RoswithaV2/particles.gd"))
			particles.setup(spec)
			particles.z_index = clampi(int(number(spec,"z")),-4000,4000)
			stage.add_child(particles)
			particle_entries.append({"node":particles,"spec":spec})
			continue
		var sp := SpineSprite.new()
		sp.name = spec.source
		sp.skeleton_data_res = load("res://RoswithaV2/assets/" + spec.source + ".tres")
		sp.update_mode = 2
		sp.position = Vector2(number(spec,"x"),-number(spec,"y"))
		if spec.group == "target": sp.position += Vector2(250,140)
		sp.rotation_degrees = -number(spec,"rotate")
		sp.scale = Vector2.ONE * number(spec,"scale",1) * float(spec.event_scale)
		sp.modulate.a = number(spec,"opacity",255)/255.0
		sp.z_index = clampi(int(number(spec,"z")), -4000,4000)
		if spec.blend == "pma":
			sp.normal_material = pma
			sp.multiply_material = multiply_pma
		stage.add_child(sp)
		sp.visible = false
		entries.append({"sprite":sp,"spec":spec})
	if attack == "ux":
		cutin = Sprite2D.new()
		cutin.z_index = 4095
		stage.add_child(cutin)
		for frame in config.cutin_frames:
			cutin_frames.append(load("res://RoswithaV2/"+frame.path))
		actor = SpineSprite.new()
		actor.skeleton_data_res = load("res://RoswithaV2/ArtWorks/roswitha/character/regent_skel_data.tres")
		actor.position = Vector2(-330,210)
		actor.scale = Vector2.ONE*.35
		actor.update_mode = 2
		stage.add_child(actor)
	if attack == "ug":
		intro = SpineSprite.new()
		intro.skeleton_data_res = load("res://RoswithaV2/assets/" + ("30134_ug" if config.variant == "base" else "30134_ug2") + ".tres")
		intro.normal_material = pma
		intro.update_mode = 2
		stage.add_child(intro)

	await get_tree().process_frame
	audio=AudioStreamPlayer.new()
	audio.stream=load("res://RoswithaV2/"+mode+".wav")
	add_child(audio)
	audio.play()
	started_usec=Time.get_ticks_usec()
	building=false
	running=true
	fit()
func fit()->void:
	if not is_instance_valid(stage): return
	var vp=get_viewport_rect().size
	stage.position=vp*.5
	stage.scale=Vector2.ONE*maxf(vp.x/1280.0,vp.y/720.0)
func _process(_delta: float)->void:
	if not running: return
	elapsed=(Time.get_ticks_usec()-started_usec)/1000000.0
	fit()
	sample_at(minf(elapsed,total))
func finish()->void:
	running=false
	hide()
	queue_free()
func sample_at(t: float)->void:
	# Only the authored middle needs blackout. The tail is an alpha overlay.
	if attack == "ug":
		blackout.color.a = clampf((t-.10)/.10,0,1) * (1.0-clampf((t-1.766)/.20,0,1))
	else:
		blackout.color.a = clampf((t-.65)/.12,0,1) * (1.0-clampf((t-float(config.return_at))/.25,0,1))
	if is_instance_valid(intro):
		intro.visible = t < .2333
		if intro.visible:
			var k := smoothstep(0.0,.2333,t)
			intro.position = Vector2(-330,160).lerp(Vector2.ZERO,k)
			intro.scale = Vector2.ONE*lerpf(.30,1.0,k)
			var state = intro.get_animation_state()
			state.set_animation("animation",false,0)
			state.update(.2333)
			state.apply(intro.get_skeleton())
			intro.update_skeleton(0)
	if is_instance_valid(cutin):
		cutin.visible = t >= .7667 and t < .7667+float(config.pause)
		if cutin.visible:
			var frame_index := 0
			while frame_index < config.cutin_frames.size()-1 and t-.7667 >= float(config.cutin_frames[frame_index].end):
				frame_index += 1
			cutin.texture = cutin_frames[frame_index]
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
			finish()
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
