# MeiLinMod 0.113 兼容性检查

检查日期：2026-11-13。首次检查仅审计；随后按用户要求完成修改、无界面完整回归、D3D12 图形模式音效验证和部署。下方保留 v0.4.6 的原始检查证据，更新结果见本节。

## 更新结果：v0.4.7

后续修复：放弃对局后，结算界面直接调用 RitsuLib 的 `TryPlayCue(..., "die")`，没有经过战斗的 `Dead` -> `death` 映射。原生 Spine 对缺失动画只记录错误，框架的 C# 异常回退未继续尝试正确名称。新增仅限 MeiLin 的 cue 补丁，将主名称 `die` 改为 `death`，保留其他角色和原有死亡逻辑。

该补丁的 D3D12 图形定向测试 1/1 通过，已目验截图中的死亡姿势；注册测试 4/4 通过，补丁总数更新为 43，失败数 0。两次测试退出码均为 0，图形日志仅保留驱动 PSO caching 提示。此轮仅复测受影响路径，未重跑下表中的 75 项完整回归。日志为 `tmp/migration113-fixed/death-graphics-stdout.log`、`death-registration-stdout.log`，截图为同目录 `death-animation.png`。已重新导出并部署三件套，编译和导出均为 0 警告、0 错误。

已完成：

- 默认编译目标改为 113，补齐路径、环境变量、条件编译及嵌套导出传递；manifest 最低游戏版本 v0.113.0，RitsuLib 编译和最低运行依赖统一为已验证的 0.6.11。
- 修复 Gloomy 死亡回调与立绘死亡清理补丁的 `CardModel source` 参数契约，保留原有补召唤、死亡保护、本地角色判断。
- 将火龙心脏、全心全灵纳入 113 的现代 `CardLocation` 分支。
- 将战绩请求适配为 `RunHistoryFiltered`，通过 `TelemetryCaptureContext.SourceData` 保留仅 MeiLin 跑局的过滤。
- 更新测试 manifest 和版本断言；补齐已有重放动画补丁与大招演出设置的旧断言。
- 嵌套导出沿用指定输出目录，导出时临时忽略 `tmp`，并排除 skill 文档。
- 在 `NAudioManager.PlayOneShot(string, Dictionary<string, float>, float)` 增加 MeiLin 自定义音效拦截，覆盖绕过原 `SfxCmd` 拦截的调用；保留原有角色范围、音量、重放抑制和普通音效透传。
- 修正背景脚本 UID；删除背景、营地和攻击 VFX 三处无效材质 UID，保留资源路径。
- Spine 重复注册来自本地 `.godot/extension_list.cfg` 同时加载正式扩展及 `tmp/roswitha_delivery` 副本。移除缓存中的临时副本记录，重新导出已无此警告；未更换引擎或扩展版本。

验证结果：

| 检查 | 结果 |
| --- | --- |
| 主工程及测试程序集编译 | 0 警告，0 错误 |
| Godot PCK 导出 | 0 警告，0 错误 |
| 最终完整回归 | 单次 75/75 通过，无跳过，退出码 0，stderr 为空 |
| D3D12 图形模式音效定向验证 | 5/5 通过，退出码 0；仅驱动 PSO caching 未实现提示 |
| 新增 113 行为回归 | 4/4 通过：野兽死亡补召唤、火龙心脏回手并消耗气/获得能量、全心全灵耗尽、死亡清理钩子及保护条件 |
| 新增音效及资源回归 | 实际音频管理器调用只生成一个播放正确资源的播放器；三个修正场景均可从 PCK 加载并实例化 |
| MeiLin 补丁 | 七组 42/42 应用成功，失败数 0 |
| VFX 预热 | 52/52，失败数 0 |
| 新 DLL 静态成员检查 | 334 个游戏成员引用均可解析；1226 个虚方法重写无失配候选，无未解析基类 |
| 加载产物 | 本机 0.113.0、RitsuLib 0.6.11/0.113.0 分支，实际加载 MeiLin v0.4.7；安装与暂存 DLL/PCK 哈希一致 |

测试使用工作区内的隔离 APPDATA、临时文件和存档；游戏原有用户存档与工坊文件未覆盖。本轮仅临时安装 `MeiLinMod.Tests`、`TestTheSpire`、`xunit.v3.assert`，验证后归档回工作区；保留原有 RitsuLib 和正式 MeiLinMod。

测试框架也有二进制兼容问题：TestTheSpire 0.1.2 和 0.1.4 都引用旧版 `RunState.CreateForTest`，缺少新增的 `isCustomGame` 参数。新增 `tools/build_testthespire_113.ps1`，只复制并重编译框架到工作区，未修改旁边的 TestTheSpire 工程。测试工程优先使用该重编译 DLL，并显式复制进测试依赖包。

火龙心脏会立即返回手牌，通用 `Play` 测试助手等待“观察到离开手牌”会误超时。该用例改用实际排队的 `PlayCardAction`，等待动作结束后检查气、能量和结果牌堆，未改卡牌行为。

产物及日志：

- 可安装三件套：`tmp/migration113-fixed/mods/MeiLinMod/MeiLinMod.dll`、`MeiLinMod.json`、`MeiLinMod.pck`。
- 已部署：`D:\STEAM\steamapps\common\Slay the Spire 2\mods\MeiLinMod`；原安装备份：`tmp/migration113-fixed/previous-installed-MeiLinMod/`。
- 最终编译：`tmp/migration113-fixed/test-build.log`。
- 完整回归：`tmp/migration113-fixed/all-tests-stdout.log`、`all-tests-stderr.log`。
- 图形模式音效验证：`tmp/migration113-fixed/graphics-audio-stdout.log`、`graphics-audio-stderr.log`。
- 导出过程：`tmp/migration113-fixed/export.log`。
- 更新后的可复用 skill：`skills/sts2-migrate-to-113/`，同步到个人 skill 目录。

边界：完整回归使用无界面模式，图形模式只验证 D3D12 启动及音效路径，未逐场景检查动画观感或实际网络联机。最终测试未再出现 UID 回退及 `meilin_select` 音效路径错误。此前真实游戏日志中，D3D12 分配失败同时伴随系统可用内存约 0.39 GiB，不能宣称本次资源引用修复已解决低内存问题；未修改全局渲染器或关闭其他进程。旧 Mark 存档引用未加载的 Mark 角色是另一项问题，未改动该存档或其他 Mod。旧版本路由保留，但本轮只验证 0.113。

## 原始检查结论：v0.4.6

MeiLinMod v0.4.6 可以完成启动注册，但有两处已确认的 API 不兼容，均与死亡回调新增 `CardModel source` 参数有关：

| 优先级 | 问题 | 证据与影响 |
| --- | --- | --- |
| 高 | Gloomy 首领死亡监听重写失配 | `GloomyPrimeMonster.cs:154` 编译报 CS0115。旧 DLL 的回调签名也未匹配新虚方法，野兽阵亡后的补召唤计数存在失效风险。 |
| 中 | 背身立绘死亡清理补丁失败 | `MeiLinBattleReadyOverlayPatches.cs:84` 仍指定旧版目标；最新日志 531–537 行明确记录找不到 `Hook.AfterDeath`。死亡时不能依靠该补丁重置立绘及角色悬停动画状态。 |

本次没有实际运行 Gloomy 补召唤或死亡清理战斗；上述行为影响来自代码调用关系和 API 失配，不能表述为已完成实战复现。

## 检查基线

- 安装版本：`v0.113.0`，commit `16ac0fe3`，构建时间 `2026-11-12T18:09:24+00:00`。
- 本机游戏：`D:\Steam\steamapps\common\Slay the Spire 2`。
- 日志：`C:\Users\lozalia\AppData\Roaming\SlayTheSpire2\logs\godot.log`，本次启动使用 0.113。
- 实际加载 MeiLin：Workshop `3747511784\MeiLinMod.dll` 和同目录 PCK；不是仅凭本地 mods 目录推断。
- RitsuLib：`0.6.11 [compat branch: 0.113.0]`，框架自身各补丁组均无失败。
- 项目仍使用目标 `111`、编译依赖 `STS2.RitsuLib 0.5.11`，Mod 版本 `v0.4.6`。
- 前一个轮转日志虽然文件名含 2026-11-13，但内部版本为 `v0.111.0`，且没有加载 MeiLin，不能作为 MeiLin 更新前后对照。

## 两处修改位置

### Gloomy 首领

文件：`MeiLinModCode/Monsters/Gloomy/GloomyPrimeMonster.cs:154`。

旧签名：

```csharp
public override Task AfterDeath(
    PlayerChoiceContext choiceContext, Creature creature,
    bool wasRemovalPrevented, float deathAnimLength)
```

0.113 对应签名：

```csharp
public override Task AfterDeath(
    PlayerChoiceContext choiceContext, Creature creature, CardModel? source,
    bool wasRemovalPrevented, float deathAnimLength)
```

迁移时保留方法体中的死亡阻止判断、阵营判断、野兽类型判断和待召唤数量累计。`GloomySummonPower.BeforeSideTurnStart` 依赖这里维护的 `PendingSummons`；不能以“进游戏没崩溃”代替该路径验证。

### 背身立绘清理

文件：`MeiLinModCode/Mechanics/CardHoldOverlay/MeiLinBattleReadyOverlayPatches.cs:84`。

目标需要从：

```csharp
PatchTarget.Method(typeof(Hook), nameof(Hook.AfterDeath),
    typeof(IRunState), typeof(ICombatState), typeof(Creature),
    typeof(bool), typeof(float))
```

改为：

```csharp
PatchTarget.Method(typeof(Hook), nameof(Hook.AfterDeath),
    typeof(IRunState), typeof(ICombatState), typeof(Creature),
    typeof(CardModel), typeof(bool), typeof(float))
```

Postfix 如果不使用来源卡牌，可以继续省略该参数。保留本地玩家、MeiLin 角色和 `!wasRemovalPrevented` 的检查。

`tests/MeiLinMod.Tests/OptionalOverlayPatchTests.cs:80` 还断言旧目标签名，需要一起更新。现有 `PatchRegistrationTests.All_ritsu_patch_targets_resolve` 可以验证目标解析，无需另外堆叠重复测试。

## 构建和版本配置待迁移

这些是后续发布 0.113 版本的工作，不能与当前已发生的运行错误混为一谈：

1. `MeiLinMod.csproj` 尚不接受 `Sts2TargetVersion=113`，需补齐路径、环境变量、编译符号、校验和 Godot 子进程传递。
2. `HuoLongXinZang.cs:42` 与 `QuanXinQuanLingPower.cs:32` 的条件编译需要把 113 纳入 `CardLocation` 分支；否则新增 `STS2_113` 后反而会落入旧版 API。
3. 主 manifest、测试工程、生成 manifest 和迁移断言仍以 0.111 为基线，需要按最终支持范围同步。
4. 编译依赖与运行依赖应选择已验证支持 0.113 的组合。本次实际运行的是 RitsuLib 0.6.11；不能据此宣称它一定是最低兼容版本或最新版本。
5. 常规 Build 会自动复制到游戏目录；`IsInnerGodotExport=true` 只阻止 PCK 导出，并不阻止 DLL/manifest 复制。

## 已执行的验证

仅编译检查，使用项目现有 111 条件分支，引用路径显式指向本机 0.113：

```powershell
$env:NUGET_FALLBACK_PACKAGES = (Resolve-Path -LiteralPath '.\packages').Path
dotnet build .\MeiLinMod.csproj -t:Compile `
  -p:Sts2Path111="D:\Steam\steamapps\common\Slay the Spire 2" `
  -p:IsInnerGodotExport=true -v:minimal
```

还原成功，编译首先因 Gloomy 的旧 `AfterDeath` 重写失败。声明错误可能阻止编译器继续检查全部方法体，因此修复后仍需重新编译。

读取已发布 DLL 的 Mono.Cecil 静态检查结果：

- 334 个游戏成员引用全部可解析。
- 递归包含嵌套类型后检查 1226 个虚方法重写，只有 Gloomy 的 `AfterDeath` 未匹配当前基类签名。
- Workshop DLL 与本地 2026-10-20 发布副本的 SHA256 一致：`FDD8EE41039911F2D55E00B273333E9A51A6D06B3070DC5F51ADDC0626AA87B3`。

该检查不执行 Mod，不覆盖反射目标、Harmony 参数注入和实际战斗行为。没有运行全量测试，因为主工程尚不能编译。

日志还表明：

- 七个补丁组共 41 个目标，40 成功，1 个死亡清理目标失败。
- MeiLin 启动 VFX 预热 `52/52`，失败数 0。
- 角色和内容注册已经完成，没有发现 MeiLin 的初始化异常。

## 资源警告与未归属错误

`meilin_bg.tscn:5` 记录的脚本 UID 与当前 `.cs.uid` 不一致；背景及攻击 VFX 场景引用的 `spine_pma.tres` UID 也无法在运行时解析。Godot 明确回退到文本路径，当前没有由此确认资源加载失败。后续修复引用并重新导出即可，不需要据此重做 Spine 或 VFX 系统。

`Invalid Task ID` 出现在 Godot 脚本桥接调用栈中，旧 0.111 日志在未加载 MeiLin 时也有此错误，尚不能归因于 MeiLin 或本次更新。退出阶段的 RID/resource 泄漏提示同样缺乏单 Mod 对照，保留为未归属项。

## 其他 Mod 的后续入口

| Mod | 本次证据 | 后续优先检查 |
| --- | --- | --- |
| Yuki | 最新日志确认死亡清理目标失配；随后选人补丁报 `Parameter "characterModel" not found`，初始化抛异常 | `Hook.AfterDeath`；两个 `SelectCharacter` Postfix 从 `charSelectButton.Character` 读取角色；共享 Gloomy 回调 |
| Shu / Mark | 仅在旧 0.111 日志出现，本轮 0.113 未加载 | 先对各自工程编译，再检查回调和选人补丁，不能直接认定已坏或已兼容 |
| Yusha | 最新日志显示入口和内容注册完成；BaseLib 对部分 SavedProperty 打警告 | 独立核对战斗、保存/加载与各框架实际序列化路径，不能仅凭 BaseLib 警告断言丢档 |
| 其他皮肤 Mod | 未逐个验证 | 选人页、背景、持卡/出牌、死亡和演出取消时的补丁与模型恢复 |
| MinionLib | 旧集合伤害补丁目标找不到 | 集合 `CreatureCmd.Damage` 新增的可选 `ModifyDamageHookType` 参数 |
| GoldTransferMod | `MerchantTransferGoldEntry` 类型加载失败 | 新版 `OnTryPurchaseInternal(MerchantInventory, bool)` 返回契约 |
| MultiplayerModSync | 声明的 DLL 不存在 | 安装或打包完整性，与 MeiLin 无关 |

可复用 skill 源码：`skills/sts2-migrate-to-113/SKILL.md`，附 API 证据和只读程序集检查脚本。后续迁移优先完成 MeiLin 两处 API 修复与版本配置，再进行补丁解析、死亡清理和 Gloomy 补召唤的必要验证。
