# MeiLinMod BaseLib 到 RitsuLib 迁移计划

本分支采用渐进迁移。当前代码和 manifest 已经移除 BaseLib 依赖，内容注册、设置页和补丁注册由 RitsuLib 承接。迁移过程中必须保持现有卡牌 ID、本地化 key、存档行为、VFX 时间轴、位移补丁、背身立绘和动态卡图行为不漂移。

## 当前状态

已完成：

1. `MeiLinMod.csproj` 增加编译期依赖 `STS2.RitsuLib 0.4.50`。
2. `MeiLinMod.json` 增加运行时依赖 `STS2-RitsuLib`，并移除 BaseLib 依赖。
3. 通过 `ModTypeDiscoveryHub.RegisterModAssembly(MainFile.ModId, assembly)` 注册 MeiLinMod 程序集。
4. 新增 `MeiLinRitsuMigration`，把 RitsuLib 初始化从入口拆出。
5. 新增 RitsuLib 原生设置页，继续读写 Yuki/Chaos 共享设置文件。
6. 用本地 `PoolAttribute` 替换 BaseLib 池属性。
7. 新增 `MeiLinRitsuContentRegistration`，集中注册角色、卡牌、遗物、药水和能力。
8. 将卡牌、遗物、药水、能力、角色模板迁到 RitsuLib content template 形态。
9. 将旧 BaseLib hover tip、power icon、古旧卡框材质等 API 迁到 RitsuLib 对应形态。
10. 拆分并迁移所有活动 Harmony 补丁到 RitsuLib optional patcher。
11. 移除入口中的 `harmony.PatchAll()` 和旧 Harmony patch 状态日志。
12. 保留 `ScriptManagerBridge.LookupScriptsInAssembly(assembly)`，用于当前 Godot C# 脚本桥接。
13. 将场景实际引用的 C# 脚本移动到 `GodotScripts`。
14. 导出排除普通 C# 源码、测试、文档、工具、旧 BaseLib 包、编辑器插件和临时文件。
15. 新增 TestTheSpire 自动测试项目，并通过全量验证。
16. 启用 RitsuLib 遥测 applicant，接入 PostHog US、RunHistory 和 MeiLin 自定义平衡 summary。

最近验证：

- `dotnet build tests\MeiLinMod.Tests\MeiLinMod.Tests.csproj -p:IsInnerGodotExport=true` 成功，当前默认目标为 STS2 `v0.108.0` 和 `STS2.RitsuLib 0.4.50`。Windows/Linux 本地输出优先使用游戏目录下已经存在的 `mods2`，没有 `mods2` 时回退到旧 `mods`；当两者不同时，会把 DLL/manifest/PCK 镜像到旧 `mods`，兼容 TestTheSpire 仍扫描 `mods` 的路径。
- 全量 TestTheSpire 通过：

```text
SUMMARY total=54 passed=54 failed=0 skipped=0
```

- 0.108 迁移依赖专项 TestTheSpire 通过：

```text
SUMMARY total=9 passed=9 failed=0 skipped=0
```

- TestTheSpire 依赖准备已修正：当本机没有 `mods2` 目录时，也会把 Workshop RitsuLib 0.4.50 镜像到游戏实际扫描的 `mods/RitsuLib`，避免旧本地 RitsuLib 0.4.40 抢先加载。
- BaseLib v3.2.1 + RitsuLib 0.4.50 + MeiLinMod 的设置兼容冒烟测试通过：`SettingsTests` 共 3 项通过。该组合下 BaseLib 自身会在 STS2 v0.108.0 记录旧 `CustomPile` 补丁初始化异常，但 MeiLinMod、RitsuLib 和测试 Mod 继续加载，说明 MeiLinMod 未重新依赖 BaseLib，也没有因 BaseLib 同场存在而阻断。
- YukiMod 空设置面板兼容补丁仍保留：旧 YukiMod 触发 `XCskin_ModSettingsPanel` 空 VBox 的临时 `_Ready` 异常时，由 `YukiSettingsPanelEmptyReadyCompatPatch` 只针对该窄条件抑制。
- PCK marker 检查通过：包含 `GodotScripts/...`、`MeiLinMod/vfx_configs/1027/generated/meilin_vfx_commands.json` 和 `MeiLinMod.json`；不包含 `MeiLinModCode/`、`tests/`、`docs/`、`tools/`、旧 BaseLib package、`BaseLib.pck`、`~libspine_godot` 和 `.codex_tmp/`。
- 安装目录一致性测试已加入：`MeiLinMod.json` 必须与仓库一致，安装 DLL 必须与当前测试加载的主 Mod DLL 哈希一致，并且 DLL 不含旧 `Alchyr.Sts2.BaseLib` 标记。

## 迁移入口

入口文件：`MeiLinModCode/Entry/MeiLinModEntry.cs`

当前初始化顺序：

1. `MeiLinRitsuMigration.Initialize()`
2. `ScriptManagerBridge.LookupScriptsInAssembly(assembly)`
3. `MeiLinSharedSettings.EnsureSettingsLoaded()`
4. `CardSpinePortraitPatch.PreloadDynamicPortraitScenes()`
5. `MeiLinCommandVfxCoordinator.PreloadConfiguredScenes()`
6. `MeiLinAttackMovementController.PreloadMovementEffects()`

保留 `ScriptManagerBridge.LookupScriptsInAssembly` 是为了兼容当前 Godot 脚本注册。未来只有在确认 RitsuLib 能完整覆盖 Godot C# 脚本桥接后，才应移除它。

## 内容注册

目标：用 RitsuLib 注册所有 MeiLin 内容，同时保持旧 public entry。

当前实现：

- `MeiLinRitsuContentRegistration.Register(assembly)` 统一注册角色、卡牌、遗物、药水和能力。
- 卡牌默认进入 `MeiLinModCardPool`，也可以通过 `[Pool(typeof(NoneCardPool))]` 等属性显式改变池归属。
- 遗物默认进入 `MeiLinModRelicPool`。
- 药水默认进入 `MeiLinModPotionPool`。
- 能力使用 RitsuLib power 注册。
- 所有公开 ID 显式固定为旧 `MEILINMOD-*` 形式。

必须保持的约束：

- 不重新引入 `Alchyr.Sts2.BaseLib`。
- 不重新引入 BaseLib `PoolAttribute`。
- 不改变现有本地化 key。
- 不改变存档可见 public entry。

## 设置迁移

目标：没有 YukiMod 时，MeiLinMod 自己也能提供完整设置页；有 Yuki/Chaos 时继续复用共享配置。

当前 RitsuLib 设置页覆盖：

- `battle_ready_overlay`
- `combat_effects`
- `dynamic_card_portraits`
- `battle_ready_scale`
- `battle_ready_offset_x`
- `battle_ready_offset_y`
- `voice_volume`

旧的 `MeiLinSharedSettingsUiPatch` / `MeiLinSharedSettingsUiOpenPatch` 手工 `NSettingsScreen` 注入实现已经删除。设置 UI 只由 `RitsuLibFramework.RegisterModSettings(...)` 注册，避免无 YukiMod 时出现两套设置入口或乱码旧文本。

共享设置兼容约束：

- 目录：`chaosmod`
- 文件：`xcskin_settings.json`
- AppDomain key 前缀：`CHAOSMOD_XCSKIN_`
- JSON 字段继续保留 `Volume`、`PortraitsEnabled`、`ActionVfxEnabled`、`DynamicCardPortraitsEnabled`、`BattleReadyScale`、`BattleReadyOffsetX`、`BattleReadyOffsetY`

`MeiLinModConfig` 只保留为普通静态门面，不再让 BaseLib 管理配置生命周期。

## 补丁迁移

补丁按功能域拆成 7 组 optional patcher：

- `optional-ui`
- `optional-audio`
- `optional-overlay`
- `optional-combat-animation`
- `optional-scene`
- `optional-card-visual`
- `optional-content`

每个补丁必须：

- 实现 `IPatchMethod`。
- 提供稳定 `PatchId`。
- 使用精确 `PatchTarget.Method(...)`。
- 在 `MeiLinRitsuMigration.RegisterOptionalPatchers()` 中注册。

当前分组：

- `optional-ui`
  - `StatsScreenMeiLinPatch`
  - 当前仍需保留为运行时 UI 补丁，因为基础统计页没有 MeiLin 专用注册入口。`OptionalUiScenePatchTests` 固定了缺少统计容器、缺少 MeiLin 统计和重复插入时的保护逻辑。
- `optional-audio`
  - `SfxCmdMeiLinAudioPatch`
  - 当前仍需保留为运行时音频补丁，因为基础 `SfxCmd` 的角色语音事件和玩家死亡音效没有 MeiLin 专用声明式注册入口。
  - 只 hook `SfxCmd.Play(string, float)`、`SfxCmd.Play(string, string, float, float)` 和 `SfxCmd.PlayDeath(Player)`；不要 hook 怪物死亡音效路径。
  - `MeiLinAudioService` 负责把 `meilin_attack`、`meilin_cast`、`meilin_die`、`meilin_select` 和指定特殊卡 key 映射到 `MeiLinMod/sound/*.mp3`，并用玩家角色 ID 限定死亡、姿态和特殊卡语音。
  - `OptionalAudioPatchTests` 固定了补丁元数据、精确 target、怪物死亡排除、MeiLin key 解析、默认音效抑制边界和音频资源存在性。
- `optional-overlay`
  - 背身立绘战斗开始、胜利、死亡、手牌 focus/unfocus、鼠标点击、键鼠/手柄打牌开始、hover、取消打牌、出牌前状态切换。
  - 当前仍需保留为运行时补丁，因为背身立绘依赖手牌 UI focus、hover、点击和出牌流程的多个节点事件，RitsuLib 目前没有声明式持卡状态入口。
  - 普通鼠标移出后延迟 `0.2s` 播放退出动作；取消出牌保留较长的 `0.8s` 延迟，避免点击/拖牌时误退出。
  - 角色本体持卡动作统一走 `MeiLinAnimationSequenceManager`，进入为 `idle_to_b_idle -> b_idle`，退出为 `b_idle_to_idle -> idle`；出牌后会短暂抑制 focus，避免刚释放卡牌又被 hover 重新拉回持卡状态。
  - `OptionalOverlayPatchTests` 固定了 11 个 overlay patch 的 target、非关键补丁元数据、点击不触发退出、退出延迟和持卡动画序列。
- `optional-combat-animation`
  - `MeiLinTriggerAnimPatch`
  - 不再注册 `MegaAnimationState.SetAnimation(String, Boolean, Int32)` 的 prefix/postfix 补丁，避免和其他角色/动画 mod 的同类 Harmony 补丁互相影响。
  - `MeiLinTriggerAnimPatch` 只拦截美铃玩家的 `Attack` trigger；非美铃、非玩家或非攻击 trigger 必须回到原流程。
  - 攻击段执行时先位移到目标，再播放首段攻击语音、命令 VFX/动画和命中反馈；成功段调用 `ScheduleReturnAfterSegment`，失败段调用 `ForceReturnSoon`。
  - 非最终多段攻击会安排 abandoned return，并用 `AbortActiveAttack` 丢弃剩余段，避免目标提前死亡或流程中断后角色留在敌人位置。
  - 位移层级只在战斗角色父节点内移动 sibling 或使用相对 `ZIndex` fallback，返回时恢复原 sibling/Z 状态，避免超过 UI 层。
  - `OptionalCombatAnimationPatchTests` 固定了 1 个补丁 target、拦截范围、攻击段返回保证、 abandoned return 和层级恢复结构。
- `optional-scene`
  - 商店/营火角色动画 fallback。
  - 不再注册 `MegaAnimationState.SetAnimation(String, Boolean, Int32)` fallback 补丁，包括旧的 game over `die -> death` fallback。
  - 当前只保留商店/营火运行时场景补丁，因为它们修正的是 Godot/MegaSpine 场景生命周期中的缺失动画，且不全局 hook `SetAnimation`。`OptionalUiScenePatchTests` 固定了只作用于 `MeiLinMod/scenes/`、候选动画顺序和异常时返回基础流程。
- `optional-card-visual`
  - 动态卡图 overlay。
  - 古旧/混沌卡框、费用层、稀有度装饰和卡框状态清理。
  - 当前仍需保留为运行时 UI 补丁，因为它需要在 `NCard.UpdateVisuals`、`Reload`、`_EnterTree`、`OnFreedToPool` 等节点生命周期中修正和清理 Godot 子节点。`OptionalCardVisualPatchTests` 固定了动态卡图资源、Ancient 槽位、自定义卡框标记，以及放大卡图下费用和类型文本 overlay 的结构。
- `optional-content`
  - Orobas、Prismatic Gem、Colorful Philosophers、Touch of Orobas、Archaic Tooth、Dusty Tome、火用于下次等内容兼容补丁。
  - 当前仍需保留为运行时补丁，因为它们改的是原游戏事件/遗物奖励分支，不是单纯内容注册。`OptionalContentPatchTests` 固定了这些补丁必须保持非 critical，并验证 Dusty Tome 不会把美铃的 Archaic Tooth 转化目标重新放回候选池。

音频补丁只挂玩家相关死亡语音和角色自定义 SFX，不再 patch 怪物死亡音效目标。这是之前“怪物声效丢失”的主要风险点。

## 战斗动画与 VFX

迁移后的动画/VFX 目标是：角色动作、命中特效、自身特效和位移保持互斥且可中止，避免打断后卡在远处或卡在攻击动画。

当前规则：

- 基础攻击走 `attack_play1`。
- 多段攻击按 `attack_play1`、`attack_play2` 交替。
- 超过 3 段时，最后一段使用 `u2_attack_play` 收尾。
- 被中止的多段攻击会丢弃剩余动画段。
- VFX 命令和角色动画使用同一套 `BuildAttackCommands(...)` 规则，避免两边漂移。

仍需游戏内确认：

- 击杀敌人后角色归位。
- 攻击结束播放回 idle 的动画。
- 背身立绘 `idle -> b_idle -> b_idle_to_idle -> idle` 状态机不被点击/键盘/手柄路径打断。
- 自身特效位置、脚底特效位置和角色缩放一致。

## Godot 脚本与导出

迁移策略：

- 场景实际引用的 C# 脚本放到 `GodotScripts`。
- 普通运行时代码留在 `MeiLinModCode`，由 DLL 承载。
- `MeiLinModCode/.gdignore` 阻止普通 C# 源码进入 `.pck`。
- 导出期间临时写入编辑器插件和工具目录的 `.gdignore`，导出后删除。
- `export_presets.cfg` 排除 `packages/**`、`tmp/**`、`tests/**`、`tools/**`、`docs/**`、`logs/**`、`MeiLinMod/newcard/**`、编辑器插件目录和 `**/~*.TMP`。

验证门禁：

- `.pck` 必须包含 `GodotScripts/...` 场景脚本。
- `.pck` 不应包含 `MeiLinModCode/`、测试、文档、工具、旧 BaseLib 包、编辑器插件和 spine 临时 dll。
- Spine GDExtension 必须保留 `windows.editor.msil`、`windows.debug.msil`、`windows.release.msil` alias。

## TestTheSpire 验证

测试项目：`tests/MeiLinMod.Tests`

常用命令：

```powershell
dotnet msbuild tests\MeiLinMod.Tests\MeiLinMod.Tests.csproj -restore -t:RunSts2Tests -p:Sts2Path="d:/steam/steamapps/common/Slay the Spire 2" -p:IsInnerGodotExport=true
```

单测过滤示例：

```powershell
dotnet msbuild tests\MeiLinMod.Tests\MeiLinMod.Tests.csproj -restore -t:RunSts2Tests -p:Sts2Path="d:/steam/steamapps/common/Slay the Spire 2" -p:IsInnerGodotExport=true -p:Sts2TestArgs=--sts2-test-filter=StrikeMeilin_deals_six_damage
```

YukiMod 设置兼容示例：
```powershell
dotnet msbuild tests\MeiLinMod.Tests\MeiLinMod.Tests.csproj -restore -t:RunSts2Tests -p:Sts2Path="d:/steam/steamapps/common/Slay the Spire 2" -p:IsInnerGodotExport=true -p:Sts2TestDisabledModIds=KaylaMod -p:Sts2TestArgs=--sts2-test-filter=SettingsTests
```

测试环境会禁用：

- BaseLib
- Fei
- KalipeiMod
- KaylaMod
- YukiMod
- StS2_MeiLinTexturePack

覆盖范围：

- 基础战斗。
- 角色模板。
- 内容注册 public entry。
- 卡池归属。
- 本地化 key。
- Godot 脚本路径和预加载资源。
- manifest 和 csproj 依赖。
- `.pck` 内容排除。
- Ritsu patch target 解析。
- patcher 分组。
- 音频补丁不 hook 怪物死亡音效。
- 共享设置 schema。
- 多段攻击命令队列。
- VFX 配置。
- 遥测预留。

注意：本地 C 盘空间不足时，Headless 启动可能出现基础游戏资源加载失败。测试项目已经把 `APPDATA`、`XDG_DATA_HOME`、`TEMP`、`TMP` 指到仓库内 `tmp/sts2-combat-tests/MeiLinMod.Tests`，但系统盘只有约 1GB 时仍可能触发偶发启动失败。遇到这种情况应释放系统盘或重跑一次确认。

## 遥测

当前实现已从预留状态切换为启用状态：

- `MeiLinTelemetryBootstrap` 注册 RitsuLib applicant，并由入口调用。
- `MeiLinTelemetryConfiguration` 使用 `PostHogTelemetryAdapter`，host 为 `https://us.i.posthog.com`。
- `RunHistory` 已启用，但只采集包含 MeiLin 角色的跑局。
- 不再注册 `Custom` request，也不再发送本地平衡摘要事件。
- `run_history.completed` 是完整平衡数据主源，卡牌选择率和胜率从完整 RunHistory 解析。

### 历史预留记录

RitsuLib telemetry 教程要求先声明 applicant 和数据类别，再由用户授权决定是否发送。MeiLinMod 当前只保留脚手架：

- `MeiLinTelemetryBootstrap` 可以通过 `TelemetryRegistry.RegisterApplicant(...)` 注册 applicant。
- 入口尚未调用 `MeiLinTelemetryBootstrap.Initialize()`。
- `MeiLinTelemetryConfiguration` 返回 `DisabledTelemetryAdapter`。
- `IngestEndpoint` 为空。
- 不使用 `HttpJsonTelemetryAdapter` 或 `PostHogTelemetryAdapter`。

后续接入后端时参考 `STSVWB` 和 `STSVLogs`：

- Mod 侧只声明 applicant 和通用 request。
- 服务侧接收 `/ingest`，写入 `events` 表。
- 半结构化字段使用 `properties` / `payload` JSONB。
- 不把真实 ingest key 写进 Mod。

隐私边界：不要上传玩家姓名、路径、完整日志或任何本地文件内容；自定义 MeiLin 事件先独立定义 schema，再作为显式类别接入。

## 面向 Yuki 后续迁移的约束

后续 YukiMod 迁移到 RitsuLib 时，建议复用 MeiLin 这套形状：

- 入口拆成 `*RitsuMigration`。
- 内容注册集中到 `*RitsuContentRegistration.Register(assembly)`。
- 本地 `PoolAttribute` 继续承担池归属。
- 显式固定旧 public entry。
- 设置页使用 RitsuLib 原生设置页，不继续扩展 `NSettingsScreen` 注入补丁。
- 共享设置继续读写 `chaosmod/xcskin_settings.json`，除非单独做旧配置导入。
- 补丁按 `optional-ui`、`optional-audio`、`optional-overlay`、`optional-combat-animation`、`optional-scene`、`optional-card-visual`、`optional-content` 分组。
- 音频补丁不要 patch 怪物死亡音效路径。
- 遥测只保留独立 bootstrap/configuration，不散进卡牌、VFX、音频或动画补丁。
- 用 TestTheSpire 固定 public entry、本地化、卡池、patch target、共享设置和关键战斗流程。

## 剩余人工验证范围

自动测试已经覆盖 RitsuLib 依赖、内容注册、设置 schema、补丁 target、导出排除、PCK 内容、VFX 配置、动态卡图结构、背身立绘状态入口、音频补丁边界和多段攻击位移保护。以下内容仍建议进游戏确认：

- 使用 STS2 `v0.108.0` 重新导出的 `MeiLinMod.pck` 启动游戏后，最新日志应出现 RitsuLib `0.4.50 [compat branch: 0.108.0]` 初始化和 optional patcher 注册日志，而不是旧 BaseLib 初始化路径。
- 无 YukiMod / 有 YukiMod 两种情况下，设置页都应能看到美铃共享设置项，特效、背身立绘、动态卡图、音量和立绘缩放/偏移应读写同一份共享配置；自动测试已覆盖初始化和共享配置 schema，实际 UI 排版仍需进游戏确认。
- 放大卡图时，费用图标、费用数字、卡牌类型文本和动态卡图层级不应被自定义卡框遮挡。
- 鼠标进入手牌时角色应从 `idle` 进入 `idle_to_b_idle -> b_idle`；鼠标离开后约 `0.2s` 播放退出；点击/拖牌时不应误触发退出闪烁。
- 打出攻击牌时，应按 `attack_play1/attack_play2/.../u2_attack_play` 规则播放动作和特效；多段击杀或目标提前消失时角色必须回到原位并最终回到 `idle`。
- 攻击位移时美铃应在怪物图层上方，但不应覆盖 UI 层；返回后 sibling/Z 状态应恢复。
- 美铃攻击、施法、死亡和特殊卡语音应正常播放；怪物自身音效不应因为美铃音频补丁丢失。
- 基础防御、能力牌、技能牌、火龙宝石、攻防一体和 debuff 流程应播放对应动作/VFX，并在结束后回到 `idle`。

## 提交前检查清单

建议提交范围：

- `MeiLinMod.csproj`、`MeiLinMod.json`、`export_presets.cfg`。
- `MeiLinModCode/Entry/`、`MeiLinModCode/Migration/`、`MeiLinModCode/Telemetry/`。
- 迁移后的 `MeiLinModCode/Cards/`、`Character/`、`Patches/`、`Powers/`、`Potions/`、`Relics/`、`Vfx/`、`Mechanics/` 等运行时代码变更。
- `GodotScripts/` 中场景实际挂载的 C# 脚本，以及旧位置对应脚本删除。
- `MeiLinMod/.gdignore` / `MeiLinModCode/.gdignore` / `MeiLinMod/newcard/.gdignore` 等用于导出排除的 marker。
- 与动态卡图、背身立绘、VFX、Spine alias、PCK 导出相关的 `.tscn` / `.gdextension` / 资源配置变更。
- `tests/MeiLinMod.Tests/` 的 TestTheSpire 测试项目和专项测试。
- `docs/project-overview.zh-CN.md`、`docs/ritsulib-migration-plan.zh-CN.md`。

不要提交：

- `.gitignore`、`local.props` 的本地改动。
- `.codex_tmp/`、`tmp/`、`logs/`、测试 `bin/obj/mod/mod-deps`。
- 导出时临时生成的 `addons/*/.gdignore` marker。
- 旧 spine 临时 dll：`addons/spine/windows/~libspine_godot*.TMP` 不应作为文件保留；若它们是已跟踪文件，应提交删除。

1. `dotnet build tests\MeiLinMod.Tests\MeiLinMod.Tests.csproj -p:IsInnerGodotExport=true`
2. 0.108 迁移依赖专项 TestTheSpire：`SUMMARY total=9 passed=9 failed=0 skipped=0`
3. 全量 TestTheSpire：`SUMMARY total=54 passed=54 failed=0 skipped=0`
4. BaseLib 共存设置冒烟测试：`SettingsTests` 共 3 项通过；若使用 BaseLib v3.2.1，允许日志中出现 BaseLib 自身的旧 `CustomPile` 初始化异常，但 MeiLinMod 不能加载失败。
5. `git diff --check`
6. 确认 `.pck` 不包含 `MeiLinModCode/`、`tests/`、`docs/`、`tools/`、旧 BaseLib package、编辑器插件和 `~libspine_godot` 临时文件。
6. 确认新增 `GodotScripts` 文件和旧脚本移动成对出现。
7. 确认 `.codex_tmp`、`tmp/sts2-combat-tests`、测试 `bin/obj/mod/mod-deps` 没有作为未跟踪文件出现。
8. 游戏内检查最新日志是否出现：
   - `[MeiLinRitsuMigration] Initializing RitsuLib integration.`
   - `[MeiLinRitsuMigration] Optional patchers registered: ui, audio, overlay, combat-animation, scene, card-visual, content.`
   - `[MeiLinRitsuMigration] RitsuLib integration initialized.`
