# MeiLinMod 项目文档

## 1. 项目定位

`MeiLinMod` 是一个面向《Slay the Spire 2》公开测试版的自定义角色 Mod。

项目使用：

- `Godot 4.5.1`
- `C# / .NET 9`
- `BaseLib`
- `Harmony`

构建结果由两部分组成：

- `MeiLinMod.dll`：游戏逻辑、角色、卡牌、能力、遗物、补丁
- `MeiLinMod.pck`：Godot 资源包，包含场景、图片、音效、Spine 资源、本地化

## 2. 目录结构

### 根目录

- `MainFile.cs`
  - Mod 初始化入口。
- `MeiLinMod.csproj`
  - .NET / Godot 工程配置，定义依赖、构建和导出流程。
- `MeiLinMod.json`
  - Mod 清单。
- `project.godot`
  - Godot 工程配置。

### 代码目录

- `MeiLinModCode/Character`
  - 角色定义、卡池/遗物池/药水池、动画桥接。
- `MeiLinModCode/Cards`
  - 自定义卡牌实现。
- `MeiLinModCode/Powers`
  - 自定义能力、姿态、资源和状态效果。
- `MeiLinModCode/Relics`
  - 自定义遗物。
- `MeiLinModCode/Potions`
  - 自定义药水。
- `MeiLinModCode/Patches`
  - Harmony 补丁，处理兼容、替换和引擎行为修正。
- `MeiLinModCode/Services`
  - 运行时服务，例如音频播放。
- `MeiLinModCode/StanceVfx`
  - 姿态相关特效控制。
- `MeiLinModCode/HoverTips`
  - 自定义关键词说明与悬浮提示。
- `MeiLinModCode/Extensions`
  - 路径、命名等辅助扩展方法。

### 资源目录

- `MeiLinMod/scenes`
  - 角色、背景、图标、特效场景。
- `MeiLinMod/images`
  - 卡图、能力图标、遗物图标、UI 素材。
- `MeiLinMod/sound`
  - 角色语音与技能音效。
- `MeiLinMod/spine`
  - 角色 Spine 资源。
- `MeiLinMod/localization`
  - 中英文本地化文本。

## 3. 初始化流程

入口文件是 `MainFile.cs`。

启动时主要完成以下工作：

1. 使用 `ScriptManagerBridge.LookupScriptsInAssembly` 注册程序集中的脚本。
2. 创建 `Harmony` 实例并执行 `PatchAll()`。
3. 打印关键补丁目标是否成功挂载，便于排查兼容问题。

这意味着项目是一个混合型 Mod：

- 一部分通过 BaseLib 的模型注册机制接入游戏。
- 一部分通过 Harmony 修改或修正游戏原有逻辑。
- 一部分通过 Godot 场景和资源包提供可视化表现。

## 4. 角色定义

角色定义位于 `MeiLinModCode/Character/MeiLinMod.cs`。

当前角色特征包括：

- 角色 ID：`MeiLinMod`
- 性别：`Feminine`
- 初始生命：`75`
- 初始遗物：`XiangzuLegacyRelic`
- 初始卡组：
  - `AttackDefenseUnity`
  - `FireDragonGem`
  - `StrikeMeilin` x4
  - `DefendMeilin` x4

角色还定义了：

- 自定义立绘、营火、商店、选人界面和地图标记资源
- 自定义攻击、施法、死亡、选人音效
- 自定义战斗动画状态
- 额外预加载资源路径，避免姿态特效在首次使用时缺资源

## 5. 核心玩法机制

### 5.1 香族传承

核心起始遗物是 `XiangzuLegacyRelic`。

战斗开始时它会：

- 施加 `XiangzuLegacyPower`
- 移除御姿态
- 默认进入攻姿态

`XiangzuLegacyPower` 是整个角色玩法的中枢，负责：

- 记录当前姿态
- 处理姿态切换
- 触发姿态切换收益
- 刷新姿态特效
- 同步依赖姿态的能力数值

### 5.2 攻 / 御姿态

角色存在两种姿态：

- 攻姿态：由 `StanceGongPower` 表示
- 御姿态：由 `StanceYuPower` 表示

姿态切换后，系统会：

- 更新角色身上的姿态能力
- 播放对应语音
- 刷新姿态光效
- 结算“切换姿态时”的附加收益
- 强制角色回到正确的待机动画

### 5.3 气槽与气

资源系统由 `QiCounterPower` 与 `QiPower` 共同实现。

`QiCounterPower` 负责：

- 记录当前气槽进度
- 在攻击命中或被攻击命中时累积气槽
- 达到阈值后转化为 1 层 `QiPower`

气槽阈值并不是固定值。下一层气所需槽位会受到以下因素影响：

- 当前已有的气层数
- `TongQiaoPower`
- `XiangzuSpiritPower`
- `QiRequirementIncreasePower`

### 5.4 气的属性转化

`QiPower` 本身不直接写死为力量或敏捷，而是根据姿态动态映射：

- 攻姿态：每层气提供 `+1 Strength`
- 御姿态：每层气提供 `+1 Dexterity`

姿态变化或气层变化时，`QiPower` 会自动刷新已施加的属性差值。

### 5.5 余烬

`EmberPower` 是另一条重要机制线。

效果大致为：

- 每层使目标受到的攻击伤害 `+1`
- 每累计到 `5` 层的整数档位时，额外损失一次最大生命值 `5%`
- 回合结束时层数衰减

该机制可用于：

- 压低敌人承伤阈值
- 强化多段攻击收益
- 形成持续性压制

## 6. 代表性卡牌设计

### `AttackDefenseUnity`

定位是基础启动卡。

效果思路：

- 从抽牌堆和弃牌堆中检索基础攻击/防御牌
- 选出 2 张加入手牌
- 本回合将其费用改为 `0`

### `FireDragonGem`

定位是初始能力牌。

特点：

- `Innate`
- 播放自定义施法音效
- 触发施法动画
- 施加 `FireDragonGemPower`

### `GuiYi`

定位是姿态系统的高阶能力牌。

效果思路：

- 施加双姿态能力 `GuiYiDualStancePower`
- 升级后额外获得气槽
- 手动触发一次“虚拟姿态切换”，使切姿态奖励也能结算

## 7. 内容规模

基于当前代码注册情况，项目内容大致为：

- 池内卡牌：约 `98` 张
- 能力类文件：约 `78` 个
- 池内遗物：约 `10` 个
- 药水：`1` 个
- Harmony Patch 标记：约 `15` 处

按卡牌稀有度粗略统计：

- `Basic`：约 `4`
- `Common`：约 `31`
- `Uncommon`：约 `40`
- `Rare`：约 `30`
- `Ancient`：约 `2`

这些数字反映的是当前代码层面的实现规模，不等同于所有内容都已经过完整平衡与联机验证。

## 8. 音频、动画与表现层

### 音频

`MeiLinAudioService` 负责：

- 拦截或替换部分默认 `SfxCmd`
- 为角色攻击、施法、死亡、选人播放自定义音频
- 为个别卡牌播放专属音效
- 在需要时屏蔽默认音效，避免重叠

### 动画

角色动画有两层处理：

- `MeiLinMod.cs` 中的 `GenerateAnimator`
  - 面向战斗状态机，定义 Idle / Attack / Cast / Hit / Dead / Relaxed 等状态。
- `MeilinCharacterAnimBridge.cs`
  - 在 Godot 节点层面桥接动画触发名与 Spine 动画名，解决命名不一致问题。

### 姿态特效

`MeiLinStanceVfxController` 负责切换姿态时的 aura 场景表现。

当前项目里已存在：

- 攻姿态特效
- 御姿态特效
- 若干测试场景

## 9. 兼容补丁与工程策略

项目并不只依赖公开注册接口，也通过 Harmony 修补了一些游戏或遗物行为。

代表性补丁包括：

- `AncientRelicMeiLinPatch`
  - 处理 `ArchaicTooth`、`DustyTome` 对梅琳卡池和进化牌的适配。
- `SfxCmdMeiLinAudioPatch`
  - 接管部分音效播放逻辑。
- `CharacterAnimationFallbackPatch`
  - 在商店和营火场景中为角色寻找可用的备用动画，避免场景空播。

这类补丁的意义在于：

- 让新角色接入游戏原生遗物/事件逻辑
- 修正官方动画调用与自定义资源之间的接口落差
- 在角色资源尚未完全对齐原版命名时提供兜底

## 10. 构建与导出

`MeiLinMod.csproj` 中包含了完整的构建和导出流程。

主要逻辑如下：

1. 检查 `Slay the Spire 2` 数据目录是否存在。
2. 检查本机 `Godot 4.5.1` 路径是否存在。
3. 编译 DLL。
4. 将 DLL 和 `MeiLinMod.json` 复制到游戏 `mods/MeiLinMod/` 目录。
5. 调用 Godot 的 headless 导出，把 `.pck` 导出到同一目录。

工程上特别强调了一点：

- 当前应固定使用 `Godot 4.5.1`

因为注释中已经明确说明，如果使用比游戏更高的 Godot 版本，游戏可能不会加载导出的 `.pck`。

## 11. 当前已知问题

### 11.1 Mod 清单格式异常

`MeiLinMod.json` 当前看起来不是合法 JSON。

已观察到的问题：

- `description` 字段缺少闭合引号

这类错误可能导致：

- Mod 清单解析失败
- 游戏无法正确显示 Mod 信息
- 构建产物被复制后仍然无法正常加载

### 11.2 编码不一致

当前仓库中文本存在编码混用现象。

已观察到：

- `README.md` 在终端中呈现乱码
- `MeiLinMod.json` 的中文描述也呈现乱码
- 部分本地化文件正常，例如 `localization/zhs/card_keywords.json`

这通常意味着仓库中同时存在：

- 正常 UTF-8 文件
- 以其他编码保存的旧文件

建议后续统一为 `UTF-8`。

## 12. 维护建议

如果后续要继续扩展该项目，建议优先做以下几件事：

1. 修复 `MeiLinMod.json` 的合法性，确保清单可被稳定解析。
2. 统一文档和配置文件编码为 `UTF-8`。
3. 为核心机制补一份“设计文档”，单独记录：
   - 气槽增长规则
   - 姿态切换收益
   - 余烬结算逻辑
   - 联机或多目标场景下的边界行为
4. 为关键能力和补丁补充回归测试思路，尤其是：
   - 气转化阈值
   - 姿态切换后的属性刷新
   - 遗物/事件对自定义卡池的兼容

## 13. 总结

`MeiLinMod` 当前已经不是一个简单的角色原型，而是一个具备完整战斗资源系统、姿态系统、音频表现、动画桥接、资源包导出和兼容补丁的中大型角色 Mod。

项目的核心强项在于：

- 玩法主轴明确
- 表现层投入较多
- 已考虑与原版系统的融合

当前最需要优先收口的部分不是内容量，而是工程稳定性：

- 清单合法性
- 编码一致性
- 若干关键机制的可验证性

这些问题处理完之后，项目会更适合继续做平衡、发布和协作开发。
