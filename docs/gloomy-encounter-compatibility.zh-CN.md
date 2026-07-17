# Gloomy 敌群跨 Mod 兼容协议

Fei、YukiMod、MeiLinMod 可以分别携带完整的 Gloomy 敌群实现并独立运行。共同加载时遵守以下协议：

- 公共开关保存在 `chaosmod/gloomy_encounter_settings.json`，默认开启。
- 运行时开关键为 `CHAOSMOD_GLOOMY_ENCOUNTER_ENABLED`。
- 每个 Mod 初始化时发布 `CHAOSMOD_GLOOMY_PROVIDER_<ModId>`。
- 提供者优先级固定为 `Fei > YukiMod > MeiLinMod`。
- 只有当前最高优先级提供者的遭遇通过 `IsValidForAct`；其余实现仍可完成模型注册，但不会重复进入怪物池。
- 各 Mod 使用自己的资源根目录和本地化 ID，禁止引用另一个 Mod 的 PCK 路径。

三个 Mod 中的 `GloomyEncounterSharedSettings` 必须保持相同的文件名、运行时键和提供者优先级。

## 逃跑 Token

- Fei、YukiMod、MeiLinMod 分别注册自己的中立 Token、公共 ID、本地化与资源，不能引用另一个 Mod 的卡牌类型。
- 卡牌固定为 0 费技能、Token 稀有度、保留、消耗、目标自身。
- 只有实际生成本次 Gloomy 遭遇的提供者负责发牌；以提供者自己的 `GloomyPackEncounter` 类型判断，不能通过场上是否存在 Gloomy 怪物判断。
- 战斗开始且正常起手抽牌之前，每位玩家的手牌中生成且只生成一张；多人模式中任意玩家打出都会让整个队伍离开遭遇。
- 禁止对玩家调用 `CreatureCmd.Escape`。打出卡牌后应标记遭遇为玩家逃跑，并对当前敌人快照逐个调用原版 `CreatureCmd.Escape`，让原版战斗结束流程安全收尾。
- 玩家逃跑后 `ShouldGiveRewards` 返回 `false`，金币比例固定为 0；逃跑标记必须通过遭遇的 `SaveCustomState` / `LoadCustomState` 持久化。
- 该安全方案在引擎内部仍走胜利收尾，因此胜利 Hook、进度统计和遥测可能运行，但不会出现战斗奖励界面。
