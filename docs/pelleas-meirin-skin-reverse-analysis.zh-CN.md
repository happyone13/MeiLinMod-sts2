# Pelleas 梅铃皮肤逆向分析

分析日期：2026-11-14。对象为本机已订阅的二进制版本，不代表工坊后续版本。

## 1. 找到的订阅

| 项目 | 梅铃皮肤 | 共享依赖 |
| --- | --- | --- |
| 名称 | Meirin Watcher Skin | CznCore |
| 作者 | Pelleas | Pelleas |
| 本机版本 | 1.1.2 | 1.1.2 |
| 工坊 ID | 3789216478 | 3826011578 |
| 清单最低游戏版本 | 0.111.0 | 0.111.0 |
| PCK 大小 | 229,163,299 字节 | 124,604,670 字节 |

本机目录：

```text
D:\STEAM\steamapps\workshop\content\2868840\3789216478
D:\STEAM\steamapps\workshop\content\2868840\3826011578
```

梅铃清单还依赖 Boninall 的 Watcher，最低版本 0.9.24。本机 Watcher 为 0.10.11。
这是 Watcher 的表现替换皮肤，清单标记 `affects_gameplay: false`；
我们的 MeiLinMod 是独立角色，战斗机制、存档和内容注册的复杂度不能直接比较。
清单最低版本也不能证明该皮肤已经通过游戏 0.113 的运行验证。

## 2. 分析方式与产物

- 静态反编译皮肤及 CznCore DLL，追踪实际注册和调用路径。
- 读取 Godot PCK v3 目录，提取文本资源并逐文件核对包内 MD5。
- 没有加载或执行对方 DLL，没有修改工坊文件，没有进行帧耗时实测。
- 反编译产物保存在被 Git 忽略的 `tmp/pelleas-meirin-analysis/`。
- 皮肤有 1,566 个包内条目，提取文本 862 个；核心有 775 个条目，提取文本 388 个。
  条目包含导入缓存与源资源，不能视为独立素材数或显存占用。
- 皮肤反编译存在缺失引用造成的 IL 提示；参数含义以核心类型定义和实际调用交叉确认。

主要入口：

| 内容 | 本地分析产物 |
| --- | --- |
| 皮肤入口和补丁 | [Entry.cs](../tmp/pelleas-meirin-analysis/MeirinWatcherSkin.decompiled/MeirinWatcherSkin/Entry.cs) |
| 角色配置 | [MeirinSkinProfile.cs](../tmp/pelleas-meirin-analysis/MeirinWatcherSkin.decompiled/MeirinWatcherSkin/Characters/Meirin/MeirinSkinProfile.cs) |
| 普通动作与特效表 | [MeirinCombatPresentationProfile.cs](../tmp/pelleas-meirin-analysis/MeirinWatcherSkin.decompiled/MeirinWatcherSkin/Characters/Meirin/MeirinCombatPresentationProfile.cs) |
| 共享特效引擎 | [CharacterEffectEngine.cs](../tmp/pelleas-meirin-analysis/CznCore.decompiled/CznCore/Presentation/CharacterEffectEngine.cs) |
| 特效实例池 | [CombatTransientEffectPoolRuntime.cs](../tmp/pelleas-meirin-analysis/CznCore.decompiled/CznCore/Presentation/CombatTransientEffectPoolRuntime.cs) |
| UG/UX 原始脚本 | [combat_ug_ux_overlay.gd](../tmp/pelleas-meirin-analysis/skin-pack/MeirinWatcherSkin/scenes/combat_ug_ux_overlay.gd) |
| 文本解包工具 | [inspect_pack.py](../tmp/pelleas-meirin-analysis/inspect_pack.py) |

## 3. 实际架构

```text
Watcher 提供卡牌和战斗机制
  -> 梅铃 Entry 拦截攻击、支持动作和表现事件
  -> 卡牌上下文 / 能力判断 / 动作路由
  -> 梅铃 Profile 提供动画、特效、声音和时间参数
  -> CznCore 负责资源预载、实例池、节点缓存和生命周期

UG/UX 另外进入 C# + GDScript 舞台
  -> 创建表现代理并隐藏真实角色
  -> 隐藏绘制与纹理发布屏障
  -> 建立演出时间起点，通知声音与命中等待
  -> 恢复真实角色，停止视口并释放舞台
```

皮肤不是简单替换贴图。它会检查卡牌是否攻击、切姿态、入神、施加能力或随机选敌，
选择普通攻击、连续攻击、重击、支援或 UG/UX 路线。
`Entry.ResolveCardCapabilities` 从卡牌方法 IL 中识别相关调用，
按卡牌 `Type` 缓存结果，避免每次出牌都重新扫描。
这适合兼容他人的 Watcher 卡牌；我们的自有卡牌可以直接提供元数据，不必引入 IL 扫描。

## 4. 已确认生效的优化

### 4.1 按需预载与低压力预热

`Entry.ConfigureRunStartPreload` 注册开局预载：

- 基础资源项使用 `BodyOnly`，不是将全部瞬时特效作为普通资源项一次加载。
- 特效引擎另行加载场景缓存集合，并预热普通攻击 starter 实例。
- 增加入场、近战移动、出牌覆盖层和语音准备步骤。
- `RunStartResourcePreloader` 最多保留 4 个在途请求，逐帧检查状态。
  单个请求超过 5 秒后让原流程继续，另行收尾请求。

梅铃实际采用 `LowPressureSpineWarmup`：

| 配置 | 本机版本取值 |
| --- | --- |
| starter 匹配词 | `attack_play1`、`attack_play2` |
| 场景缓存 starter 匹配词 | `meirin_`、`common_hit_eff`、`system_buff_shield` |
| starter 预热实例数 | 每个 key 1 个 |
| 批次数 | 每批 1 项 |
| 战斗预热初始延迟 / 步长 | 0.85 / 0.24 秒 |
| 场景缓存初始延迟 / 步长 | 0.65 / 0.34 秒 |
| follow-up / deferred 实例预热 | 关闭 |
| 全部战斗 SFX 预热 | 梅铃显式改为开启 |

这些战斗延迟在入场演出结束后的调度基础上使用，不应理解成战斗开始后立刻启动。
`UseSubThreadsForWarmupResourceLoads` 会根据本机 Spine API 能力决定是否允许子任务，
不能笼统描述为“所有 Spine 都安全多线程预热”。

效果：将部分 IO、资源构造和首次渲染工作移到准备阶段，控制单次工作量。
代价：准备时间增加，未列入实例预热的动作仍可能在第一次使用时发生构造开销。

证据：`Entry.cs:4893, 5251`；
`CharacterEffectEngine.cs:389, 499`；
`CombatWarmupPolicyProfile.cs:195`；
`RunStartResourcePreloader.cs:189`。

### 4.2 有容量限制的特效实例池

`CombatTransientEffectPoolRuntime` 同时缓存 PackedScene 和可复用 Node2D：

```text
按场景与材质参数定位池
  -> 取出有效且 epoch 匹配的实例，池空才 Instantiate
  -> 更新 generation，挂到当前锚点，播放
  -> 停止发射，隐藏，RemoveChild
  -> 回池；超过空闲容量才 QueueFree
```

关键细节：

- 池 key 包括场景、palette、PMA、attack boost、alpha 和是否保留场景根，
  减少不同表现参数错误复用同一实例的机会。
- 皮肤回调虽然写了 `Clamp(..., 1, 4)`，核心还会按当前策略再次限制。
  当前策略默认容量 1、最大容量 2，因此每 key 空闲实例容量实际为 1~2，
  常见默认 cue 为 2。不能直接说它有“4 个实例的池”。
- 这个容量限制的是回收后保留的空闲实例，不是同时播放的实例总数。
- 3 次房间边界没有使用的池缩到 1 个；不会因此释放所有冷门 key 或全部场景缓存。
- 对局结束推进 epoch、清理空闲实例和场景缓存；旧延迟回调由 generation/epoch 隔离。

效果：重复出牌减少 Instantiate、节点包装和销毁开销。
代价：保留常驻内存；多目标或连续动作超过池储备时，仍会临时创建实例。
它仍执行 RemoveChild/AddChild，不是 Arabella 那种保留在同一父节点下的复用方式。

证据：`CombatTransientEffectPoolRuntime.cs:386, 418, 492, 521, 573, 663`；
`CharacterEffectEngine.cs:346, 485`；`Entry.cs:5272`。

### 4.3 缓存节点集合与 Spine 包装

`CombatEffectRuntime` 使用两个 `ConditionalWeakTable`：

- 根节点 -> Spine、粒子、CanvasItem 等节点集合。
- Spine 节点 -> MegaSprite 包装。

新实例完成层级组织后调用 `PrimeNodeCaches`，播放和停止时直接使用缓存集合。
缓存建立后需要维持节点结构一致，不能任意增删子节点却不刷新缓存。

我们的 `MeiLinVfxHelper.PlaySpinesAndAutoFree` 每次新实例都遍历节点，
并逐个尝试构造 MegaSprite 包装；这一点有明确的可优化空间。
需要实际采样后才能判断遍历、包装尝试和渲染各占多少耗时。

证据：`CombatEffectRuntime.cs:200, 220, 359, 547`；
`CombatTransientEffectPoolRuntime.cs:464`；
我们的 `MeiLinModCode/Vfx/MeiLinVfxHelper.cs`。

### 4.4 UG/UX 使用隐藏绘制屏障和统一演出时钟

PCK 中保留了可直接阅读的 `combat_ug_ux_overlay.gd`，这部分无需猜测反编译结果。

- 舞台使用固定 `1152x648` 的透明 SubViewport，并禁用 3D。
- 显示前将已创建的 Spine、主体和 cut-in 定位到约 0.8 秒的代表帧。
- 对粒子场景创建临时副本并强制发射，覆盖后续延迟发射路径的预热。
- 隐藏绘制后销毁预热副本，将正式节点复位到 0，并保持两帧隐藏纹理发布。
- 通过 `presentation_started` 和 `stage_started` 分别协调声音及动作时钟。
  UX 另有 1.6 秒 cut-in 前导段。
- UG 的主命中等待相对动作时钟为 2.2 秒；UX 为 2.7 秒。
  这些不是包含隐藏准备和 UX 前导段的完整调用耗时。

效果：把部分冷启动工作移到演出计时开始之前，避免开场动画时间被冷帧消耗。
代价：玩家仍可能等待准备阶段；隐藏绘制不等于消除了总耗时。

限制：`_update_entries` 仍在后续 cue 到点时实例化并在结束后 QueueFree。
隐藏预热覆盖已构造的 Spine 和粒子副本，不保证所有后续 Spine 场景都完成实际首绘。
整个 UG/UX 舞台也只缓存 PackedScene，每次仍 Instantiate，不能称为完整演出对象池。

证据：GDScript `:14, 166, 223, 650, 720, 775`；
`Entry.cs:3286, 3318, 3336`。

### 4.5 表现代理、取消和恢复流程比较完整

UG 对敌人的可视锚点创建镜像代理，AOE 最多 5 个。
代理会记录 Spine 状态、裁剪实时表现子节点，并注册镜像反馈；
真实节点主要隐藏和恢复，而不是直接把真实敌人重挂到演出舞台。

取消时先同步调用 `cancel_stage`，停止 process、隐藏输出、关闭视口更新、
停止粒子并复位相机，再延迟销毁。
这可避免 QueueFree 尚未生效时，上一演出的末帧仍覆盖下一演出的开场。
阶段回调检查 generation，恢复时验证 Godot 实例有效性，并保留原可见状态。

代价是复制可视节点的构造和内存开销。不能仅凭使用代理就认定比借用真实节点更快；
它主要使场景所有权与恢复行为更清晰。

证据：`Entry.cs:3499, 3534, 3589, 3608`；
`CombatTargetStaging.cs:306`；GDScript `:1518, 1533`。

### 4.6 卡面与音频选择了不同的成本结构

卡面使用 `card_replacements.json` 映射到 PNG，普通、古代和检视图片分别提供。
`BundledSkinCardArtService` 替换 TextureRect 纹理并记录原状态；
普通纹理由 `BundledSkinCardArt` 缓存，没有为每张卡建立 Spine 渲染视口的这条路径。

我们的动态卡面使用 Spine SubViewport，当前仍多处设为 `UpdateMode.Always`。
因此卡面成本差异首先来自功能选择，不能全部记作对方代码更高效。

音效使用缓存的 AudioStream 和可复用播放器：预建 2 个，最多 12 个，
避免每个音效都创建和销毁播放器；达到上限时会复用播放器。

证据：`BundledSkinCardArt.cs:313`；`BundledSkinCardArtService.cs:296`；
`CharacterSkinSfxPlayerRuntime.cs:192`；
我们的 `MeiLinModCode/Patches/CardSpinePortraitPatch.cs:152, 210, 216`。

## 5. 不能当成实际优化的框架能力

1. **并发预算 / Optional 降级**：核心有 QueueGate 和预算 profile，
   但在本次分析的两套 DLL 静态调用中没有找到实际调用者。
   当前 `CombatTransientEffectCueRuntime.Queue` 直接加载、取实例、挂树并安排延迟。
   不能据此声称当前梅铃启用了全局并发限制或自动 LOD。
2. **耗时监测**：梅铃创建 `SkinDiagnosticsRuntime(() => false, ...)`，
   StartTimingProbe 直接返回 0，所以该路径没有开启常规耗时统计。
   UG/UX 仍另有 loadInstantiateMs、stageMountMs、preplayMs 的直接时间日志。
3. **忙时自动退让**：存在 ShouldDeferWarmup 和 MarkWarmupBusy，
   但未在分析范围内找到 MarkWarmupBusy 调用，不能宣称已形成帧耗时自适应调度。
4. **所有资源异步 / 所有对象池化**：普通 cue 仍允许同步加载回退，
   UG/UX 后续节点也仍有即时创建和销毁。
5. **0.113 兼容性或帧率领先**：没有运行对方模组，也没有同场景性能对照，
   这次只判断实现与调用关系。

证据：`CombatTransientEffectCueRuntime.cs:258`；
`CombatTransientEffectQueueGate.cs:192`；`SkinDiagnosticsRuntime.cs:270, 325`；
`Entry.cs:5293`。

## 6. 对我们项目的判断和建议

| 维度 | Pelleas 梅铃 + CznCore | 我们当前 MeiLinMod |
| --- | --- | --- |
| 定位 | 多皮肤共享表现框架，依托 Watcher | 独立角色，卡牌机制和表现同仓维护 |
| 普通特效复用 | 有容量限制的实例池及回收代际 | PackedScene 缓存，播放实例主要现建现销 |
| 首次出牌准备 | 开局预载与低压力 starter 预热 | 启动预载，战斗中低 alpha 深预热 |
| 节点发现 | 创建后缓存分类集合与包装 | 每个新实例重新收集和尝试包装 |
| 卡面 | 缓存静态纹理 | Spine 动态卡面，更多持续渲染成本 |
| UG/UX | 隐藏绘制屏障、镜像代理、分阶段信号 | 自有演出、命中回调及房间/状态守卫 |
| 生命周期 | 实例 generation、池 epoch、冷池裁剪 | Creature generation 与战斗房间校验 |

**在多皮肤表现复用、普通特效回收和资源准备的组织上，对方更成熟。**
这个判断不延伸到独立角色玩法、存档正确性或实际 FPS。
其 UG/UX 仍有即时构造，预算类也没有确认接通，不能当作全面优于其他项目的结论。

最推荐按以下顺序做最小优化：

1. 给我们的高频普通攻击 / 命中特效加小容量池，并缓存实际 Spine、粒子节点。
   必须同时处理完成信号解绑、粒子重启、transform 复位及过期回调，不能只把 QueueFree 改成 Hide。
2. 对动态卡面增加真正的可见性驱动更新；保留动态表现，减少不可见视口持续更新。
3. 对存在首播跳帧的 UG/UX 引入“隐藏首绘 -> 复位 -> 开始演出时钟”的屏障，
   声音和命中等待都绑定同一阶段信号。先保留当前舞台结构，不整体移植 CznCore。
4. 角色/功能开启时才执行对应预载，采用有限在途请求和逐项准备。
   性能采样只覆盖首次出牌、连续出牌、AOE 和取消演出等必要场景。

与此前分析的项目相比：CznCore 的池容量和冷池裁剪值得借鉴；
Arabella 保留实例在原父节点下，可以少走一部分进出树流程，但需要控制常驻容量；
Shu 的自定义粒子和网格传输优化属于更底层的另一条路线。
这三种思路不互相替代，不应在缺少相同素材和场景实测时排总 FPS 名次。

## 7. 分析版本指纹

```text
MeirinWatcherSkin.dll
3CC58BD8D8FB37A7EFF48352698612D1FCB1FE3A2E0EDD02CF34D58E2D7EC34A
MeirinWatcherSkin.pck
0A4DDDC1C7FC95F4EB2CEEE7957724DBCDC77302A9282BC56D3A511E70448007
CznCore.dll
32E3C08BA1764DD54B3D27987DA018FE9DED52770416FB02AC1F32252BF0FDFF
CznCore.pck
B7DF3CF9E1ACF2E183F635C6B6B81D56BE22F3EE7770A9EA0E1B8B09FC9145BB
```

后续订阅更新后先核对指纹，再决定是否沿用本次结论。
