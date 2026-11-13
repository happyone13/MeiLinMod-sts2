> 已由四版本 v2 修订替代：请阅读 [roswitha-ugux-v2.md](roswitha-ugux-v2.md)。旧版整段 OGV 推荐方案无法保留透明收尾，不再作为当前接入方案。

# 罗丝薇瑟皮肤版 UG / UX：素材包与游戏接入

本说明对应 2026-10-29 重制版，包含已经修复宽高比的 UX 特写。

## 1. 当前交付到哪一步

已有皮肤版离线演出、PNG 序列、带音效 MP4、Godot 可播放的 OGV，以及可重建的原始素材和脚本。没有修改、编译或部署现有皮肤 Mod DLL；运行时适配器是待集成模板，不能把整个 ZIP 直接当成游戏 Mod 安装。

| 内容 | UG | UX |
|---|---|---|
| 源命令 | `30134_02.srmd / ug_buff` | `30134_02.srmd / ux_attack` |
| 核心皮肤配方 | `rossweisse_30134_ug_buff_eff_02` | `rossweisse_30134_ux_attack_01_skin` |
| 输出 | 121 帧，4.033 秒 | 346 帧，11.533 秒 |
| 尺寸与帧率 | 1280×720，30 FPS | 1280×720，30 FPS |
| 音效 | `se_30134_ug_buff.wav` | `se_30134_ux_attack.wav` |

原参考视频的战场、UI、敌人没有烘焙进成品。TARGET 特效在独立舞台固定位置播放；粒子由源参数近似采样，不是原游戏引擎的完整复刻。当前版本不能宣称逐帧一致。

## 2. 包结构

```text
README.md                          本说明
runtime/
  RoswithaCinematics/
    presentation.tscn/.gd          实时播放场景，可复制进目标项目
    ug.ogv / ux.ogv                已含音效，游戏侧播放入口
  RoswithaPresentationAdapter.cs    Godot C# 调用模板
bake_project/                      独立烘焙工程，不含 .godot 缓存
  stage.gd / particles.gd          离线工具，不能作为战斗脚本挂载
  config.json                     完整源事件和图层数据
  assets/ / ArtWorks/ / bin/       Spine、角色、材质导入设置和扩展
  cutin/                          UX 原始特写帧，25 FPS
  ug/ / ux/                       最终 PNG 序列，30 FPS
  *.mp4 / *.wav / preview.html     预览与音效
sources/
  ux与ug/                         原始 SRMD、CFX、Spine、粒子、音频
  project/                        重建所需的角色、视频与 Spine 扩展
tools/                            可重建脚本与转换器
references/                       MeiLin/Yuki 与皮肤 Mod 接线参考
SHA256SUMS.json                    文件大小和 SHA-256 清单
```

`sources/ux与ug/预览` 不再次归档，避免把交付包自己递归打进去；参考录像已经放在 bake_project。包内 build/package 脚本的默认路径改为相对包目录，不依赖原机器的 E 盘位置。Python 依赖见 `tools/requirements.txt`；Godot、Python、FFmpeg、游戏程序集和 Mod SDK 不随包安装。

## 3. 推荐的接入路线

| 路线 | 优点 | 限制 |
|---|---|---|
| OGV 完整演出覆盖层 | 快速落地、无需运行时加载数百张 PNG、构图固定 | 已烘焙黑底，无法把真实敌人插入画面内部 |
| Spine + 粒子 + 真实目标的实时舞台 | 可以借用敌人、按 NODE_ANI 运动，适配真实战场 | 需要另做实时调度、材质和生命周期联调 |

推荐先接 OGV，确认触发、玩法和清理正常，再做真实目标版本。不要在游戏运行时调用 Python、解析 CFX、逐帧写 PNG 或执行 `stage.gd` 的 `SceneTree.quit()`。本包的运行时模板仅播放视觉，并提供独立时钟。

## 4. 接入现有罗丝薇瑟皮肤 Mod

现有代码证据在 `references/skin_dllpatch/`：

- `UgVfx.cs` 将 `XCskinAttackSequencePatch.RoswithaTryPlayUgCardCutin` 守卫通过后的调用改为 `RoswithaPlayUgSpine`，场景为 `roswitha_ug_buff.tscn`。触发牌守卫为 `COSMIC_INDIFFERENCE`。
- `CutinVideo.cs` 使用 `RoswithaCosmicIndifference` profile 播 UG；UX 的 `CRUSH_UNDER` 保留可等待的播放流程。
- 这些是现有工程入口，不代表当前包已经把补丁修改好。原脚本含 D 盘游戏路径，修改前须换成本机实际游戏路径。

最少改动方案：

1. 在皮肤工程中找到 `roswitha_ug_cutin.tscn`、`roswitha_ux_cutin.tscn`，查看当前 `VideoStreamPlayer` 引用；把完整重制 OGV 复制到两个明确的新资源路径，先在 Godot 中预览。
2. UX 若替换已有的“开场特写”视频，必须同时调整等待流程，使它只播放一次完整 UX；不要完整视频播完后又启动旧攻击演出。完整视频已经含开场特写，不能重复加 2.4 秒。
3. UG 当前已被 `UgVfx.Patch` 改成 Spine 路径，直接替换旧 UG 视频不会生效。选择恢复到 `CutinVideo` 的 UgProfile 视频调用，或者把守卫通过后的调用改为新 presentation 场景。
4. 若走新场景，直接实例化 Node2D presentation 后挂到战斗房间，调用 `begin("ug")` / `begin("ux")`。不要盲目传给 `PlayOneShotFront`：它按 Spine 动画和完成信号回收，新视频场景没有占位 Spine。
5. 保留已有皮肤启用、角色归属、卡牌归属守卫。不要只按卡 ID 全局替换，其他角色拿到同卡不能播放。
6. 保留原卡牌的玩法命令。皮肤补丁若只是视觉监听，不能再次执行伤害或 Buff；若要让命中真正等待演出，必须接入现有可等待卡牌执行链，而不是从一个通知型补丁里并行发起伤害。
7. 完整 OGV 已带音效，关闭旧路径同一次出牌的重复音效与默认演出音频。视频模板音量默认独立，正式接线时应接游戏音量设置，不要同时播放包内 WAV。

Mono.Cecil 补丁是否能安全重打必须由原工具的幂等检测决定。不要针对已经 Patch 的 DLL 强行叠加另一轮；使用原 DLL 副本重建，保留回滚备份。

## 5. 接入 MeiLin/Yuki 式独立角色 Mod

把 `runtime/RoswithaCinematics/` 整个目录复制到目标 Godot 项目根目录，保持 `res://RoswithaCinematics/presentation.tscn` 可加载。

把适配器 C# 复制到自己的源码目录并加项目命名空间。它只依赖 Godot 和标准库；没有编译验证，不是直接替换 MeiLin/Yuki 的成品服务。调用前应落实：

```csharp
// 这是调用形状；变量应来自当前卡牌的原生执行上下文。
await RoswithaPresentationAdapter.PlayAsync(
    room,
    "ug",
    eligible,                  // 本角色 + 当前卡归属 + 演出开关 + 战斗特效开关
    new double[] { 3.2 },
    combatStillValid,          // 同一个房间、战斗未结束、施法者仍有效
    async (beatIndex, cinematic) =>
    {
        // 在此调用原卡牌自己的效果函数，保持原本伤害/Buff/同步规则。
        // cinematic==true 时按原项目方式取消重复普通攻击动画。
        await ExecuteOriginalCardEffectAsync(cinematic);
    });
```

`ExecuteOriginalCardEffectAsync` 是需要接到原卡牌的占位接口，包内不实现它。参考 `references/meilin/` 中已在仓库使用的 C# presentation；具体 STS2 类型、角色类型和音频服务以目标项目为准。

适配器在回调前先消费索引；回调抛异常不会重新补发同一个命中。资源缺失或开关关闭时立即走剩余玩法回调；战斗结束时停止并清理。调用方应串行等待每张牌，避免同时创建多个覆盖层。正式版还应增加 session revision/取消令牌，禁止旧演出回调修改新会话。

## 6. 命中时刻与两个时钟

UG 源命中为 3.2 秒。UX 的 ENTRY T0 `HIT action` 是起手动作入口，不能自动当成真正伤害。

源 SRMD 的时间由 `from_guid + delay + 等待父 duration` 得到。重制视频在 0.7667 秒插入 2.4 秒动态特写，因此后续事件的演出时间需要加 2.4 秒。UX 的 13 个源 DAMAGE 节点对应：

```text
3.3767, 3.4767, 3.5767, 3.6767,
6.5097, 6.6097, 6.7097, 6.8097, 6.9097, 7.0097,
7.9097, 8.0097, 8.1097 秒
```

不要照搬原游戏的 5/10 等伤害数值，更不能把单次 STS2 攻击自动执行 13 次。若目标卡是一次伤害，明确选一个视觉命中点（如最终爆发 7.9097 秒），只执行一次原效果；若原卡本来多段，再把已有命令逐段分配到选定 beats。UG 是 Buff 动作，也不应因为叫“大招”就新增伤害。

运行时 `elapsed` 来自 `Time.get_ticks_usec()`，C# 读取同一个字段；不要根据 VideoStreamPlayer 播放进度或 Finished 信号驱动伤害。解码掉帧时玩法时钟继续前进。当前模板会消费跨帧跳过的 beats，音视频掉帧表现仍需实机检查。

## 7. 镜头、尺寸与真实目标

- 设计分辨率 1280×720，舞台中心 (640,360)。按 `max(viewportWidth/1280, viewportHeight/720)` 等比 cover，居中裁切，不能宽高分别拉伸。
- UX 原始特写为 760×346。其比例修复已经烘焙进 PNG/MP4/OGV；不要再对它做一次反向“补偿”。
- 实时 Spine 版本要保留 CFX 的 `ani`，尤其 `animation_camera`，不得把所有层强制设成同一个动画。
- 已有骨骼相机不能再乘上一层猜测的 World 运镜。SCREEN/CENTER 使用舞台坐标，SELF/TARGET 才使用模型挂点；不要把 XCskin 的 0.29 玩家缩放乘到整屏演出上。
- 要让真实目标参与演出，应读取 `ux/原始素材/_node/rossweisse_30134_ux_attack_01_target_node.json`。先保存模型 parent、index、transform、visible、modulate、z_index、z_as_relative，再隐藏后 reparent；生命条和意图不一起搬移。
- 动画结束、战斗取消、目标死亡或新会话开始时都必须恢复快照。角色恢复时显式回 idle、恢复动画速度。烘焙视频不能把敌人实时画到已压平的图层之间，不能靠调视频透明度解决。

## 8. 重建离线文件

安装 Python 依赖，准备 Godot 4.5.1 Mono console 和 FFmpeg（需要 libtheora/libvorbis/libx264）。包内 converter 为已有工具的副本，Spine 扩展仅打包 Windows 版本。

在包根目录执行：

```powershell
python -m pip install -r tools/requirements.txt
python tools/build_roswitha_full.py
$godotExe = '你的 Godot 4.5.1 console 完整路径'
& $godotExe --headless --path bake_project --editor --import --quit
& $godotExe --headless --path bake_project --editor --import --quit
& $godotExe --path bake_project --script res://stage.gd -- ug
& $godotExe --path bake_project --script res://stage.gd -- ux
$env:ROSWITHA_FFMPEG = '你的 ffmpeg.exe 完整路径'
python tools/package_roswitha_full.py
```

离线渲染需要真实渲染设备，不能用 headless 生成最终图像。重建不要混用旧帧；参数改变导致帧数减少时，先把旧输出帧目录另存，再生成空的 ug/ux 输出目录。OGV 可参考包内 `tools/encode_runtime.ps1` 重新编码。

## 9. 导出、验证与回滚

使用目标项目现有 export preset 导出 PCK，确认包含 presentation、两个 OGV 和脚本。仅视频路线无需在运行时加载整套原始 Spine 或数百张 PNG。PNG 全部常驻的未压缩显存量级约 1.6 GiB，不推荐直接全量预载。

修改 C# 则使用目标项目自身的 .csproj、游戏引用和构建参数；不要复制仓库里历史 D 盘路径。编译后核对 DLL/PCK 哈希再部署，保留旧版备份。

至少实机验证：正确卡触发一次；其他角色同卡不触发；开关关闭和缺资源仍执行原效果一次；连续两次演出；快速游戏速度；目标提前死亡；致死命中；战斗结束中断；高低音量；16:9 和超宽屏；后续出牌/idle/UI 恢复。多人项目还要验证同步。

本次交付验证仅覆盖离线视频、序列帧和资源归档；运行时模板另做 Godot 加载/清理检查。尚未进行目标 Mod 的 C# 编译、实机出牌、多人或 PCK 安装验证。
