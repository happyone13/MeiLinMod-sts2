# More Ironclad Animations 动画实现逆向记录

分析日期：2026-10-27。范围：本机 Steam 订阅版本的静态反编译、资源解包和数据结构核对；未启动游戏验证视觉效果或测量性能。

## 定位与产物

| 项目 | 本机结果 |
| --- | --- |
| 名称 / 作者 | More Ironclad Animations / page |
| Workshop ID | `3798368044`，游戏 App ID `2868840` |
| Mod ID / 版本 | `ShieldOnly` / `v1.9.60-wings.3` |
| 安装位置 | `D:\Steam\steamapps\workshop\content\2868840\3798368044` |
| Manifest | `has_pck=false`、`has_dll=true`、`dependencies=[]`、`affects_gameplay=false` |
| 文件体积 | 128 个文件，58,969,005 字节，约 56.24 MiB |
| 主 DLL SHA-256 | `11f62be7ab9f9522d8a30ec688fcfe8f1306428550d3bd27838562e0c9cf6d43` |

使用 Rider 自带的 `ICSharpCode.Decompiler.dll`，编写独立的小工具，并让解析器读取本机游戏的 `sts2.dll`、`GodotSharp.dll`、`0Harmony.dll` 等依赖。只读取目标 DLL 的元数据和 IL，没有加载执行该 Mod 的初始化代码。

产物位于项目的 `tmp/more-ironclad-animations/`：

- `decompiled/ShieldOnly.cs`：主程序集完整反编译结果。
- `decompiled/ShieldOnly/ShieldOnly/`：主程序集按类型拆分的 137 个 `.cs`，其中 `Slam/` 包含肚皮冲撞独立实现。
- `decompiled/ShieldOnly.Settings.cs`、`decompiled/ShieldOnly.BaseLib.cs`：两个辅助程序集；另有按类型拆分文件。
- `decoded/`：7 份解压后的二进制 motion，以及 2 份解压后的 JSON 元数据。
- `asset-inventory.json`：全部安装文件的大小、SHA-256，骨骼/动画列表，以及 motion 轨道统计。
- `original/`：三个 DLL、manifest、README、checksums 的分析时快照。
- `extract_resources.ps1`、`inspect_assets.py`：资源解包与结构核对脚本。

反编译工具工程位于 `tmp/tools/mod-decompiler/`。反编译结果用于理解逻辑，不保证可直接重新编译；原注释、工程结构和部分变量名无法恢复。

## 核心结论

这是 **Harmony 事件接入 + 原生 Spine 播放 + 自定义网格变形数据 + 表现会话管理** 的组合。当前检查到的主要角色动画路径不依赖整角色 PNG 序列帧；骨骼、插槽、IK、网格顶点和绘制顺序共同参与表现。

部分 `.spjson` 只有一个根骨骼，例如坚不可摧和重振精神。这些资源的大量运动由 `.motion` 顶点轨道承担，不能仅凭骨骼数量判断动画复杂度，也不能只复制 `.spjson` 就期望得到完整动作。

入口 `ModEntry.Initialize()` 调用 `Harmony.PatchAll()`；BaseLib/JmcModLib 设置界面是可选集成。Manifest 的最低版本是作者声明，不能据此确认所有后续游戏版本都兼容。

## 资源格式与运行时加载

| 资源 | 含义 |
| --- | --- |
| `.spjson` | 可直接解析的 Spine JSON；版本主要是 4.2.43，壁垒放置为 4.2.22 |
| `.spmeta` | 自定义元数据，包含纹理页、锚点、时间、材质、网格等；部分经过压缩 |
| `.motion` | 自定义二进制网格变形轨道，运行时插值并写到 Spine 插槽 |
| `.atlas` / PNG | 图集信息与纹理 |
| `.gdshader` | 金身、模糊合成、恶魔眼部火焰、冲撞效果等 |
| `blood/plans.blood` | JSON 血流轨迹预设；`blood/atlas.png` 提供血块图块 |

`RuntimeAsset.Open()` 检测前四字节：`CAZ1`、`SWZ1`、`JMZ1` 是标记，其后的数据使用 **Brotli** 压缩；不是加密。解压后，前两种分别得到 `CAM1`、`SWM1` 二进制格式，最后一种得到 JSON。

典型 Spine 加载流程，见 `NativeCardActionPresentation`、`NativeShieldPresentation`：

1. 通过 `ClassDB.Instantiate("SpineSkeletonFileResource")` 创建资源，并 `load_from_file()` 读取外部 `.spjson`。
2. 构造 `SpineSkeletonDataResource`，绑定图集与骨骼文件。
3. 创建 `SpineSprite`，设置 skeleton data，将自动更新改成手动控制。
4. 每次采样先 `set_to_setup_pose()`，再对指定时间调用动画 `apply()`。
5. 在 `before_world_transforms_change` 等适当阶段写入网格变形，最后 `update_skeleton(0f)`。

`NativeTextureAtlas` 及相关建图集代码会读取原版 atlas，并结合额外纹理构造运行时 `.spatlas`，缓存到 `user://ShieldOnly/texture-cache/`。它不需要为每个完整动作保存一套全身画面。

## motion 如何驱动动画

`CardActionMotion` 的主要数据：基础顶点数组、共享帧表、动画名到轨道列表的映射。轨道记录 `Slot`、`Attachment`、顶点浮点数数量、时间数组、共享帧索引。

每个绑定保存 `Left`、`Right`、`Values` 数组。播放时二分查找相邻关键帧；只有关键帧区间变化才重新解码左右帧，然后逐顶点线性插值，调用插槽 `set_deform()`。还会检查当前 attachment 名称，避免对错误网格写入顶点。

`CAM1` 帧模式：`0` 为完整浮点数组，`1` 为基础网格加仿射变换与浮点位修正，`2` 为稀疏值，`3` 为常数填充。压缩文件里的完整浮点帧采用字节平面布局，加载时重排。`SecondWindMotion` 使用另一种按轨道存储的 `SWM1` 格式。

以下是实际解包并解析的结果；轨道数是 deform 轨道总数，不是骨骼数，时长列为最后关键帧时间，不等同于运行时耗时。

| 文件 | 内部格式 | 轨道数 | 轨道关键帧合计 | 最后关键帧（秒） |
| --- | --- | ---: | ---: | ---: |
| `shield.motion` | CAM1 | 173 | 4,124 | 0.686 |
| `body-slam.motion` | CAM1 | 45 | 4,743 | 1.908333 |
| `barricade-placement.motion` | CAM1 | 61 | 18,326 | 1.7 |
| `feed.motion` | CAM1 | 18 | 3,522 | 1.683333 |
| `impervious.motion` | CAM1 | 43 | 3,218 | 0.55 |
| `second-wind.motion` | SWM1 | 69 | 13,520 | 2.0 |
| `shrug-it-off.motion` | CAM1 | 47 | 6,530 | 1.2 |

## 举盾、受击与破盾

入口是 `EnemyTurnWindowPatch`、`ShieldIntentStartPatch`、`EnemyActionPatch`，协调类是 `TurnSession`。

敌方回合建立会话；敌人即将执行攻击意图时，在配置和护甲条件允许的情况下播放举盾，保持举盾姿势，订阅玩家的 `BlockChanged` 和 `CurrentHpChanged`。它根据实际护甲减少、归零和生命损失选择受击或破盾表现，不靠固定计时猜测敌人何时造成伤害。

`QueueHit()` 将同一帧内的相关通知延迟到下一帧统一注册，避免一次攻击先扣护甲、再扣生命时重复触发两段受击。破盾后播放收盾，并释放角色接管。

`NativeShieldPresentation` 中有 `guard-raise`、`normal-impact`、`weak-impact`、`guard-break` 动画。当前主要受击选择路径使用 normal/guard-break，不能仅凭资源存在就认定所有动画都在常规流程中播放。`guard-lower` 通过倒采样举盾动作实现。

一个具体时间换算：举盾源动画为 0.45 秒，运行时可以在 280 毫秒内播放完；收盾耗时 340 毫秒。因此调节手感的控制点在会话和时间映射层，不只在 Spine 文件里。

## 肚皮冲撞：命中同步

入口：`Slam/Played.cs` 补丁 `BodySlam.OnPlay`；`Slam/Attack.cs` 补丁 `AttackCommand.Execute`。核心是 `SlamSession` 和 `NativeSlam`。

1. `TryBegin()` 根据本次 `CardPlay` 的实际目标创建表现，接管身体并预建冲击特效。
2. `Prepare()` 对属于这次出牌的攻击调用 `WithNoAttackerAnim()`，避免再播原版攻击动作。
3. 包装 `AfterAttackerAnim()`，保留已有回调，然后等待 `_ready.Task`。
4. 表现循环推进到源动画的 `1.0333333` 秒接触点，放行 `_ready`。
5. 攻击回调锁定接触目标并让原攻击流程继续；命中特效通过 `WithHitVfxNode()` 定位到实际受击对象，同时开始目标后仰。
6. 后续播放离场和回位；`Run()` 等待可交还角色的节点，而不要求所有视觉尾段都结束后才能继续。

源数据还记录了起跳 `0.9` 秒、到达 `0.9333333` 秒、接触 `1.0333333` 秒、释放 `1.5333333` 秒。`PlaybackTiming` 会重新映射这些时间：例如前 0.9 秒源动作按 2.25 倍速推进，约 0.4 秒完成蓄势；不能把源关键帧时间直接当实际等待时间。

这条实现保留原卡牌结算代码，但确实通过异步等待改变了结算与动画之间的时间关系。它不是单纯在旁边播放一个不参与协调的特效。

## 恶魔形态：附件跟随与连续时钟

`DemonFormPlayedPatch` 启动 `DemonFormSession` 和附件状态；`DemonHorns` 管理不同表现表面的角、翼、眼睛等附件。

`DemonHornState.WingSeconds` 属于角色状态，随 `ProcessFrame` 推进。同一角色的原身体、举盾身体、金身、冲撞身体从共享时钟计算翅膀相位。`demon-wings.spmeta` 定义 4.8 秒周期，切换表现节点不会重置相位；场景暂停或坚不可摧保持姿势时停止推进，之后继续。

`NativeDemonWings` 在 `world_transforms_changed` 后更新跟随：既支持普通骨骼矩阵，也支持从胸部变形网格的三个参考点恢复变换；加权网格由 `NativeWeightedSurface` 计算骨骼影响。最终提取旋转和统一缩放，减少身体网格形变把翅膀过度拉扯的问题。`DemonWingMotion` 再叠加翅膀自身运动、攻击转身和前后翼布局变化。

所以此处附件跟随不只是固定偏移挂到一个骨骼下；它考虑了角色在不同动画资源里的躯干变形。

## 金身与其他效果

`NativeImperviousPresentation` 建立三个 `SubViewport`：身体、横向模糊、纵向模糊。身体按 0.75 倍尺寸渲染，两个模糊视口再使用身体视口的一半分辨率。最终 `impervious-composite.gdshader` 合成金色外光、描边和边缘亮度。它是离屏渲染与 Shader 的组合，不是只改 `Modulate` 为黄色。

`BloodReturnDamagePatch` 在放血/祭品实际自伤后触发血流。`BloodFlowSession` 把角色位置和能量 UI 中心转换到同一 Canvas 坐标系；御血术通过攻击回调连接实际目标，包括队友。`NativeBloodFlow` 使用一个绘制层的 `Draw` 回调绘制图集图块、多边形与轨迹，没有给每一滴血创建独立节点。

`CardWeapons` 监听原生 Spine 动画信号，在对应卡牌攻击时隐藏原剑插槽并挂接锤/狼牙棒图像。壁垒则分为 `NativeBarricadePlacement` 放置动作和 `NativeBarricadeWall` 持续墙体，包含碎片、旗帜等独立表现。

## 接管、恢复与兼容

`CardBodySession` 管理角色表现所有权、取消令牌、可交还时点与视觉结束时点。它隐藏原身体的可见子部件，保存原始颜色；需要衔接下一动作时调用 `YieldActor()`，交给 `CardBodyHandoff` 做约 90 毫秒的平滑透明度交接。

清理覆盖死亡、离树、战斗切换、Run 清理、Spine 资源替换和异常。释放时取消等待、断开信号、注销事件、恢复部件颜色、释放 Carrier。`CharacterVisualOwnership.RestoreHiddenParts()` 只在部件仍处于本 Mod 写入的隐藏颜色时恢复，降低覆盖其他 Mod 后续修改的概率。

`SpineBinding` 保存 skeleton data/file/atlas/skeleton 的绑定，`CharacterSkinCompatibility` 检查骨骼布局和 atlas 签名并缓存结果，同时监听资源变更。遇到不兼容皮肤或另一套非标准动画时，相关接管路径退出。

范围限制也明确：完整身体接管主要限单人；部分血流、恶魔附件、武器等允许多人；多人壁垒墙体只针对本地玩家。相关判断集中在 `PresentationScope`，不能把所有动画都视作多人可用。

## 性能策略与可借鉴点

已确认的策略包括：静态缓存骨骼、纹理和 motion；复用顶点缓冲；共享帧索引减少重复数据；只在关键帧区间变化时解码；兼容检查按资源版本缓存；用低分辨率模糊视口降低离屏效果代价。

`PresentationPreloader` 不只加载文件：它在 256×256 的临时视口实例化多种表现、采样代表性姿势，再等待渲染帧后释放临时实例。这覆盖了比单纯 `ResourceLoader.Load()` 更多的首次使用路径，但不能据此断言所有 Shader 和 GPU 上传都已完全预热。

与梅铃目前的预生成 `.tscn` + 指令时间轴方案相比，最值得借鉴的是：**命中回调门控、明确的身体接管与交还、死亡/换资源时中断、附件跨动作共享时钟、实例化后实际渲染预热**。无需为了采用这些机制而把现有 VFX 系统整体改成自定义 motion 格式。

潜在代价是每轨道的 C# ↔ Godot 调用、CPU 顶点插值、临时视口和多次 Shader 采样。当前没有 Profiler 数据，无法把它们认定为已发生的瓶颈。

## 已完成验证与边界

- 三个程序集都完成反编译，并解析本机游戏依赖，拆出 139 个按类型保存的文件。
- 7 个 motion、2 个压缩 JSON 都成功 Brotli 解压。
- motion 解析检查了文件头、计数、时间排序、关键帧引用/偏移和文件结尾；7 个文件全部通过。
- `.spjson` 的骨骼、插槽、动画及最后关键帧信息已写入 inventory；本机 128 个文件均记录 SHA-256。
- 未启动游戏、未进行运行时兼容测试、未验证画面层级或采集帧耗时；这些结论限于该本地版本的代码和数据。
- 本次只新增分析工具和报告，没有修改梅铃战斗代码、Steam 安装文件或订阅内容。
