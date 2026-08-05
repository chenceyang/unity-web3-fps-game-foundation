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
- 比赛核心：服务器状态机、比分记录、名次生成、固定 MatchResult 数据结构。
- 存证数据：确定性 JSON、Ethereum `keccak256`、`matchIdKey` 与 `resultHash`。
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

在 Package Manager 中导入 `Bootstrap Example` 可获得最小 Mock 示例脚本。该 Sample 不包含场景、
Prefab 或 UI，导入后不能直接作为完整游戏运行。

包内含 EditMode 测试。当前 Unity 6.3 验证工程使用 `com.unity.test-framework 1.6.0` 和
`com.unity.ext.nunit 2.0.5`；若项目未安装 Test Framework，测试源码会因找不到 NUnit 类型而无法编译。

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

## 对局结果与存证

专用服务器使用 `MatchCoordinator` 生成结果，然后：

```csharp
MatchResult result = coordinator.Finish(endedAtUnixSeconds);
MatchAttestationPayload payload = MatchResultHasher.CreatePayload(result);
await publisher.PublishAsync(payload, ct);
```

序列化器执行固定 schema 的确定性 JSON：对象字段按字典序写入，玩家按 `playerId` 排序，
奖励槽按 `slot + playerId` 排序，只使用整数。`Keccak256` 是 Ethereum Keccak padding，不能替换为 SHA3-256。

后端必须保存 `canonicalJson` 原文，并把 `resultHash` 排入 `MatchAttestation` 提交队列；发布后不得原地修改。

## 赛事安全边界

Unity 不签名、不托管钱包，也不直接调用 `TournamentEscrow`。`ITournamentGateway` 返回 `actionUrl`，
客户端用系统浏览器完成交易。游戏内展示 `Open / Settled / Cancelled`，金额始终使用十进制字符串表示 wei。

后端扩展契约见 `Documentation/game-backend-extension-openapi.yaml`。

## 资产失败策略

- RPC/后端失败：返回已有缓存或空库存，继续默认皮肤匹配。
- pending NFT：可展示，不可装备。
- 未拥有/已转出 NFT：本地拒绝；服务器仍会独立核验并回退默认皮肤。
- contentHash 不一致：不加载远端 bundle，返回 `asset_hash_mismatch`，调用方加载默认资源并上报告警。
- 奖励/赛事交易超时：保留状态并允许幂等重试，不影响下一局。

## 测试

在 Unity Test Runner 中运行 `EditMode` 测试。没有 Unity 编辑器时，可用仓库根目录的纯 C#
审计工程验证 Keccak、比赛结果与服务层；UnityEngine 组件仍需在编辑器中完成最终编译和场景测试。

截至 2026-08-06，运行时程序集和测试程序集已在 Unity 6000.3.21f1 编译；纯 C# 审计通过。
尚未记录一次完整 Unity Test Runner 运行结果，也未完成场景、联网或真实后端的端到端验收。

## 明确不包含

- 场景、Prefab、动画、美术和 UI；
- 具体联机 SDK 的传输层；
- 反作弊算法、账号后端和链交易服务；
- 武器/地图/模式数值平衡；
- NFT 拍卖、ERC-20、开箱、押注或战斗数值加成。

本包是“底层与契约”，不是可直接发布的完整游戏。
