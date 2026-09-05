# MeiLin 特效实现方法清单

本文记录 MeiLinMod 后续复现 `E:\DATA\GODOT\res\1027\effect` 特效的处理流程。目标是兼顾制作复杂度、运行时流畅度、位移兼容和动画兼容。

## 结论

推荐方案：

```text
制作/转换阶段：参考千鹤，解析 .cfx/.particle，生成 Godot .tscn
运行时播放阶段：参考 Yuki/Fei，只加载 .tscn、播放 Spine/粒子、自动清理
技能整体节奏：参考 Fei，用 coordinator 控制攻击、命中、目标特效、震屏、声音
```

不建议最终在战斗运行时解析 `.cfx` 或 `.particle`。复杂 XML 解析、粒子材质构建、节点层级生成都应尽量提前完成，否则大特效首次播放容易卡顿。

## 已确认资源

主资源目录：

```text
E:\DATA\GODOT\res\1027\effect
```

当前资源类型概况：

| 类型 | 数量 | 用途 |
|---|---:|---|
| `.cfx` | 55 | 复合特效层级配置 |
| `.particle` | 32 | Cocos 粒子发射器配置 |
| `.json` | 97 | Spine JSON 数据 |
| `.atlas` | 97 | Spine 贴图图集 |
| `.png` | 97 | Spine/粒子贴图 |
| `.import` | 194 | Godot 导入文件 |

可复用通用位移素材：

```text
E:\DATA\GODOT\MyMod\YukiMod\YukiMod\ArtWorks\modspine\effect\tongyong
```

其中包含：

```text
step_player_move
step_player_arrive
step_target_move
step_target_arrive
```

这几组适合用于瞬移、贴近敌人、回位、目标受击移动等通用步法/位移特效。它们是 `.skel/.atlas/.png/_skel_data.tres`，可以直接复制后制作 MeiLin 自己的 `.tscn` 包装场景。

可参考的粒子生成脚本：

```text
E:\DATA\GODOT\res\gen_u3_buff_particles.py
```

这个脚本用于 Fei 的 `u3_buff_play`，它的做法是：

- 读取 `.particle` plist XML。
- 手写 `CFX_LAYERS`，记录 CFX 中的 `source/format/z/x/y/scale/rotate`。
- 将每个 emitter 生成一个 `GPUParticles2D` 节点。
- 生成 `ParticleProcessMaterial`、`CanvasItemMaterial`、`Gradient`、`CurveTexture` 等 sub_resource。
- 将 `.sct` 贴图通过 `TEXTURE_PATHS/TEXTURE_UIDS/TEXTURE_SIZES` 映射到 Godot PNG 资源。
- 从现有 `.tscn` 读取 Spine ext_resource 和 Spine 节点，再把粒子节点合并写回场景。

它不是完整通用 CFX 转换器，因为 CFX layer 是手写在脚本里的；但它提供了一个可复用方向：先把 CFX layer 信息结构化，再用脚本生成 `.tscn`。

## CFX、Spine、Particle 的关系

### CFX 是什么

`.cfx` 是复合特效配置文件，本质是 plist XML。它不包含真正的动画数据，也不包含粒子运动参数。它只描述一个特效由哪些层组成，以及这些层怎么摆放和什么时候播放。

常见字段：

| 字段 | 作用 |
|---|---|
| `source` | 引用某个 Spine 特效或 `.particle` 文件 |
| `format` | `spine`、`particle` 或少量 `sprite` |
| `x/y` | 层偏移，导入 Godot 时 `y` 取反 |
| `z` | 层级 |
| `scale` | 层缩放 |
| `rotate` | 层旋转 |
| `delay` | 层启动延迟，单位 ms |
| `ani` | Spine 动画名覆盖 |
| `disable` | 禁用该层 |

因此 `.cfx` 更像“导演表”或“合成表”。

### Spine 动画是否需要 CFX

不一定。

单个 Spine 特效可以不经过 CFX，直接做成：

```text
xxx.skel / xxx.json
xxx.atlas
xxx.png
xxx_skel_data.tres
xxx.tscn
```

运行时直接播放 `xxx.tscn` 中的 Spine 动画即可。

只有当需要还原原版复合特效时，Spine 才需要通过 CFX 参与合成。例如一个技能同时有前景层、背景层、屏幕层、粒子层，并且每层有不同 z、delay、scale，这时就需要读 `.cfx`。

判断规则：

| 场景 | 是否需要 CFX |
|---|---|
| 只播放一个 Spine 动画 | 不需要 |
| 只播放一个已做好的 `.tscn` | 不需要 |
| 多个 Spine 层要按 z/offset 合成 | 需要 |
| Spine + 粒子要按原版位置和 delay 合成 | 需要 |
| 只想做近似基础效果 | 可以不用，手写一个简单 `.tscn` |

### 粒子是否需要 CFX 和 Particle 文件

粒子的核心参数在 `.particle` 文件里，CFX 不是粒子参数来源。

`.particle` 负责：

- emitter 数量
- 发射角度
- 速度
- 生命周期
- 大小
- 旋转
- 颜色渐变
- 混合模式
- 贴图

`.cfx` 负责：

- 这个粒子层放在哪里
- 层级是多少
- 是否旋转/缩放
- 延迟多久出现
- 是否禁用
- 是否和 Spine 层一起组成完整特效

所以：

| 目标 | 需要 `.particle` | 需要 `.cfx` |
|---|---:|---:|
| 复刻某个粒子发射器本身 | 是 | 否 |
| 把粒子放进完整原版复合特效 | 是 | 是 |
| 只想临时做一个近似粒子 | 否，可以手写 GPUParticles2D | 否 |
| 复刻 `meirin_1027_skill_2_play.cfx` | 是，因其引用 `meirin_1027_skill_2_play_pati1` | 是 |

基础特效阶段可以按这个优先级处理：

```text
单独 Spine：不走 CFX
单独粒子：只用 .particle
原版复合效果：.cfx + 被引用的 Spine + 被引用的 .particle
```

## Yuki / 千鹤 / Fei 的差异

### Yuki

Yuki 的特效核心是“场景播放器”：

- 运行时加载已经做好的 `.tscn`。
- 用 `MegaSprite` 播指定动画。
- 一次性特效监听 `AnimationCompleted`，播完释放。
- 持续特效用 `DurationSeconds` 或 `OutAnim` 控制退出。
- 投射物用 Tween 从起点移动到终点。

优点：

- 运行时逻辑少。
- 播放稳定。
- 适合 one-shot、follow-loop、projectile 这类已经做成场景的特效。

不足：

- 不解析 `.cfx`。
- 不自动处理 `.particle`。
- 不适合直接还原 MeiLin 当前的原始 effect 目录。

### 千鹤

千鹤的特效核心是“复合特效构建器”：

- 解析 `.cfx` primitive。
- 解析 `.particle` emitter。
- 根据 `source/format/x/y/z/scale/rotate/delay` 生成节点。
- Spine、粒子、sprite 都能作为 layer 合成。

优点：

- 最适合 MeiLin 当前这种 `.cfx + .particle + spine` 原始资源。
- 能还原复合层级、delay、z 序和粒子。

不足：

- 如果运行时解析和构建，首次播放成本较高。
- 复杂 particle 映射需要大量校准。

### Fei

Fei 的特效核心是“预制场景 + 时间轴协调器”：

- 特效先转成 `.tscn`。
- 运行时实例化 `.tscn`。
- `FeiVfxHelper` 触发 `GPUParticles2D`，播放 Spine。
- `AttackCoordinator/PowerCoordinator/DefenseCoordinator` 控制整体时机。

优点：

- 运行时流畅。
- 攻击、命中、目标特效、震屏、hitstop 可以精确控制。
- 适合最终版本。

不足：

- 需要先制作或转换出 `.tscn`。
- 每个复杂技能仍然需要一个 coordinator 或 timeline 配置。

## 目标目录建议

建议不要直接引用 `E:\DATA\GODOT\res\1027\effect`，因为导出 PCK 时不会包含外部绝对路径。应复制或生成到项目内部。

推荐结构：

```text
MeiLinMod/
  vfx_configs/
    1027/
      raw/
        *.cfx
        *.particle
      texture_map.json
  spine/
    effect/
      <effect_name>/
        *.skel 或 *.json
        *.atlas
        *.png
        *_skel_data.tres
  scenes/
    vfx/
      <effect_name>/
        <effect_name>.tscn
  ArtWorks/
    modspine/
      effect/
        tongyong/
          step_player_move/
          step_player_arrive/
          step_target_move/
          step_target_arrive/
```

如后续使用 `addons/auto_spine_skel_data`，Spine 文件建议优先整理为：

```text
xxx.skel
xxx.atlas
xxx.png
```

这样插件可以自动生成：

```text
xxx_skel_data.tres
```

如果只有 Spine JSON，需要先验证当前 Spine GDExtension 是否能作为 `SpineSkeletonFileResource` 直接加载 JSON。若不能，先批量转 `.skel`。

## 实现阶段清单

### 阶段 1：资源归档

- [ ] 将 `E:\DATA\GODOT\res\1027\effect` 中需要的 `.cfx/.particle/.json/.atlas/.png` 复制到项目内。
- [ ] 不要复制 `.import` 作为权威资源；导入后让 Godot 自己生成项目内 import。
- [ ] 将 Yuki `tongyong` 四组通用步法资源复制到项目内。
- [ ] 检查 `.particle` 中引用的 `.sct` 贴图，建立 `.sct -> .png` 映射。
- [ ] 检查是否存在重名贴图、重名 Spine、大小写差异。
- [ ] 确认项目只保留一份 Spine GDExtension，优先保留 `addons/spine`。
- [ ] 如果已经导入 `addons/spine`，旧的 `bin/spine_godot_extension.gdextension` 必须删除或改名停用；否则 Godot 会重复注册 Spine 类，导致 `.skel` 导入不稳定。
- [ ] 停用旧 `bin` 后，如果 Godot 仍尝试加载 `res://bin/spine_godot_extension.gdextension`，检查本地 `.godot/extension_list.cfg`，清掉旧路径后重新打开/导入项目。

### 阶段 2：Spine 资源准备

- [ ] 对每个 `format=spine` 的 CFX layer，确认存在同名 Spine 资源。
- [ ] 如果资源是 `.json + .atlas + .png`，验证是否能直接生成 `.tres`。
- [ ] 如果不能直接加载 JSON，批量转为 `.skel`。当前 MeiLin/Fei 验证路径建议显式输出 Spine `4.2.11`，例如：

```powershell
MeiLinMod\spine\SpineSkeletonDataConverter.exe input.json output.skel -v 4.2.11
```

- [ ] 如果 `.skel.import` 出现 `valid=false`，先确认不是 Spine GDExtension 重复加载；修复后删除或移开失败的 `.skel.import` 与对应 `.godot/imported/*.md5`，再让 Godot 重新导入。
- [ ] 生成或手工确认 `*_skel_data.tres`。
- [ ] 为每个 Spine 特效制作最小 `.tscn`：

```text
Node2D
  SpineSprite
```

- [ ] 默认动画名使用 `animation`。
- [ ] 如果 CFX 中有 `ani` 字段，使用指定动画，例如 `limited_break`。
- [ ] 确认 SpineSprite 的原点和缩放，不要把位移写死到子节点里，优先由 CFX layer 控制。

### 阶段 3：Particle 转换

每个 `.particle` 可能包含多个 emitter。每个 emitter 应生成一个独立 `GPUParticles2D`。

基础映射：

| `.particle` | Godot |
|---|---|
| `emitterType=0` | `ParticleProcessMaterial.EMISSION_SHAPE_BOX` |
| `emitterType=1` | `ParticleProcessMaterial.EMISSION_SHAPE_RING` |
| `sourcePositionx/y` | emitter 节点位置 |
| `sourcePositionVariancex/y` | 发射区域尺寸，方差乘 2 |
| `angle/angleVariance` | `direction/spread` |
| `speed/speedVariance` | `initial_velocity_min/max` |
| `particleLifespan` | `lifetime` |
| `rotationStart/rotationDir` | 初始角度/角速度 |
| `colorBlends` | `Gradient` |
| `blendFuncSource=770, blendFuncDestination=1` | Add 混合 |

注意项：

- [ ] `GPUParticles2D` 大小写必须正确。
- [ ] 一次性特效使用 `one_shot=true`。
- [ ] 播放时调用 `Restart()` 并设置 `Emitting=true`。
- [ ] `colorBlends` 有两种格式，颜色不对时优先检查 Alpha 和 RGB 顺序。
- [ ] `.sct` 贴图必须映射到 Godot 可加载的 `.png/.tres`。
- [ ] 半径模式不要做成静态 ring，需要给径向速度，否则粒子会贴环不动。
- [ ] 粒子 size 要按纹理尺寸换算为 scale。

### 阶段 4：CFX 转场景

将每个 `.cfx` 转为一个复合特效 `.tscn`。

CFX layer 字段处理：

| CFX 字段 | Godot 处理 |
|---|---|
| `source` | 查找 Spine scene 或 particle scene |
| `format=spine` | 实例化 Spine layer |
| `format=particle` | 实例化 particle layer |
| `x` | `position.x = x` |
| `y` | `position.y = -y` |
| `z` | `z_index = z` |
| `scale` | 支持 `1` 和 `1.2,1` 两种格式 |
| `rotate` | `rotation_degrees = rotate` |
| `delay` | 毫秒转秒，延迟显示/播放 |
| `ani` | 覆盖默认动画名 |
| `disable=true` | 默认跳过，必要时保留为 disabled layer |

复合场景建议结构：

```text
Node2D <effect_name>
  Node2D Layers
    Node2D <source>_zXXXX
      SpineSprite 或 GPUParticles2D...
```

延迟方案：

- 简单方案：每个 delayed layer 附加 `delayed_vfx_layer.gd`，到时间后 visible=true 并触发子粒子/Spine。
- C# 方案：根节点附加 `MeiLinCompositeVfxRoot`，读取 exported delay 列表，统一调度。
- 最终建议：生成 `.tscn` 时已经写入 delay 元数据，运行时由 C# helper 统一触发，便于日志和清理。

## 运行时播放清单

### MeiLinVfxHelper

参考 Yuki/Fei，实现一个通用 helper：

- [ ] `Preload(scenePath)`
- [ ] `Instantiate(scenePath, parent, globalPos, scale, zIndex)`
- [ ] `PlaySpine(root, animName)`
- [ ] `TriggerParticles(root)`
- [ ] `PlayComposite(root)`：处理 layer delay、Spine、particle
- [ ] `AutoFree(root)`：根据 Spine 完成、粒子 lifetime、或显式 duration 清理
- [ ] `FollowCreature(root, creatureNode, offset)`：需要跟随时每帧同步 `VfxSpawnPosition`

一次性特效释放策略：

```text
Spine only：所有 Spine 动画完成后释放
Particle only：最大 delay + particle lifetime + preprocess + 安全余量后释放
Spine + Particle：取两者最大结束时间
手写 duration：优先 duration
```

### MeiLinTimelineCoordinator

复杂技能不要直接在卡牌里散写多个 delay。建议按 Fei 风格做 coordinator：

```text
MeiLinAttackCoordinator
MeiLinSkill2Coordinator
MeiLinSkillXCoordinator
MeiLinPowerVfxCoordinator
```

职责：

- [ ] 找 caster/target 的 `NCreature`。
- [ ] 根据 `VfxSpawnPosition` 放置自机特效和目标特效。
- [ ] 控制何时播放 ready/play/impact/end。
- [ ] 控制伤害点或命中点。
- [ ] 控制震屏、hitstop、音效。
- [ ] 多段命中时避免重复播放完整时间轴。
- [ ] 结束后清理所有生成节点。

等待方式：

- 战斗演出中优先使用 `Cmd.CustomScaledWait` 或 Godot `SceneTreeTimer`。
- 不建议大量使用 `Task.Delay`，否则暂停、倍速、慢动作时可能不同步。

## 位移兼容清单

位移是最容易和特效错位的部分，需要单独处理。

### 坐标和锚点

- [ ] 自机特效默认锚点：`casterNode.VfxSpawnPosition`。
- [ ] 目标特效默认锚点：`targetNode.VfxSpawnPosition`。
- [ ] 大范围屏幕特效锚点：`room.CombatVfxContainer` 或屏幕中心。
- [ ] CFX 的 `y` 必须取反。
- [ ] 如果角色左右站位不同，需要处理 X 翻转或保持特效朝向。
- [ ] Follow 特效不要挂在会被角色缩放/旋转影响的错误父节点下。

### 角色移动与特效跟随

推荐区分三类：

1. 跟随角色的特效
   - 自机蓄力 aura、脚底光、身上火焰。
   - 使用 follow component，每帧同步 `VfxSpawnPosition`。

2. 固定在世界位置的特效
   - 斩击残影、地面爆炸、目标受击光。
   - 出现后不再跟随角色。

3. 位移过程特效
   - 使用 Yuki `tongyong` 的 `step_player_move/arrive` 等素材。
   - 位移开始、到达、目标被击退分别播放。

### Yuki 通用步法素材用法

建议复制并包装为 MeiLin 自己的场景：

```text
MeiLinMod/ArtWorks/modspine/effect/tongyong/step_player_move/...
MeiLinMod/ArtWorks/modspine/effect/tongyong/step_player_arrive/...
MeiLinMod/ArtWorks/modspine/effect/tongyong/step_target_move/...
MeiLinMod/ArtWorks/modspine/effect/tongyong/step_target_arrive/...
```

再生成：

```text
MeiLinMod/scenes/vfx/tongyong/step_player_move_b.tscn
MeiLinMod/scenes/vfx/tongyong/step_player_move_f.tscn
MeiLinMod/scenes/vfx/tongyong/step_player_arrive_b.tscn
MeiLinMod/scenes/vfx/tongyong/step_player_arrive_f.tscn
...
```

使用建议：

- `move`：位移开始位置播放。
- `arrive`：位移结束位置播放。
- `target_move`：目标被拉动/击退时播放。
- `target_arrive`：目标到达或受击落点播放。
- 动画名先按 Yuki 的 `eff_b/eff_f` 约定检查；如果实际不同，以 Spine 动画列表为准。

## 动画兼容清单

### Spine 动画名

- [ ] 默认特效动画尝试 `animation`。
- [ ] CFX `ani` 字段优先。
- [ ] 通用步法素材检查 `eff_b/eff_f`。
- [ ] 角色攻击动画按 MeiLin 实际资源检查，例如 `attack_ready/attack_play/attack_end`、`skill_2_*`、`limited_break`。
- [ ] 缺动画时只 warn 一次，不能让战斗报错中断。

### Spine JSON / SKEL

- [ ] 确认当前 `addons/spine` 能否加载 JSON Spine。
- [ ] 若不能加载 JSON，必须批量转 `.skel`。
- [ ] 自动生成 `.tres` 前，保证 `.skel/.atlas/.png` 同名同目录。
- [ ] 不要依赖 `res://.godot/imported/*.spskel` 作为长期源路径，迁移或导出时容易失效。

### 动画速度

- [ ] 普通特效速度默认 1.0。
- [ ] 需要慢放/加速时，优先集中在 coordinator 或 runtime manager 中处理。
- [ ] 可用 `spine_speed_inspector` 仅作为编辑器调试工具，不要把预览 meta 当作正式 runtime 数据依赖。

## 优先转换对象

建议按复杂度从低到高推进。

### 第一批：验证转换链路

```text
meirin_1027_skill_2_play.cfx
```

原因：

- 只有两个 Spine layer 和一个 particle layer。
- 适合验证 CFX 解析、Spine 播放、particle 转换、z 序、rotate。

### 第二批：技能 2 完整链路

```text
meirin_1027_skill_2_ready.cfx
meirin_1027_skill_2_play.cfx
meirin_1027_skill_2_impact.cfx
meirin_1027_skill_2_impact2.cfx
meirin_1027_skill_2_burst.cfx
meirin_1027_skill_2_end.cfx
meirin_1027_skill_2_screen.cfx
meirin_1027_skill_2_wing.cfx
```

目标：

- 建立 `MeiLinSkill2Coordinator`。
- 分清自机、目标、屏幕层特效。
- 校准伤害点和粒子颜色。

### 第三批：Skill X / 大招

```text
meirin_1027_skill_x_eff_1.cfx
```

注意：

- layer 多。
- delay 多。
- 有 `limited_break` 动画。
- 有 disabled particle layer。
- 需要单独处理屏幕层、cut-in、目标特效、震屏。

### 第四批：常规攻击和强化攻击

```text
meirin_attack_play1_root.cfx
meirin_attack_play2_root.cfx
meirin_attack_play_target.cfx
meirin_strong_attack_play_root.cfx
meirin_unique_strong_attack_play_root.cfx
```

目标：

- 建立 `MeiLinAttackCoordinator`。
- 如需要贴近敌人或回位，接入 Yuki `tongyong` 步法特效。

## 转换工具建议

可以在项目内建立：

```text
tools/vfx/
  convert_cfx_to_tscn.ps1 或 convert_cfx_to_tscn.cs
  convert_particle_to_scene.cs
  texture_map.json
```

转换工具职责：

- [ ] 扫描 `.cfx`。
- [ ] 解析 primitive。
- [ ] 为 Spine layer 查找或生成 scene。
- [ ] 为 particle layer 生成 `GPUParticles2D` scene。
- [ ] 生成复合 `.tscn`。
- [ ] 输出缺失资源报告。
- [ ] 输出需要人工校准的颜色/贴图报告。

生成报告建议包含：

```text
missing_spine_source
missing_particle_source
missing_texture_sct
json_spine_needs_skel_conversion
disabled_layers
unknown_color_blends_format
large_texture_warning
```

### `gen_u3_buff_particles.py` 可复用点

`E:\DATA\GODOT\res\gen_u3_buff_particles.py` 可以作为第一版转换器的参考，但不建议直接照搬硬编码路径。

可复用设计：

- `parse_plist_dict / parse_plist_value`：plist XML 解析。
- `parse_particle`：读取 `.particle`。
- `ResourceGen`：统一管理 ext_resource、sub_resource、node 文本。
- `make_gradient`：将 `colorBlends` 转成 `Gradient + GradientTexture1D`。
- `make_scale_curve`：将起止粒子尺寸转成 `CurveTexture`。
- `make_process_material`：生成 `ParticleProcessMaterial`。
- `make_particle_nodes`：一个 emitter 生成一个 `GPUParticles2D`。
- 读取现有 `.tscn`，保留 Spine ext_resource 和已有 Spine 节点。
- 写出完整 `.tscn` 前，按 z 排序合并 Spine 节点和粒子节点。

需要改成通用化的点：

- `PROJECT_ROOT`、`EFFECT_DIR`、`SCENE_PATH` 不能写死。
- `TEXTURE_PATHS/TEXTURE_UIDS/TEXTURE_SIZES` 应改为外部 `texture_map.json`。
- `CFX_LAYERS` 不能手写，应由 `.cfx` 解析生成。
- `delay = 33 if "pat1" in src else 700` 这种规则不能保留，应读取 `.cfx` 的 `delay`。
- 输出路径要按 MeiLin 目录约定生成。
- Fei 专用命名、uid、scale 默认值要去掉。
- 若没有现有 Spine `.tscn`，工具应能创建新的 Spine wrapper scene。

## 验证流程

每个特效转换后按以下顺序验收：

1. Godot 编辑器能打开 `.tscn`，无 missing resource。
2. SpineSprite 节点可见，能播放指定动画。
3. GPUParticles2D 能 `Restart()` 后显示。
4. CFX `y` 取反正确。
5. z 序正确：背后层不盖住前景层。
6. Add 混合正确，发光不发黑。
7. 粒子颜色接近原版。
8. delay 层按预期出现。
9. 运行时播放不报错，播完能释放。
10. 多次播放不残留节点。
11. 暂停/倍速/慢动作时不明显错位。
12. PCK 导出后仍能加载。

## 当前需要补齐的信息

- [ ] `.particle` 中所有 `.sct` 贴图的实际 PNG 对应关系。
- [x] `meirin_1027_skill_2_play` 的 Spine JSON 已能通过 `SpineSkeletonDataConverter.exe -v 4.2.11` 转为 Godot 可导入 `.skel`。
- [ ] Spine JSON 是否能直接被当前 GDExtension 加载；基础流程不依赖这个能力。
- [x] MeiLin 原始技能时间轴文件已找到：`E:\DATA\GODOT\res\1027\model_data\1027.srmd`。
- [x] MeiLin 技能命令组已找到：`E:\DATA\GODOT\res\1027\model_data\1027.srcs`。
- [ ] Skill X 中 `limited_break` 对应的角色/特效 Spine 场景是否已齐全。
- [ ] Yuki `tongyong` 动画名和 MeiLin 场景包装的命名约定。

## 最小落地路径

建议最小可行版本按这个顺序做：

1. 保留 `addons/spine`，移除或停用旧 `bin` Spine GDExtension。
2. 复制 `meirin_1027_skill_2_play.cfx` 相关资源到项目内。
3. 转换两个 Spine layer 和一个 particle layer。
4. 生成 `MeiLinMod/scenes/vfx/generated/meirin_1027_skill_2_play/meirin_1027_skill_2_play.tscn`。
5. 写 `MeiLinVfxHelper.PlayComposite(scenePath, position)`。
6. 在测试命令或临时卡牌里播放该场景。
7. 验证无卡顿、无残留、z/rotate/particle 正确。
8. 再扩展到 `skill_2` 完整链路。
9. 最后做 `skill_x` 和攻击位移。

当前试点已完成第 1-5 步的基础资源和代码，`dotnet build MeiLinMod.csproj` 已通过。第 6 步开始需要接入具体测试入口或卡牌触发点。

## 当前批量落地状态

本轮已先不接入具体卡牌，只完成基础特效资源、场景和通用播放能力。

已生成内容：

| 类型 | 数量 | 位置 |
|---|---:|---|
| CFX 复合场景 `.tscn` | 54 | `MeiLinMod/scenes/vfx/generated/<技能组>/<effect>.tscn` |
| Spine 数据 `.tres` | 86 | `MeiLinMod/spine/effect/generated/<source>/*_skel_data.tres` |
| Spine `.skel.import` | 86 | `MeiLinMod/spine/effect/generated/<source>/*.skel.import` |
| 粒子贴图 `.png` | 13 | `MeiLinMod/images/vfx/particles` |
| 复制入项目的 `.particle` | 20 | `MeiLinMod/vfx_configs/1027/generated` |
| 复制入项目的 `.cfx` | 54 | `MeiLinMod/vfx_configs/1027/generated` |
| SRMD 命令拼接配置 `.json` | 1 | `MeiLinMod/vfx_configs/1027/generated/meilin_vfx_commands.json` |

已完成的代码能力：

- `tools/generate_meilin_vfx.py` 会扫描 `E:\DATA\GODOT\res\1027\effect`，跳过 `skill_x`，把其余 CFX 转为 Godot `.tscn`。
- 生成器会读取 `E:\DATA\GODOT\res\1027\model_data\1027.srmd`，把命令中的 effect 引用补齐；当前已从 `E:\DATA\GODOT\res\effect` 自动补入 `common_hit_eff`。
- 生成器会读取 `E:\DATA\GODOT\res\1027\model_data\1027.srcs`，输出 `u2_attack/u3_buff/u4_buff/u1_buff/ug_attack/ux_buff` 等命令组。
- `meilin_vfx_commands.json` 已包含每个 command 的角色动画、特效列表、命中点、震屏、hitstop、位移和 close-combat 信息，后续 coordinator 应优先消费这个文件，而不是在卡牌里散写 delay。
- Spine JSON 会通过 `MeiLinMod/spine/SpineSkeletonDataConverter.exe -v 4.2.11` 转为 `.skel`。
- `.particle` 会被转换为场景内的 `GPUParticles2D` 和对应 `ParticleProcessMaterial`。
- CFX layer 的 `x/y/z/scale/rotate/delay/opacity/ani` 已进入生成结果，其中 `y` 按 Godot 坐标取反。
- 延迟 layer 会写入 `metadata/meilin_vfx_delay_sec`，运行时由 `MeiLinVfxHelper` 统一隐藏、延迟显示、触发 Spine/粒子。
- `MeiLinVfxHelper` 会优先读取每个 SpineSprite 的 `preview_animation`，用于播放 CFX `ani` 指定的动画；没有指定时回退到默认 `animation`。
- `MeiLinCommandVfxCoordinator` 已能读取 `meilin_vfx_commands.json`，按 command 的 effect 列表播放 SELF/TARGET/SCREEN/FOR_CENTER 特效，处理 command 级 delay、scale、rotation、zOrder 和 ATTACH 跟随。
- `MeiLinCommandVfxCoordinator` 已提供 Fei 风格的时间轴接口：`PlayCommandTimelineAsync` 播单个 command，`PlayCommandSetTimelineAsync` 串 ready/play_ready/play/end，`PlayCommandSetUntilFirstHitAsync` 在首个 hit 点返回，方便卡牌逻辑与表现对齐。
- 技能牌默认在 `BeforeCardPlayed` 中播放 `debuff_ready -> debuff_play`；特定卡 `AttackDefenseUnity`、`ShenGongFangYiTi` 覆盖为 `u3_buff`。
- Power 牌继续通过 `PlayPowerCastAnim()` 播放表现；默认使用 `u4_buff`，`FireDragonGem` 覆盖为 `u1_buff`。
- 攻击牌在 `BeforeCardPlayed` 记录 caster/target，现有战斗动画 patch 播放 `attack_play1/attack_play2` 交替序列，并同步按 command duration 播放对应特效。超过 3 hit 的多段攻击最后一段使用 `u2_attack_play`，随后接 `u2_attack_end`。
- 旧的单卡试点入口 `PlaySkill2AtCreature`、`Skill2PlayScenePath` 和手写 `meirin_1027_skill_2_play` 目录已移除，当前只保留通用播放接口与批量生成产物。

验证状态：

- `dotnet build MeiLinMod.csproj` 通过，0 warning / 0 error。
- Godot 导入后，生成的 Spine `.skel.import/.atlas.import` 未发现 `valid=false`。
- 已删除旧 `bin` Spine GDExtension，当前使用 `addons/spine`。
- `rg 'valid=false|load_failed|missing_resource|resource_path=""'` 扫描生成目录未发现明显坏引用。

当前仍是近似复刻的地方：

- `scale` 或 `rotate` 如果是随机范围，目前取平均值，不做每次播放随机。
- `.particle` 到 `GPUParticles2D` 的映射是基础版，颜色、发光、发射形状、重力和尺寸接近原始配置，但不是逐字段完全等价。
- CFX 的 `attach`/骨骼挂点尚未实现，当前所有 layer 都按 CFX 坐标放在复合场景里。
- `skill_x` 已按约定跳过；因此 `meilin_vfx_commands.json` 中剩余缺失场景只有 `meirin_1027_skill_x_eff_1` 和 `meirin_1027_skill_x_eff_screen`。
- `1027.srmd` 中存在 `SCREEN`、`FOR_CENTER`、`TARGET` 这类空 `file_name` 的逻辑事件；它们不是资源缺失，当前 coordinator 会跳过空资源事件，后续需要在接大范围屏幕层时按 `type` 解释为屏幕层、中心层或目标锚点。
- 当前 command coordinator 已能触发角色模型动画、首个 hit 等待和特效播放；实际伤害仍由卡牌/原游戏命令执行，震屏、hitstop 和 close-combat 位移均已接入。

## 当前从 model_data 拼出的时序结论

`1027.srcs` 是技能命令组入口，负责把 ready、loop、play_ready、play、end 串起来。例如：

- `u2_attack`：`attack_ready` -> `normal_attack` -> `u2_attack_ready` -> `u2_attack_play` -> `u2_attack_end`。
- `u3_buff`：`u3_buff_ready` -> `normal_attack` -> `u3_buff_play` -> `u3_buff_end`。
- `u4_buff`：`u4_buff_ready` -> `normal_attack` -> `u4_buff_play` -> `u4_buff_end`。
- `u1_buff`：`u1_buff_ready` -> `u1_buff_play` -> `u1_buff_end`。

`1027.srmd` 是实际战斗表现表，里面的 command 同时描述：

- 角色 Spine 动画：`ani`，含 `animation_name/duration/loop/delay`。
- 特效：`effect`，含 `file_name/type/bone_name/scale/position/angle/z/delay`。
- 命中：`hit`，例如普通攻击多在 66ms 或 416ms 触发。
- 震屏：`shake`。
- hitstop：`stop`，区分 `WEAK/STRONG/FINISH`。
- 位移：`move` 和 `close_combat`。

当前运行时逻辑拆成两层：

1. `MeiLinVfxHelper` 继续只负责播放单个已生成 `.tscn`，处理 delay、Spine、粒子、follow 和自动释放。
2. `MeiLinCommandVfxCoordinator` 读取 `meilin_vfx_commands.json`，按 command 实例化多个 effect，并按 `ani` 播放角色动画、按 `hit.delay/motion_delay` 对齐卡牌逻辑。
3. 战斗动画 patch 保留原有攻击动画队列，同时只在旁路播放攻击特效，避免和 `DamageCmd.Attack` 的伤害流程冲突。攻击序列规则为：1 hit = `attack_play1`；2 hit = `attack_play1, attack_play2`；3 hit = `attack_play1, attack_play2, attack_play1`；超过 3 hit = 前面 `1/2` 交替，最后 `u2_attack_play` 收尾。
4. coordinator 已按配置触发震屏和 hitstop；攻击 patch 通过 `MeiLinAttackMovementController` 处理 close-combat 前移、绘制层快照与回位恢复。

## 运行时硬化约定

- 每条 command set / command sequence 共用一个按施法者区分的 timeline generation。新的时间轴启动后，旧时间轴尚未到达的命中回调、结束动画和回待机操作必须失效；已经生成的纯视觉尾迹可以自然播放完。
- 攻击伤害仍由原版 `AttackCommand` 执行。`MeiLinTriggerAnimPatch` 只在配置的首个 hit delay 到达后结束 `TriggerAnim` 等待，因此伤害点已经与视觉命中拍对齐，不再额外叠加 `BeforeDamage` 闸门。
- 启动预热必须输出汇总结果：去重后的请求数、成功加载数和失败数。缺失资源保留逐路径日志，汇总用于判断是否值得对高频场景进一步做战斗上下文深度预热。
- 高频攻击和位移场景还会在 `NCombatRoom._Ready` 后执行战斗上下文深度预热：先等两帧，再逐场景以 `0.001` 根节点透明度走真实实例化/播放入口，保留两帧用于提交首帧绘制，然后释放并让出一帧。任务绑定房间实例与 generation；切换战斗、关闭战斗特效或容器失效时，旧任务立即停止，避免跨房间异步污染。
