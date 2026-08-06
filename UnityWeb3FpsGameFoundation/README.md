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
- 可玩本地 Prototype：一键生成玩家/机器人 Prefab、测试竞技场、死亡复活、比分、倒计时、胜负和 HUD。
- Web3 大厅：一键生成 Mock 场景，展示钱包、confirmed NFT loadout、奖励和赛事操作及安全降级状态。
- ASH//LEDGER 正式垂直切片：一键生成 UI Toolkit 大厅、RIFT RELAY 地图、带弹药/装填/命中反馈的战斗 HUD 与三把可编辑武器灰盒 Prefab。
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
3. 按 Play，在大厅点击 `DEPLOY TO RIFT RELAY` 进入正式风格的本地对局。

生成内容包括 UI Toolkit 正式大厅、Mock Web3 状态绑定、战斗 HUD、RIFT RELAY 三路灰盒地图，以及
KESTREL-7、PULSE-9、RELAY-3 三把武器 Prefab。v1.4.1 进一步加入断裂轨道背景、工业塔群、极光中继柱、
发光导视、雾效与局部灯光；v1.4.2 加入独立的精细低多边形敌方角色和第一人称双臂/持枪模型，不再依赖旧版
Prototype Prefab；v1.4.3 修复 Unity 6 生成场景丢失 PanelSettings 导致 UI 不显示的问题。所有生成资产都位于项目
`Assets` 下，可继续编辑。v1.4.4 会根据项目当前的 Built-in / URP / HDRP 渲染管线选择材质 Shader，并禁止在
Play Mode 中运行生成器。
v1.4.5 进一步修正 Unity 6 在 `UIDocument.OnEnable` 时清空 PanelSettings 的顺序问题，并在生成阶段强制校验引用。
v1.4.6 不再依赖 UIDocument 自身保存该引用：正式 View 会保存备用 PanelSettings，并在运行时自动恢复 UI。
v1.5 增加 30/120 弹药、R 键定时装填、实时装填进度、空仓提示和命中标记；玩家及机器人射击会生成短暂视觉弹道与枪口闪光，
但命中与伤害仍只采用 `IShotCommandSink` 返回的权威 hitscan 结果。HUD 使用原创的低干扰生存 FPS 信息层级，不复制第三方游戏资源。
大厅角色预览复用完整分层装甲 Prefab，第一人称手臂、手套与枪械姿态也增加了可编辑细节。
角色与建筑仍是制作级模块资产，并非概念图中的最终高精度模型；背景视觉已作为实际场景资源接入。

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

生产环境使用 `HttpEntitlementGateway` 调用 `/internal/v1/entitlement-check`。service token 必须由专用服务器
进程的内存委托提供，不能写入 Unity 场景、Prefab、ScriptableObject、日志或版本控制。客户端构建不得实例化该适配器。
具体联网 SDK 仍需负责把客户端意图送到服务器，并把最终 resolved skin 快照广播给所有客户端。

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
奖励槽按 `slot + playerId` 排序，只使用整数。`Keccak256` 是 Ethereum Keccak padding，不能替换为 SHA3-256。

后端必须保存 `canonicalJson` 原文，并把 `resultHash` 排入 `MatchAttestation` 提交队列；发布后不得原地修改。

## 赛事安全边界

Unity 不签名、不托管钱包，也不直接调用 `TournamentEscrow`。`ITournamentGateway` 返回 `actionUrl`，
客户端用系统浏览器完成交易。游戏内展示 `Open / Settled / Cancelled`，金额始终使用十进制字符串表示 wei。

后端扩展契约见 `Documentation/game-backend-extension-openapi.yaml`。
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
v1.4 新增 ASH//LEDGER 正式垂直切片生成器、内容目录和 UI Toolkit 表现，仍需在 Unity 中生成后记录
完整 Test Runner 与 Play Mode 实机结果。仓库的静态审计脚本已使用 Unity 6000.3.21f1 自带 Roslyn
编译 Runtime、Editor 和 Tests 三个程序集，并执行 39 个测试全部通过；
该结果不替代 Unity Editor/Test Runner。
联网和真实后端的端到端验收尚未完成。

## 明确不包含

- 最终生产地图、角色动画、高精度美术和完整前端页面（v1.4 已包含首个正式风格灰盒垂直切片）；
- 具体联机 SDK 的传输层；
- 反作弊算法、账号后端、真实 entitlement 服务、持久化存证队列和链交易服务；
- 武器/地图/模式数值平衡；
- NFT 拍卖、ERC-20、开箱、押注或战斗数值加成。

本包是“底层与契约”，不是可直接发布的完整游戏。
