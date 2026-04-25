# 打击/防御判定规则

## 结论

- 牌面、Power、遗物文本写的是“打击”或“防御”时，代码必须按 `CardTag.Strike` / `CardTag.Defend` 判定。
- 只有文本明确写“基础打击 / 基础防御”或明确要生成角色起始牌时，才允许走 `IsBasicStrike` / `IsBasicDefend` / `CreateBasicStrikeForPlayer` / `CreateBasicDefendForPlayer`。

## 为什么

- 2026-04-24 的提交 `91083d0 (0.104+card anime)` 把 `BasicStrikeDefendHelper.IsBasicStrikeOrDefend` 一类 helper 从宽匹配 tag 收窄成了基础牌语义。
- 但大量旧牌效仍在调用这些 helper，导致像 `剑柄打击` 这种带 `Strike` tag 的牌不再被 `攻防一体`、`心流`、`运气`、`屏气凝神` 等效果识别。

## 以后怎么写

- 触发、筛选、自动打出、费用修改、重放次数、强化数值：
  - 优先用 `BasicStrikeDefendHelper.IsStrikeCard`
  - 优先用 `BasicStrikeDefendHelper.IsDefendCard`
  - 同时接受两者时用 `BasicStrikeDefendHelper.IsStrikeOrDefendCard`
- 只在“基础牌语义”场景使用基础 helper：
  - 生成本角色基础打击/防御
  - 明确写着“基础打击/基础防御”的遗物或效果
  - 需要排除普通打击的随机池过滤

## 特别约束

- `变化莫测` 和 `十八般武艺` 的随机池必须继续排除普通/起始打击。
- 这两个效果应继续依赖 `RandomStrikeHelper` 的“非基础打击”筛选，不要为了修复 tag 判定去改窄全局的打击识别。
