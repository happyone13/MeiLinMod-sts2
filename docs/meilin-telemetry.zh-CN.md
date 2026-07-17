# MeiLinMod 遥测实现说明

## 当前接入

MeiLinMod 通过 RitsuLib telemetry 注册独立 applicant：

- ApplicantId: `MeiLinMod`
- Adapter: `PostHogTelemetryAdapter`
- Host: `https://us.i.posthog.com`
- Endpoint: `https://us.i.posthog.com/batch`

RitsuLib 仍然负责用户授权、本地队列、公共属性和发送流程。没有授权对应 request 时，`ITelemetryClient` 调用会直接 no-op。

## 请求类别

- `BasicUsage`：启动、框架/游戏版本、平台、语言、匿名安装 ID。
- `ModInventory`：已加载 mod 列表、版本和加载状态，用于兼容性分析。
- `Diagnostics`：异常和运行时诊断。
- `RunHistory`：完整跑局记录。当前通过 `MeiLinTelemetryBootstrap.IsMeiLinRun` 过滤，只采集包含 MeiLin 角色的跑局。

## 事件

### `run_history.completed`

由 RitsuLib 自动在跑局结束时发送，payload 包含完整 `SerializableRun` JSON。它是后续分析的主数据源。

适合统计：

- MeiLin 跑局胜率。
- 死亡/结束楼层。
- 进阶分布、游戏模式、每日挑战、多人数。
- 最终卡组、遗物、事件和完整路线。
- 卡牌选择率与卡组留存率。

本地平衡摘要事件已移除。MeiLinMod 不再发送本地二次摘要，避免在 mod 端用字符串扫描 RunHistory 生成不稳定的平衡字段。

## 看板建议

PostHog 直连阶段可先看：

- `run_history.completed` 按事件属性 `properties.is_victory` 计算胜率；HogQL 中建议使用 `lower(toString(properties.is_victory)) IN ('true', '1')` 判断胜利；完整跑局位于 `properties.payload.applicant_payload.run_history`。
- `run_history.completed` 按 `run_floor_reached` 看死亡/结束楼层分布。
- `run_history.completed` 从 payload 中解析最终卡组和选牌记录，统计卡牌选择率、入组率和胜率。
- `run_history.completed` 按 `run_ascension`、`run_game_mode`、`run_player_count` 过滤。

如果要达到 STSVLogs/STSVWB 那种完整看板能力，后续需要 ingest 后端或导出脚本解析完整 RunHistory，把最终卡组、卡牌出现/选择/升级、遗物和路线等字段提升成可聚合列。PostHog 负责事件存储、查询和看板；结构化解析越复杂，越适合放在 ingest 后端或离线 ETL 中完成。

## 代理

当前按需求不使用代理，project API key 会被打进 mod 包。PostHog project API key 不是传统私钥，但别人可以拿它向项目写入脏数据。

长期公开收集时建议增加 Cloudflare Worker 或其他代理：

- 校验 applicant/event schema。
- 限制请求大小和事件数量。
- 过滤本地路径、日志和不需要的 payload 字段。
- 做来源校验和限流。
- 服务端持有 PostHog key，mod 只打到代理。
