# Web3 FPS Game Foundation for Unity

Unity 游戏侧底层包。实时 FPS 与 Web3 资产层严格分离：战斗只依赖 Unity 和权威游戏服务器；
NFT、钱包、奖励、赛事和结果存证通过游戏后端异步接入。

包清单声明最低目标版本为 Unity 2022.3 LTS；当前实际验证编辑器为 Unity 6000.3.21f1，
Unity 2022.3 的最终编辑器编译仍待验证。包本身没有 Nethereum、UniTask 或具体联网 SDK 依赖。

## 已包含

- `Game.Web3.IGameAssetGateway`：兼容 `timothyshen/web3-fps-assets` 的资产、钱包、奖励和 loadout 契约。
- Mock 与 HTTP 网关：Unity 可以在后端和测试网完成前独立开发。
- 大厅服务：安全刷新、默认皮肤降级、confirmed 过滤、loadout 本地校验与提交。
- 钱包/奖励协调器：系统浏览器跳转、可取消轮询、终态与超时处理。
- 赛事网关：列表、详情、报名、赞助、领奖、退款和交易确认。
- FPS 核心：输入帧、CharacterController 移动、第一人称视角、命中指令、权威射线结算、生命值。
- 武器数值目录：`WeaponDefinition` 承载六把武器的服务器认可数值，可写入 `HitscanWeapon`；NFT 内容不得覆盖。
- 可玩本地 Prototype：一键生成玩家/机器人 Prefab、测试竞技场、死亡复活、比分、倒计时、胜负和 HUD。
- Web3 大厅：一键生成 Mock 场景，展示钱包、confirmed NFT loadout、奖励和赛事操作及安全降级状态。
- ASH//LEDGER 正式垂直切片：一键生成 UI Toolkit 大厅、RIFT RELAY 地图、带弹药/装填/命中反馈的战斗 HUD 与六把可编辑武器灰盒 Prefab。
- 正式页面：登录/游客覆盖层（demo `POST /v1/auth/login`，token 仅内存）、匹配确认页（冻结 loadout 与降级警示）、资产详情页（serial/season/wear/tokenId/contentHash、系统浏览器市场跳转）与独立赛后页（发布与存证状态，不遮挡比分）。
- 本地权威驱动：`LocalAuthoritativeMatchDriver` 在切片中扮演专用服务器 —— 开局前冻结 entitlement 快照、战斗零网关调用、赛后发布一次并展示 published/duplicate/conflict/failed。
- NFT 外观：`FormalSkinCatalog`（覆盖后端与合约种子 skinDefId）+ `FormalSkinApplicator`（仅 MaterialPropertyBlock 视觉改色）+ `FormalSkinResolver`（可选 hash 校验 Bundle 路径，失败降级默认外观）。
- 比赛核心：服务器状态机、比分记录、名次生成、固定 MatchResult 数据结构。
- 服务器边界：开局前 entitlement-check、不可变 loadout 快照、失败默认皮肤和权威对局会话。
- 存证数据：确定性 JSON、Ethereum `keccak256`、`matchIdKey` 与 `resultHash`。
- 结果发布：成功一次后去重、失败可重试、同 matchId 冲突结果拒绝。
- 资产完整性：下载 AssetBundle 后验证链上 `contentHash`，失败返回可降级结果。
- EditMode 测试：Keccak 官方向量、结果确定性、比赛状态、资产降级和赛事状态。

## 安装

选择一种方式：

1. 把整个目录复制到 Unity 项目的 `Packages/com.web3fps.game-foundation/`；或
2. 在 `Packages/manifest.json` 使用本地路径：

```json
{
  "dependencies": {
    "com.web3fps.game-foundation": "file:../UnityWeb3FpsGameFoundation"
  }
}
```

在 Package Manager 中可导入 `Bootstrap Example` 获得最小 Mock 示例，也可导入
`Playable Local Prototype` 获取本地对局生成说明。

包内含 EditMode 测试。当前 Unity 6.3 验证工程使用 `com.unity.test-framework 1.6.0` 和
`com.unity.ext.nunit 2.0.5`；若项目未安装 Test Framework，测试源码会因找不到 NUnit 类型而无法编译。

包已声明所用内置模块依赖（Physics、UIElements、UnityWebRequest 等），并为全部资产提供确定性
GUID 的 `.meta` 文件（由 `tools/generate_unity_metas.py` 生成）；新增文件后重跑该脚本即可补齐。

## 可玩本地 Prototype

1. 在 Unity 菜单执行 `Tools > Web3 FPS > Create Local Prototype Scene`。
2. 生成器会创建 `Assets/Web3FpsPrototype/Prototype.unity`、玩家/机器人 Prefab 和基础材质。
3. 打开生成场景并按 Play。

操作：WASD 移动、Shift 冲刺、Space 跳跃、鼠标瞄准、左键射击、Escape 释放/锁定鼠标；
结算后按 R 重开。规则为三分钟内率先击杀 5 次获胜，包含两秒复活、倒计时、平局和 MatchResult 生成。

该场景只验证离线游戏循环，使用 `LocalAuthoritativeShotSink`。生产联网版本必须替换为权威服务器适配器；
本地命中、比分和结果不能直接作为链上或奖励事实。

## ASH//LEDGER 正式垂直切片

1. 在 Unity 菜单执行 `Tools > Web3 FPS > Create ASH LEDGER Vertical Slice`。
2. 生成器会创建 `Assets/AshLedgerVerticalSlice/`，并自动打开 `Scenes/AshLedgerLobby.unity`。
3. 按 Play：先经登录/游客覆盖层进入大厅，点击 `DEPLOY TO RIFT RELAY` 打开匹配确认页，
   CONFIRM 会在加载战斗场景前完成 entitlement 冻结；赛后页提供 REMATCH 与返回大厅。

当前生成内容包括 UI Toolkit 正式大厅（登录、匹配确认、资产详情覆盖层）、战斗 HUD 与独立赛后页、
Mock Web3 状态绑定、RIFT RELAY 三路灰盒地图、全部六把武器 Prefab（KESTREL-7/PULSE-9/WITNESS/
BREACH-12/ANCHOR/RELAY-3，战斗内切换尚未实现）、稳定的模块化低多边形角色、程序化动作和第一人称手臂。
装备 confirmed 皮肤会即时改变大厅陈列武器与战斗第一人称武器的颜色（仅视觉，不改任何战斗数值）。
本地战斗已接通 30/120 弹药、装填、弹匣/枪械/相机后坐力、原创合成音效、视觉弹道、表面弹着、
头/躯干/腿分区伤害、出生保护、动态散布，以及带视线/反应/记忆/距离控制/侧移的 Bot。
生成器会选择当前 Built-in/URP/HDRP 对应 Shader，并持久化 UI Toolkit 引用。Quaternius CC0 FBX 仅作为可替换参考资产保留，不再由生成场景实例化。
RIFT RELAY 保留 v1.7.4 的四向宇宙轨道战斗背景。若 Unity 尚未导入包内角色表面或轨道背景 PNG，生成器会先强制同步导入一次；确实缺失时才记录警告并使用可读颜色材质继续生成，贴图恢复后重新执行生成器即可获得完整视觉。
所有生成资产位于项目 `Assets/AshLedgerVerticalSlice/`，可以继续编辑；完整版本历史见 `CHANGELOG.md`。
整体参考低多边形生存 FPS 的清晰轮廓与简洁反馈，但不复制 Unturned 的模型、动画、纹理或音频。
角色和建筑仍是可继续替换的低多边形制作资产，并非写实 AAA 高精度模型；背景视觉已作为实际场景资源接入。

## Web3 大厅

1. 在 Unity 菜单执行 `Tools > Web3 FPS > Create Web3 Lobby Scene`。
2. 打开 `Assets/Web3FpsLobby/Lobby.unity` 并按 Play。
3. Mock 模式会展示两件 confirmed NFT、一个待领奖励和一个开放赛事；钱包/交易 URL 只记录、不打开浏览器。

`Web3LobbySession` 是不依赖 MonoBehaviour 的大厅应用控制器，统一管理刷新、装备、钱包绑定、领奖、
赛事报名/赞助/领奖/退款及错误状态。`Web3LobbyController` 和 `Web3LobbyHud` 只是 Unity 包装层。
这些方法只允许在大厅或菜单由用户操作触发；不得从战斗 `Update`、网络 tick 或射击链路调用。

接真实后端时关闭 `GameFoundationBootstrap.Use Mock Backend`，填写 HTTPS 游戏后端 URL，并在登录后仅用
`SetAccessToken` 写入短期 token。场景、Prefab、日志和版本控制中不得包含 token 或任何链上密钥。

## 最小启动

1. 在首个常驻场景创建空对象并挂 `GameFoundationBootstrap`。
2. 开发期保持 `Use Mock Backend`；联调期关闭并填写游戏后端 URL。
3. 登录成功后调用 `bootstrap.SetAccessToken(session.AccessToken)`；不要把 token 序列化到场景。
4. 大厅加载时：

```csharp
var refresh = await bootstrap.Context.Assets.RefreshLobbyAsync(ct);
if (refresh.UsedFallback)
{
    // 资产服务不可用，继续允许默认皮肤和普通匹配。
}
```

5. 选择 NFT 时仅允许 `SkinItem.IsConfirmed == true`，并通过 `LoadoutService.TryEquip` 保存意图。
6. 开局前由服务器调用 `/internal/v1/entitlement-check` 冻结资产快照；客户端无权调用。

## 联机接入

包不锁定 NGO、Mirror、FishNet 或 Photon。为选定的联网 SDK 实现：

- `IGameNetworkAdapter`：发送输入帧/射击指令并广播最终结果；
- `IAuthorityProvider`：让 `Health` 只接受权威实例伤害；
- `IShotCommandSink`：把 `ShotCommand` 发到服务器，并在服务器执行射线与伤害；
- `IMatchResultPublisher`：由专用服务器把最终结果发送到游戏后端。

`LocalAuthoritativeShotSink` 只用于离线、Host 或专用服务器场景。生产客户端不能把本地射线结果当作事实。

## 专用服务器开局边界

`AuthoritativeMatchSession` 接收客户端的 loadout 意图，但不会信任它。调用 `StartAsync` 时，服务器先通过
`LoadoutSnapshotResolver` 核验每名玩家并冻结 `PlayerLoadoutSnapshot`，随后才启动 `MatchCoordinator`。
后端不可用、NFT 已转出或未确认时只把对应外观降级成默认皮肤，不拒绝玩家进入普通对局。

生产环境使用 `HttpEntitlementGateway` 调用 `/internal/v1/entitlement-check`（请求为
`{playerId, matchId, wallet, tokenIds}`，响应按后端 `EntitlementResult` 映射回快照）。service token 必须由专用服务器
进程的内存委托提供，不能写入 Unity 场景、Prefab、ScriptableObject、日志或版本控制。客户端构建不得实例化该适配器。
具体联网 SDK 仍需负责把客户端意图送到服务器，并把最终 resolved skin 快照广播给所有客户端。

本地垂直切片由 `LocalAuthoritativeMatchDriver` + `LocalMatchHandoff` 扮演这一服务器角色：确认页在加载战斗场景前
冻结快照（Mock 网关默认，配置 live 后走 Http 适配器），战斗期间零 Web3 调用，赛后经 `MatchPublishCoordinator`
发布一次。联网阶段用真实专用服务器进程替换驱动与静态交接，并改由 `AuthoritativeMatchSession` 同时承载逐击杀计分。

## 对局结果与存证

专用服务器使用 `MatchCoordinator` 生成结果，然后：

```csharp
MatchResult result = coordinator.Finish(endedAtUnixSeconds);
MatchAttestationPayload payload = MatchResultHasher.CreatePayload(result);
await publisher.PublishAsync(payload, ct);
```

推荐由 `MatchPublishCoordinator.PublishOnceAsync` 包装 publisher：首次失败会保留可重试状态，成功后相同结果
不会重复发送，而同一 matchId 的不同 `resultHash` 会被拒绝。该内存协调器不是持久化队列；真实服务器仍需由
后端提供 durable queue、幂等键和监控告警。

序列化器执行固定 schema 的确定性 JSON：对象字段按字典序写入，玩家按 `playerId` 排序，
奖励槽按 `slot + playerId` 排序（`slot` 是 uint8 整数），只使用整数。`Keccak256` 是 Ethereum Keccak padding，
不能替换为 SHA3-256。

`HttpMatchResultPublisher` 直接把规范化字节 POST 到 `/internal/v1/matches` —— 后端对收到的原文自行
canonicalize 并派生 `resultHash`/`matchIdKey`，重复推送返回既有记录，内容不同的同 matchId 返回 409。
本地驱动还会为名次第一的玩家派生一个确定性 `rewardSlots` 条目，后端据 rewardId 决定铸造哪款皮肤。

## 赛事安全边界

Unity 不签名、不托管钱包，也不直接调用 `TournamentEscrow`。`ITournamentGateway.CreateIntentAsync`
返回 `actionUrl`，客户端用系统浏览器完成交易，游戏内不轮询交易状态——链上结果通过后端刷新回读。
游戏内展示 `open / settled / cancelled` 与取消原因，金额一律使用 `Amount`（wei 十进制字符串 + 格式化值）。

后端契约的唯一来源是 web3-fps-assets 仓库的 `api/openapi.yaml`；本包 `Game.Web3` 与 `Tournaments`
目录是其 unity-sdk 的逐字镜像，不要在包内单独修改契约文件。
正式 UI、美术、地图、武器和 Web3 创意方向见 `Documentation/FORMAL_CONTENT_DESIGN.md`。

## 资产失败策略

- RPC/后端失败：返回已有缓存或空库存，继续默认皮肤匹配。
- pending NFT：可展示，不可装备。
- 未拥有/已转出 NFT：本地拒绝；服务器仍会独立核验并回退默认皮肤。
- contentHash 不一致：不加载远端 bundle，返回 `asset_hash_mismatch`，调用方加载默认资源并上报告警。
- 奖励/赛事交易超时：保留状态并允许幂等重试，不影响下一局。

## 测试

在 Unity Test Runner 中运行 `EditMode` 测试。没有 Unity 编辑器时，可用仓库根目录的纯 C#
审计工程验证 Keccak、比赛结果与服务层；UnityEngine 组件仍需在编辑器中完成最终编译和场景测试。

截至 2026-08-06，v1.0 运行时程序集和测试程序集已在 Unity 6000.3.21f1 编译；纯 C# 审计通过。
v1.7.0 的静态审计（Unity 6000.3.21f1 自带 Roslyn 编译三个程序集 + 静态执行器）通过了当时的 53 个测试；
该结果不替代 Unity Editor/Test Runner。v1.9.1 已把 v1.7.4 的角色稳定性方案合入 v1.9 功能线，并新增
程序化角色动作测试；Unity 6000.3.21f1 引用编译和静态执行器已通过 117/117 项测试。隔离 Unity Test Runner
因本机缺少 `com.unity.editor.headless` 许可无法启动，仍须在已授权的交互式 Unity 中运行全部 EditMode 用例、
重建两张场景并完成一整局 Play Mode 验收。联网和真实后端的端到端验收尚未完成。

## 明确不包含

- 最终生产地图、定制角色拓扑/动作捕捉和高精度美术（v1.9 的登录/确认/详情/赛后页与六把武器均为可玩灰盒，不是成品视觉）；
- 具体联机 SDK 的传输层；
- 反作弊算法、账号后端、真实 entitlement 服务、持久化存证队列和链交易服务；
- 武器/地图/模式数值平衡；
- NFT 拍卖、ERC-20、开箱、押注或战斗数值加成。

本包是“底层与契约”，不是可直接发布的完整游戏。
