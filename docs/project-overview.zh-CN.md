# MeiLinMod 项目概览

## 项目定位

`MeiLinMod` 是面向《Slay the Spire 2》的自定义角色 Mod。当前分支已经进入从 BaseLib 迁移到 RitsuLib 的收尾阶段，核心目标是让内容注册、设置页和补丁注册由 RitsuLib 承接，同时保持原有卡牌 ID、本地化 key、存档兼容、VFX、背身立绘和动态卡图行为不漂移。

构建产物由两部分组成：

- `MeiLinMod.dll`：角色、卡牌、能力、遗物、药水、补丁、设置、VFX 调度和迁移入口。
- `MeiLinMod.pck`：Godot 场景、图片、音频、Spine 资源、本地化、VFX 配置和少量场景挂载脚本。

## 当前技术栈

- Godot 4.5.1
- C# / .NET 9
- STS2.RitsuLib 0.4.50
- RitsuLib optional patcher
- TestTheSpire headless 测试
- Spine Godot GDExtension

BaseLib 已不再作为主 Mod 的编译期或运行时依赖。默认测试环境会显式禁用 BaseLib、YukiMod、Fei 等外部 Mod，用来验证 MeiLinMod 在 RitsuLib 下可以独立加载；另外会单独跑 BaseLib + RitsuLib + MeiLinMod 的设置兼容冒烟测试，确认 MeiLinMod 不依赖 BaseLib 也不会因为 BaseLib 同时存在而阻断加载。

## 目录结构

- `MeiLinModCode/Entry`
  - Mod 初始化入口和全局 using。
- `MeiLinModCode/Migration`
  - RitsuLib 初始化、集中内容注册、本地 `PoolAttribute`。
- `MeiLinModCode/Cards`
  - 自定义卡牌实现。
- `MeiLinModCode/Powers`
  - 能力、姿态、气、余烬和临时状态。
- `MeiLinModCode/Relics`
  - 自定义遗物。
- `MeiLinModCode/Potions`
  - 自定义药水。
- `MeiLinModCode/Patches`
  - RitsuLib `IPatchMethod` 补丁，覆盖内容兼容、音频、卡图、动画和场景 fallback。
- `MeiLinModCode/Mechanics`
  - 背身立绘、共享设置和其他机制。
- `MeiLinModCode/Vfx`
  - 技能特效、攻击位移和命令式 VFX 调度。
- `MeiLinModCode/Telemetry`
  - RitsuLib 遥测接入，当前使用 PostHog US 直连，并注册 BasicUsage、ModInventory、Diagnostics 与 MeiLin 过滤后的 RunHistory。
- `GodotScripts`
  - Godot 场景实际引用的 C# 脚本。普通运行时代码不放在这里，避免进入 `.pck`。
- `MeiLinMod`
  - Godot 资源、场景、图片、音频、Spine、本地化和 VFX 配置。
- `tests/MeiLinMod.Tests`
  - TestTheSpire 自动测试。

## 初始化流程

入口位于 `MeiLinModCode/Entry/MeiLinModEntry.cs`。当前初始化顺序为：

1. `MeiLinRitsuMigration.Initialize()` 注册 RitsuLib 集成。
2. `ScriptManagerBridge.LookupScriptsInAssembly(assembly)` 注册 Godot 脚本桥接。
3. `MeiLinSharedSettings.EnsureSettingsLoaded()` 加载 Yuki/Chaos 共享设置。
4. `CardSpinePortraitPatch.PreloadDynamicPortraitScenes()` 预加载动态卡图场景。
5. `MeiLinCommandVfxCoordinator.PreloadConfiguredScenes()` 预加载 VFX 配置引用场景。
6. `MeiLinAttackMovementController.PreloadMovementEffects()` 预加载位移特效。

入口中不再调用 `harmony.PatchAll()`。补丁由 `MeiLinRitsuMigration.RegisterOptionalPatchers()` 分区注册。

## RitsuLib 集成

迁移入口 `MeiLinRitsuMigration` 负责：

- `ModTypeDiscoveryHub.RegisterModAssembly(MainFile.ModId, assembly)`
- `MeiLinRitsuContentRegistration.Register(assembly)`
- `RitsuLibFramework.RegisterModSettings(...)`
- 创建并应用 optional patcher：
  - `optional-ui`
  - `optional-audio`
  - `optional-overlay`
  - `optional-combat-animation`
  - `optional-scene`
  - `optional-card-visual`
  - `optional-content`

`optional-ui` 里包含一个很窄的 YukiMod 兼容补丁：当旧 YukiMod 在设置界面创建空的 `XCskin_ModSettingsPanel` 时，`NSettingsPanel._Ready()` 会先于 Yuki 填充控件而抛出 `Sequence contains no elements`。MeiLin 只在该面板名、该异常和 VBox 为空同时成立时吞掉这个临时异常，避免影响其他设置面板。

内容注册由 `MeiLinRitsuContentRegistration` 集中处理。它会遍历程序集中的具体卡牌、遗物、药水和能力类型，按本地 `PoolAttribute` 决定卡池/遗物池/药水池，并显式固定旧 public entry：`MEILINMOD-*`。这可以避免 RitsuLib 默认命名导致本地化 key、存档引用或旧卡牌逻辑漂移。

## 角色与核心机制

角色定义位于 `MeiLinModCode/Character/MeiLinMod.cs`。当前角色模板保留：

- 起始生命：75
- 起始金币：99
- 起始遗物：`XiangzuLegacyRelic`
- 初始牌组：
  - `AttackDefenseUnity`
  - `FireDragonGem`
  - `StrikeMeilin` x4
  - `DefendMeilin` x4

核心玩法包括：

- 攻/御姿态切换。
- 气槽与气层。
- 余烬。
- 攻防一体。
- 多段攻击。
- 技能动画与 VFX 命令联动。

## 动画、VFX 和位移

战斗动画由角色模板、`MeiLinBattleAnimationService`、`MeiLinTriggerAnimPatch`、`MeiLinCommandVfxCoordinator` 和移动控制器协作处理。

当前攻击流程按命令序列控制：

- 普通多段攻击循环使用 `attack_play1`、`attack_play2`。
- 超过 3 段时，最后一段使用 `u2_attack_play` 收尾。
- 中途目标死亡或攻击中止时，会丢弃剩余动画段，避免下一次攻击消费旧队列。

VFX 配置主要来自 `MeiLinMod/vfx_configs/1027/generated/meilin_vfx_commands.json`。测试会验证基础流程仍存在，包括 `u1_buff`、`u2_attack`、`u3_buff`、`u4_buff`、普通攻击和 debuff 流程。

## 设置

RitsuLib 设置页当前覆盖：

- 背身立绘开关。
- 战斗特效开关。
- 动态卡图开关。
- 背身立绘缩放。
- 背身立绘 X/Y 偏移。
- 语音音量。

这些设置仍然读写 Yuki/Chaos 共享配置：

- 目录：`chaosmod`
- 文件：`xcskin_settings.json`
- AppDomain key 前缀：`CHAOSMOD_XCSKIN_`

旧的 `NSettingsScreen` 手工注入实现已经移除。当前设置页只走 RitsuLib 注册路径，避免维护两套 UI 和旧乱码文本。

迁移中没有改变共享设置 schema。未来如果要改成 RitsuLib data store，必须先做一次性旧配置导入，并单独验证 YukiMod 兼容性。

## 导出策略

`MeiLinMod.csproj` 和 `export_presets.cfg` 会排除开发目录和废弃资源，避免把普通 C# 源码、测试、文档、工具、旧 BaseLib 包、编辑器插件和临时文件打入 `.pck`。Windows/Linux 本地输出优先使用游戏目录下已经存在的 `mods2`，没有 `mods2` 时回退到旧 `mods`；当两者不同时，会镜像到旧 `mods` 以兼容 TestTheSpire。

Godot 场景实际引用的 C# 脚本被移动到 `GodotScripts`。普通运行时代码保留在 `MeiLinModCode`，并由 DLL 承载。

导出保留 Spine GDExtension 的 `msil` Windows alias，保证 `binary_format/architecture="msil"` 时仍能找到对应库。

## 自动测试

测试项目位于 `tests/MeiLinMod.Tests`。当前覆盖：

- 基础战斗与基础牌可打出。
- 角色模板起始属性、卡组和遗物。
- Ritsu 内容注册 public entry 兼容。
- 卡池归属。
- 本地化 key。
- Godot 脚本路径与预加载资源。
- Manifest 与 csproj 不重新引入 BaseLib。
- `.pck` 不包含开发目录、旧资源和普通 C# 源码。
- Ritsu patch target 解析和 patcher 分组。
- 音频补丁不再挂到怪物死亡音效路径。
- 设置 round-trip 与 Yuki/Chaos schema 兼容。
- BaseLib + RitsuLib + MeiLinMod 同时加载时，设置相关测试能通过。
- 多段攻击队列中止和 `1212 + u2` 收尾规则。
- VFX 命令配置。
- 遥测 PostHog adapter 和 RunHistory 过滤。

最近一次完整结果：

```text
SUMMARY total=54 passed=54 failed=0 skipped=0
```

0.108 迁移依赖专项结果：

```text
SUMMARY total=9 passed=9 failed=0 skipped=0
```

BaseLib 共存冒烟结果：

```text
SettingsTests total=3 passed=3 failed=0 skipped=0
```

该测试使用缓存的 BaseLib v3.2.1。BaseLib 自身在 STS2 v0.108.0 下会记录一个旧 `CustomPile` 补丁初始化异常，但 RitsuLib、MeiLinMod 和 MeiLinMod.Tests 仍能继续加载并完成设置测试；这说明当前已验证的是“MeiLinMod 与已加载 BaseLib 共存不阻断”，不是“BaseLib 3.2.1 已完全适配 0.108”。

## 遥测

遥测功能参考 `STSVWB` 和 `STSVLogs` 的分层方式：配置、注册、适配器和事件发送分离。MeiLinMod 当前通过 `TelemetryRegistry.RegisterApplicant(...)` 注册 applicant，并在 RitsuLib 授权机制下请求 `BasicUsage`、`ModInventory`、`Diagnostics` 与 `RunHistory` 类别。

当前状态：

- `MeiLinModEntry.Initialize()` 已调用 `MeiLinTelemetryBootstrap.Initialize()`。
- `MeiLinTelemetryConfiguration` 使用 `PostHogTelemetryAdapter`，host 为 `https://us.i.posthog.com`。
- `RunHistory` 已启用，但通过 `MeiLinTelemetryBootstrap.IsMeiLinRun` 过滤为包含 MeiLin 角色的跑局。
- 自定义本地平衡摘要已移除，不再在 mod 端生成二次摘要事件。

平衡看板用途：选择率、胜率、死亡层数和完整卡组分析从 `run_history.completed` 读取。胜负字段使用事件属性 `properties.is_victory`，HogQL 中建议用 `lower(toString(properties.is_victory)) IN ('true', '1')` 判断；完整跑局在 `properties.payload.applicant_payload.run_history`。卡牌选择率、入组率、升级率、最终卡组和路线统计应由 PostHog HogQL、ingest 后端或离线 ETL 解析完整 RunHistory 得出。

隐私边界：不要上传玩家姓名、路径、完整日志或任何本地文件内容。直接 PostHog 会把 project API key 暴露在 mod 包里，后续公开长期收集时建议增加代理做限流、字段过滤和来源校验。

## 仍需人工运行时验证

自动测试已经覆盖主要注册、构建和逻辑门禁，但以下内容仍需要游戏内实测：

- 无 BaseLib 时角色能进入游戏并正常显示 MeiLin 卡池、遗物、药水和能力。
- 设置页在有/无 YukiMod 时都能读写背身立绘、特效、动态卡图、语音音量和立绘缩放/偏移；自动测试已覆盖初始化和共享配置 schema，视觉布局仍需进游戏看。
- 鼠标移入/移出手牌、键盘/手柄选牌、打出牌后 `idle -> b_idle -> b_idle_to_idle -> idle` 状态机正常。
- 多段攻击击杀敌人后能归位，并执行攻击结束到 idle 的动画。
- 动态卡图、古旧卡框、费用层、卡牌类型文本没有被放大卡图遮挡。
- 梅铃语音补丁不再吞掉怪物声效。
