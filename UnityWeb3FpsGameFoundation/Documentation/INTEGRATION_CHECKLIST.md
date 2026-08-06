# Unity 集成检查单

## 本地 Prototype v1.1

- [x] 提供 Unity 菜单生成可编辑的 Prototype 场景。
- [x] 生成玩家与机器人 Prefab、竞技场、出生点和基础材质。
- [x] 接通移动、视角、射击、伤害、死亡和两秒复活。
- [x] 接通目标击杀数、倒计时、胜负/平局、HUD 和 R 重开。
- [x] 对局结束生成本地 `MatchResult`，但不自动发布或发奖。
- [x] 使用 Unity 6000.3.21f1 引用程序集编译 Runtime、Editor、Tests，并静态执行 20 个测试全部通过。
- [ ] 在 Unity 6000.3.21f1 重新编译 v1.1，运行 EditMode 测试并手工完成一局。
- [ ] 确认生成场景在目标渲染管线和目标输入配置下正常运行。

## Web3 大厅 v1.2

- [x] 提供 Unity 菜单生成独立 Web3 Lobby 场景。
- [x] `Web3LobbySession` 统一封装资产、loadout、钱包、奖励和赛事操作。
- [x] Mock 模式不打开真实外部页面，只记录钱包/交易 action URL。
- [x] UI 只允许 confirmed NFT 装备，并始终保留默认皮肤入口。
- [x] 资产或赛事异常转为可见状态，不禁用普通游戏。
- [x] 使用 Unity 6000.3.21f1 引用程序集编译并静态执行 24 个测试全部通过。
- [ ] 在 Unity 中生成 Lobby 场景并手工完成 Mock 钱包、领奖和赛事报名流程。
- [ ] 接入真实登录 session，只在内存中调用 `SetAccessToken`。

## 大厅

- [ ] 创建并持久化 `GameFoundationBootstrap`。
- [ ] 登录后仅在内存中设置 access token。
- [ ] 进入大厅调用 `RefreshLobbyAsync`，捕获取消异常。
- [ ] `UsedFallback` 时显示轻提示，不禁用普通匹配。
- [ ] 衣柜只把 confirmed 资产加入正式 loadout。
- [ ] tokenId 全链路保持字符串。

## 开局

- [ ] 客户端提交 loadout 意图。
- [ ] 游戏服务器调用 entitlement-check，不信任客户端。
- [ ] 服务器生成 snapshotId，并向所有客户端下发最终 resolved skin。
- [ ] 核验失败时改用默认皮肤，不拒绝玩家进入普通对局。
- [ ] 对局开始后停止全部资产、奖励和赛事轮询。

## 战斗

- [ ] 网络适配器发送 `PlayerInputFrame` 和 `ShotCommand`。
- [ ] 只有权威服务器执行命中、伤害、死亡与比分。
- [ ] `Health.authorityProvider` 指向联网 SDK 的权威实现。
- [ ] NFT 皮肤不改变伤害、射速、碰撞体或后坐力。

## 结算

- [ ] 只有服务器调用 `MatchCoordinator.Finish`。
- [ ] 同一 matchId 只发布一次。
- [ ] 后端保存 canonicalJson 并验证 resultHash。
- [ ] 存证失败进入异步重试，不阻断赛后页或下一局。
- [ ] 高价值奖励在反作弊状态 passed 前不得铸造。

## 安全

- [ ] Unity 工程和日志中不存在私钥、助记词、RPC key、奖励签名密钥或 service token。
- [ ] 钱包、奖励、赛事交易都使用系统浏览器。
- [ ] AssetBundle 加载前完成 Keccak 内容哈希验证。
- [ ] 客户端自报资产、比分、名次和奖励资格均不作为最终事实。
