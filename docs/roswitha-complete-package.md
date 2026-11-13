# UG / UX 四版本整合包（2026-10-30）

包含原皮 UG、皮肤 UG、原皮 UX、皮肤 UX，以及两套原始 WebP 开场。解压即包含全部交付内容，无需叠加以前的 ZIP。游戏侧仍需按下文接入，不能把 ZIP 直接当作已安装 Mod。

# 罗丝薇瑟 UG / UX v2：四版本、PMA 与透明收尾

本说明已更新至原始 WebP 开场版；替代共享开场和整段 OGV 的推荐接入方案。原始工程未部署到游戏。

## 四套主体资源

| 模式 | SRMD | 入口配方 |
|---|---|---|
| base_ug 原皮 UG | 30134.srmd / ug_buff | rossweisse_30134_ug_buff_eff |
| skin_ug 皮肤 UG | 30134_02.srmd / ug_buff | rossweisse_30134_ug_buff_eff_02 |
| base_ux 原皮 UX | 30134.srmd / ux_attack | rossweisse_30134_ux_attack_01 |
| skin_ux 皮肤 UX | 30134_02.srmd / ux_attack | rossweisse_30134_ux_attack_01_skin |

UG 分别为 26/27 个 Spine 层；两套 UX 各为 38 个 Spine 层（包含目标特效）。两个版本均另外读取各自 CFX 的粒子。UX 收尾复合配方 _02 是两个角色版本共享的源资源，不要把这个后缀自动理解为“皮肤版”。

UG 各 121 帧、4.033 秒；UX 各 356 帧、11.867 秒（原生时间轴 11.852 秒）；均为 1280×720、30 FPS。

## 此次修正

1. 使用梅铃 `spine_pma.gdshader`，源贴图保持预乘，导入 `premult_alpha=false`、`fix_alpha_border=false`。旧自定义 Shader 漏了槽位 alpha 对 RGB 的衰减，已经淡出的光仍残留 RGB，导致前景把女神遮住。修复不是统一压低所有特效亮度，源本身的白闪仍保留。
2. 已按用户指定的 17 秒参考视频第 10 秒定位。UX 第 10 秒对应背景复合中有 multiply 槽。预乘透明区不能以黑色直接参与乘法；新 multiply 材质使用 `rgb * tint.rgb * tint.a + (1-alpha)`，让透明区回到乘法单位色白色，恢复蓝色云层、星空和行星背景。
3. UG 开场用对应版本 30134_ug/30134_ug2 的背身姿态，从左侧拉近到舞台中心，再接源动画。当前拉近使用 0.2333 秒的 smoothstep 过渡，是按录像校准的实现，不是声称从 SRMD 完整解出了原游戏镜头。
3. 黑幕作为独立 ColorRect 调度。UG 0.1～0.2 秒进入，1.766～1.966 秒释放；UX 0.65～0.77 秒进入，8.2517～8.5017 秒释放。后续仍可播放金光，不能在演出 total 结束前一直留黑幕。
4. PNG 保存 RGBA，尾段 alpha 可为 0；独立预览把它们叠加在参考战场截图上，便于看清“特效还在，战场已经回来”。预览 MP4 中的战场是示意底图，不是实际游戏场景资产，也不能作为最终游戏覆盖视频使用。
5. 原皮 UX 使用原版 CFX 全套背景层；皮肤版使用皮肤 CFX，保留原生 Spine 的动画和场景背景层。PMA alpha 修复后背景/光效的淡入淡出按槽位工作。
6. UX 开场现已分别使用新增的原始动画 WebP，不再使用 OGV 或参考录像提取帧：
   - 原皮：8f82e4fc26e7323de2ced17adf06c8ba.webp（黑色服装）。
   - 皮肤：704681867cdb2f971503baa7ccf79b32.webp（白金服装）。
   - 两者均 1580×720、46 帧；帧时长有 33/67 ms，总计 2742 ms。按每帧 duration 累加选帧，不能假设固定帧率。
   - Pillow 在帧像素实际解码后才更新 duration；读取时先 seek，再 convert/load，再取 info，避免误判成 0 毫秒。
   - WebP 解码帧按普通 RGBA Sprite2D 播放，不把 Spine 的预乘 Shader 批量套上去；等比 cover 到 1280×720，裁切两侧，不拉伸比例。
   - 开场从 0.7667 秒开始，3.5087 秒结束，首段特效 3.5187 秒开始，末段爆发 8.2517 秒开始；黑幕在 8.2517～8.5017 秒退出。

两个 UX 开场均已独立接入，不再存在“皮肤 UX 使用原皮开场”的资源限制。开场之外的粒子和目标模型限制见末尾。

## 文件组织

```text
preview.html + 四个 MP4       带示意战场的四版本审查
RGBA/base_ug/ ... skin_ux/    透明 PNG 序列，不能当作 RGB 帧保存
NativeProject/               可独立 Godot 导入的原生舞台工程
  RoswithaV2/
    presentation.tscn/.gd    游戏视觉入口，Node2D + CanvasLayer
    config.json              四套命令、层和时间轴
    assets/                  源 PNG、atlas、skel、tres 和导入配置
    cutin/                   原始 WebP 解码帧，base/ 和 skin/ 独立
    ArtWorks/                当前角色替身所需资源
    *.wav / particles.gd / spine_pma.gdshader
tools/                       烘焙、打包、高清恢复脚本及映射
```

本整合包已经包含原始素材 sources/ 和最新 OriginalWebP/，无需另外下载旧包或叠加更新包。包内不含游戏 DLL，也不自动安装修改游戏。

## 接入游戏应使用原生舞台

把 NativeProject/RoswithaV2 整个目录复制到目标 Godot 项目根目录，确保 Spine 扩展兼容 Godot 4.5.1，执行两遍导入。舞台路径是：

```text
res://RoswithaV2/presentation.tscn
```

实例化后 `begin("base_ug")` / `begin("skin_ug")` / `begin("base_ux")` / `begin("skin_ux")`，返回 bool；`elapsed`、`total` 提供同一个实时时钟，`running` 在资源构建完成后变 true。调用方在退出、取消、新会话替换时执行 `finish()`。不要把它交给只检查 Spine 动画完成的 PlayOneShotFront 回收机制。

舞台在自己的 CanvasLayer 下运行，只在中段绘制黑幕；无需修改游戏 viewport 的透明设置。音效从舞台开始时播放一次，需由宿主接入游戏音量/静音设置，并抑制同次出牌的旧音效路径。

RuntimeAdapter.cs 保留原来“宿主资格判断 + beat 回调”的模板形式，但场景路径已改为 RoswithaV2。它尚未在目标 Mod 编译。仍需把 eligibility 绑定角色/卡牌归属、开关，把 combatStillValid 绑定同一战斗房间和施法者生命周期，使用独占 session/取消令牌防止重入。

原皮/皮肤选择必须来自当前皮肤 ID，而不是根据牌名猜测。现有皮肤补丁 UG 的 COSMIC_INDIFFERENCE、UX 的 CRUSH_UNDER 仍是接线参考，不能仅因为做了四套资源就对所有角色全局触发。

## 玩法时序保持原生命令

UG 源命中 3.2 秒；UX 开场额外暂停 2.742 秒，后续 DAMAGE 的视频时刻仍为：

```text
3.7187, 3.8187, 3.9187, 4.0187,
6.8517, 6.9517, 7.0517, 7.1517, 7.2517, 7.3517,
8.2517, 8.3517, 8.4517
```

它们不是 STS2 伤害数值。单次攻击只执行一次原命令，多段牌只消费它原本已有的段数。源 T0 HIT 是入口，不能自动结算伤害。视频播放完成也不是伤害时钟。

资源缺失、开关关闭走无演出原效果；战斗结束中断必须清理。当前舞台使用本地角色替身，不借用真实目标；TARGET 仍是固定预览挂点。要接真实敌人，继续按 _node/rossweisse_30134_ux_attack_01_target_node.json 驱动，并保存/恢复 parent/index/transform/visible/modulate/z。不得连生命条、意图一起搬走。

## 透明素材的限制

PNG 可以保存透明，但单张普通 RGBA 无法完全表达原生 additive/multiply 在任意战场背景上的混合。帧截图合成可能与原生舞台有细微差异；原生舞台的 Spine/粒子混合优先作为游戏接入方案。不要把这里的演出重新编码成无 alpha 的 OGV 后再铺满战场，否则收尾黑幕会重新出现。

所有 RGBA 帧全量预载显存开销很大。原生舞台应在战斗前预热，按当前皮肤加载，后续再评估 Spine 更新和纹理驻留；不要在一次出牌内加载全部四套。当前每次舞台仍重建节点，生产接入前应增加缓存/预热与性能评估。

## 验证记录和未完成项

- 四套主体已分别渲染，PMA 修复前后已目验，女神和前景层不再出现旧版的残留 RGB 遮挡。
- 已检查 RGBA 尾帧包含透明区域。
- NativeProject/smoke.gd 验证了四套加载、时钟推进、模式资源对应、尾段 blackout.alpha=0，以及 finish 后节点释放。
- 新增两套原始 WebP 已按衣服核对、分别解码，并验证运行时加载了各自路径、46 帧和 2.742 秒总时长。
- 粒子依旧是源参数离线近似；UG 拉近镜头未经逐帧数值拟合；TARGET 未接真实敌人。
- 尚未进行目标 Mod C# 编译、实际卡牌触发、模型恢复、多人与 PCK 部署验证。

当前应称为“四套独立演出 + 透明收尾的修正版”，不能宣称所有镜头均已与原游戏逐帧一致。

## 从本整合包重建

v2 包自带可直接加载的 NativeProject 和透明帧；原始素材也已放在本包 sources/ 中。

```powershell
$env:ROSWITHA_SOURCE = Join-Path (Get-Location) 'sources/ux与ug'
python -m pip install -r tools/requirements.txt
python tools/build_roswitha_v2.py
# 使用 Godot 4.5.1 Mono 对 BakeProject 执行两遍 --editor --import --quit
# 随后分别运行（离线工具不能挂到战斗场景）：
# Godot --path BakeProject --script res://stage.gd -- base_ug
# Godot --path BakeProject --script res://stage.gd -- skin_ug
# Godot --path BakeProject --script res://stage.gd -- base_ux
# Godot --path BakeProject --script res://stage.gd -- skin_ux
$env:ROSWITHA_FFMPEG = 'ffmpeg.exe 的完整路径'
python tools/package_roswitha_v2.py
```

开场原文件在 OriginalWebP/；生成器优先读取 ROSWITHA_SOURCE/wbep/，找不到则使用包内 OriginalWebP/。References/reference_ux.mp4 仅作对照，不再参与生成。原始图片保留在本包 sources/；NativeProject 已包含当前演出所需的 85 套 Spine 资产，无需其他包即可运行烟雾测试。


## 整合包校验

SHA256SUMS.json 重新覆盖所有文件。来源按原始素材包、v2、WebP 更新包的顺序合并，后者覆盖前者。浏览器打开 Preview/preview.html；运行时使用 NativeProject；透明帧在 RGBA/。
