# Unity 集成检查单

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
