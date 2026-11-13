extends Node2D
## Deterministic offline emitter sampling, independent of wall-clock/render speed.
var emitters: Array = []
var textures: Dictionary = {}
var elapsed := -1.0
var life := 1.0

func setup(spec: Dictionary) -> void:
	emitters = spec.emitters
	life = float(spec.life)
	for e in emitters: textures[e.texture] = load("res://assets/" + e.texture)
	var mat := CanvasItemMaterial.new()
	mat.blend_mode = CanvasItemMaterial.BLEND_MODE_ADD
	material = mat

func _draw() -> void:
	if elapsed < 0 or elapsed > life: return
	for ei in range(emitters.size()):
		var e: Dictionary = emitters[ei]
		if e.get("disable",false): continue
		var t := elapsed-float(e.get("startDelay",0))
		if t < 0: continue
		var rate := maxf(1,float(e.get("emissionRate",20)))
		var lifespan := maxf(.01,float(e.get("particleLifespan",.5)))
		var duration := float(e.get("duration",-1))
		var last_birth := t if duration < 0 else minf(t,duration)
		for i in range(maxi(0,int((t-lifespan*2)*rate)), int(last_birth*rate)+1):
			var rng := RandomNumberGenerator.new()
			rng.seed = ei*100000+i+42
			var age := t-i/rate
			var lt := maxf(.01,lifespan+rng.randf_range(-1,1)*float(e.get("particleLifespanVariance",0)))
			if age < 0 or age >= lt: continue
			var f := age/lt
			var angle := deg_to_rad(float(e.get("angle",0))+rng.randf_range(-1,1)*float(e.get("angleVariance",0)))
			var pos := Vector2(float(e.get("sourcePositionx",0))+rng.randf_range(-1,1)*float(e.get("sourcePositionVariancex",0)), -float(e.get("sourcePositiony",0))-rng.randf_range(-1,1)*float(e.get("sourcePositionVariancey",0)))
			if int(e.get("emitterType",0))==1:
				var radius := lerpf(float(e.get("maxRadius",0)),float(e.get("minRadius",0)),f)
				angle += deg_to_rad(float(e.get("rotatePerSecond",0)))*age
				pos += Vector2(cos(angle),-sin(angle))*radius
			else:
				var speed := float(e.get("speed",0))+rng.randf_range(-1,1)*float(e.get("speedVariance",0))
				pos += Vector2(cos(angle),-sin(angle))*speed*age + Vector2(float(e.get("gravityx",0)),-float(e.get("gravityy",0)))*age*age*.5
			var size := lerpf(float(e.get("startParticleSize",10)),float(e.get("finishParticleSize",0)),f)
			var color := Color(float(e.get("startColorRed",1)),float(e.get("startColorGreen",1)),float(e.get("startColorBlue",1)),float(e.get("startColorAlpha",1)))
			var end := Color(float(e.get("finishColorRed",1)),float(e.get("finishColorGreen",1)),float(e.get("finishColorBlue",1)),float(e.get("finishColorAlpha",0)))
			color = color.lerp(end,f)
			draw_texture_rect(textures[e.texture],Rect2(pos-Vector2.ONE*size*.5,Vector2.ONE*size),false,color)
