---
name: "sts2-migrate-110-to-111"
description: "Migrates STS2 RitsuLib-based mods from game version 0.110 to 0.111 (csproj, manifest json, STS2_111 conditional compilation, API changes, test projects). Invoke when user asks to update/migrate any STS2 mod (e.g. yukimod) to 0.111."
---

# STS2 Mod 迁移：0.110 → 0.111

将基于 RitsuLib 的 Slay the Spire 2 mod 从游戏版本 0.110 升级到 0.111 的完整迁移流程。
已在 MeiLinMod (v0.4.3 → v0.4.4) 上验证通过。

## 触发场景

- 用户要求将某个 STS2 mod 更新/迁移/适配到 0.111 版本
- 用户提到 `STS2_111`、`Sts2TargetVersion=111`、游戏 0.111 更新等
- 目标 mod 此前面向 0.110（或更早但含 110 分支）构建

## 前置准备

1. **先获取官方迁移指南**（版本对号入座，不要凭记忆）：
   - https://tutorials.sts2modding.com/docs/07-migration-99-100/ 中的 "0.110 → 0.111" 小节
   - 用 WebFetch 拉取最新内容，逐条核对 API 变更表
2. **确认本地环境**：
   - Steam 已安装 0.111 版 Slay the Spire 2
   - NuGet 包源可拉到 `STS2.RitsuLib >= 0.5.11`（0.5.11 是首个带 0.111 运行时变体的版本）
   - RitsuLib 运行时 mod（mods 目录下的 STS2-RitsuLib）需同步更新到 0.5.11

## 迁移步骤

### 第 1 步：更新主 `.csproj`

以下四处全部要加 111（保留旧版本分支，不要删除）：

```xml
<!-- 1) 默认目标版本 -->
<Sts2TargetVersion Condition="'$(Sts2TargetVersion)' == ''">111</Sts2TargetVersion>

<!-- 2) RitsuLib 版本：0.5.11 提供 0.111 运行时变体与编译期 API -->
<RitsuLibVersion Condition="'$(RitsuLibVersion)' == ''">0.5.11</RitsuLibVersion>

<!-- 3) 111 路径定义（加在 Sts2Path110 定义之后，按现有 110 的写法复制） -->
<Sts2Path111 Condition="'$(Sts2Path111)' == '' and '$(STS2_111_PATH)' != ''">$(STS2_111_PATH)</Sts2Path111>
<Sts2Path111 Condition="'$(Sts2Path111)' == '' and Exists('$(SteamLibraryPath)/common/Slay the Spire 2')">$(SteamLibraryPath)/common/Slay the Spire 2</Sts2Path111>
<Sts2Path Condition="'$(Sts2TargetVersion)' == '111'">$(Sts2Path111)</Sts2Path>

<!-- 4) 条件编译符号 -->
<DefineConstants Condition="'$(Sts2TargetVersion)' == '111'">$(DefineConstants);STS2_111</DefineConstants>
```

同时更新版本校验 Target（通常名为 `CheckDependencyPaths`）中的两处错误检查：

- "Unsupported Sts2TargetVersion" 的 Condition 追加 `and '$(Sts2TargetVersion)' != '111'`
- 新增一行 `Sts2Path111 is not configured` 的 Error（照抄 110 那行改数字）

### 第 2 步：更新 mod 清单 `<ModName>.json`

```json
{
  "version": "<递增一位补丁版本，如 v0.4.3 → v0.4.4>",
  "dependencies": [
    { "id": "STS2-RitsuLib", "min_version": "0.5.11" }
  ],
  "min_game_version": "v0.111.0"
}
```

### 第 3 步：修复条件编译（最常见的编译错误来源）

构建后如果报 **CS0115 "没有适合的类型来重写该方法"**，几乎都是 `#if` 分支漏了 `STS2_111`。
先全局搜索再逐处补上：

```
Grep pattern: STS2_110  (glob: **/*.cs)
```

把 `#if STS2_109 || STS2_110` 改为 `#if STS2_109 || STS2_110 || STS2_111`。

### 第 4 步：核对 0.110 → 0.111 API 变更（以官方指南为准）

MeiLinMod 迁移时涉及/核对过的变更：

| API 变更 | 处理方式 |
|---|---|
| `CharacterModel.GenerateAnimator` 新增 `Creature` 参数 | 走 RitsuLib `SetupCustomCreatureAnimator` 的 mod 无需改动；直接 override 的需补参数 |
| `CardCmd.Exhaust` 返回类型改为 `Task<CardPileAddResult?>` | 仅在需要结果值时改写 await 用法；单纯 await 无需改动 |
| 卡牌打出结果位置 API（0.109 引入，111 沿用）`GetResultLocationForCardPlay()` / `ModifyCardPlayResultLocation()` | 通过 `#if STS2_109 \|\| STS2_110 \|\| STS2_111` 走新分支，`#else` 保留旧的元组版本 |

在目标 mod 中 Grep 这些符号名（`GenerateAnimator`、`Exhaust`、`ResultPileTypeAndPosition`、`ResultLocation`），确认是否有直接调用。

### 第 5 步：更新测试项目（如果存在 `tests/`）

1. 测试 `.csproj`：
   - `Sts2TestMinGameVersion` → `v0.111.0`
   - `PackageReference Include="STS2.RitsuLib"` Version → `0.5.11`
2. 测试用清单 json（`mod/*.json`、`mod-deps/*/*.json`）中所有 `min_game_version` → `v0.111.0`
3. 迁移相关测试（如 `MigrationDependencyTests`）中的断言：
   - 主清单的 version / min_game_version / RitsuLib min_version 期望值
   - csproj 断言：`Sts2TargetVersion` 默认值 111、包含 `Sts2Path111`、包含 `STS2_111`（可同时保留对 `Sts2Path110`/`STS2_110` 的断言，验证多版本支持仍在）

### 第 6 步：构建验证

```powershell
dotnet build <ModName>.csproj -p:Sts2TargetVersion=111 -t:Build --no-incremental -v:minimal
```

期望结果：`已成功生成。0 个警告 0 个错误`，且 Godot savepack 打包完成（`[ DONE ] savepack`）。

**排障**：
- CS0115 → 回到第 3 步补条件编译
- 报 GodotPath 未配置 → 这是打包环节问题，不影响代码正确性；可先用 `-t:Compile` 单独验证 C# 编译，再配好 GodotPath 打完整包
- RitsuLib 还原失败 → 检查 csproj 的 `RestoreSources` 是否包含本地包源目录

### 第 7 步：收尾清单

- [ ] 主 csproj 四处 111 配置 + 校验 Target 更新
- [ ] mod json：version 递增、min_game_version=v0.111.0、RitsuLib min_version=0.5.11
- [ ] 所有 `STS2_110` 条件编译处已加 `|| STS2_111`
- [ ] 测试项目 csproj / 清单 / 断言同步更新
- [ ] 全量构建通过（0 警告 0 错误）
- [ ] 提示用户：游戏内 mods 目录的 STS2-RitsuLib 也要更新到 0.5.11，然后进游戏冒烟测试
