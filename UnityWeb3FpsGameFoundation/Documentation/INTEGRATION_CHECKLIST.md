# Unity 集成检查单

## ASH//LEDGER 垂直切片 v1.6

- [x] 提供一键生成 UI Toolkit 正式大厅与 RIFT RELAY 战斗场景的 Unity 菜单。
- [x] 大厅绑定 Mock 钱包、NFT、奖励和赛事状态，PLAY 始终为默认焦点。
- [x] 战斗 HUD 不显示钱包、tokenId、交易或链状态。
- [x] 生成 KESTREL-7、PULSE-9、RELAY-3 三把可编辑灰盒 Prefab。
- [x] RIFT RELAY 包含三路结构、阵营色、中央中继柱、冷却通道和硬掩体。
- [x] v1.4.1 加入实际场景使用的轨道背景、开放式边界、工业塔群、雾效、发光材质和局部灯光。
- [x] v1.4.2 使用包内独立角色 Prefab，加入敌方装甲人形和第一人称双臂，避免旧 Prefab 的 missing script。
- [x] v1.4.3 显式持久化 UIDocument 的 PanelSettings/UXML 引用，并将轨道背景设为双面渲染。
- [x] v1.4.4 根据激活的渲染管线选择 Shader，并阻止在 Play Mode/编译过程中执行生成器。
- [x] v1.4.5 在 UIDocument OnEnable 后写入并校验 PanelSettings，背景同时生成反向平面。
- [x] v1.4.6 正式 View 保存备用 PanelSettings，并在运行时恢复 UIDocument；静态审计 36/36 通过。
- [x] v1.5 加入弹匣/备弹、R 装填、装填进度、空仓警告、命中标记以及玩家/机器人视觉弹道。
- [x] 弹道与枪口闪光只消费权威射击结果，不参与伤害、比分或 Web3 数据计算；静态审计 39/39 通过。
- [x] 大厅预览复用完整装甲角色，第一人称双臂、手套和装甲细节得到加强。
- [x] v1.6 导入 CC0 骨骼士兵 FBX，记录来源、许可和 SHA-256，并生成 Idle/Run/Shoot/Death Animator 状态机。
- [x] 第一人称武器具备换弹弧线、弹匣抽插、枪械后坐力、相机后坐力以及原创射击/机械音效；静态审计 41/41 通过。
- [x] NFT 槽位与武器内容目录具备 EditMode 覆盖；静态审计 35/35 通过。
- [ ] 在 Unity 6000.3.21f1 中执行生成器并确认 UXML/USS 无导入错误。
- [ ] 手工完成大厅进入 RIFT RELAY、击杀、复活和结算的一整局。
- [ ] 根据商业美术预算决定是否用定制 FBX、动作捕捉和录音棚音频替换当前 CC0 低多边形资产。

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

## 权威服务器边界 v1.3

- [x] `LoadoutSnapshotResolver` 在开局前核验并冻结每名玩家的外观快照。
- [x] `HttpEntitlementGateway` 仅提供专用服务器调用的 entitlement-check 适配器，凭据由内存委托注入。
- [x] 核验失败时整套 loadout 降级默认皮肤，不阻断普通对局。
- [x] `AuthoritativeMatchSession` 只在快照完成后启动 `MatchCoordinator`，结算结果不可再次修改。
- [x] `MatchPublishCoordinator` 保证相同结果成功发布一次，失败可重试，并拒绝同 matchId 的冲突结果。
- [x] OpenAPI 已补充 entitlement-check 请求与快照响应，tokenId 始终声明为字符串。
- [x] 使用 Unity 6000.3.21f1 引用程序集编译并静态执行 32 个测试全部通过。
- [ ] 把边界接入选定的联网 SDK 和真实专用服务器进程。
- [ ] 实现真实后端 entitlement-check、持久化存证队列与监控告警。
- [ ] 经联网 SDK 广播 resolved skin 快照并完成双客户端端到端验收。

## 大厅

- [ ] 创建并持久化 `GameFoundationBootstrap`。
- [ ] 登录后仅在内存中设置 access token。
- [ ] 进入大厅调用 `RefreshLobbyAsync`，捕获取消异常。
- [ ] `UsedFallback` 时显示轻提示，不禁用普通匹配。
- [ ] 衣柜只把 confirmed 资产加入正式 loadout。
- [ ] tokenId 全链路保持字符串。

## 开局

- [ ] 客户端提交 loadout 意图。
- [x] 游戏服务器侧已提供 entitlement-check 调用边界，不信任客户端。
- [ ] 服务器生成 snapshotId，并向所有客户端下发最终 resolved skin。
- [x] 核验失败时改用默认皮肤，不拒绝玩家进入普通对局。
- [ ] 对局开始后停止全部资产、奖励和赛事轮询。

## 战斗

- [ ] 网络适配器发送 `PlayerInputFrame` 和 `ShotCommand`。
- [ ] 只有权威服务器执行命中、伤害、死亡与比分。
- [ ] `Health.authorityProvider` 指向联网 SDK 的权威实现。
- [ ] NFT 皮肤不改变伤害、射速、碰撞体或后坐力。

## 结算

- [x] 服务器会话封装 `MatchCoordinator.Finish`，并缓存不可变结算结果。
- [x] `MatchPublishCoordinator` 保证同一 matchId 的同一结果成功发布一次。
- [ ] 后端保存 canonicalJson 并验证 resultHash。
- [ ] 存证失败进入异步重试，不阻断赛后页或下一局。
- [ ] 高价值奖励在反作弊状态 passed 前不得铸造。

## 安全

- [ ] Unity 工程和日志中不存在私钥、助记词、RPC key、奖励签名密钥或 service token。
- [ ] 钱包、奖励、赛事交易都使用系统浏览器。
- [ ] AssetBundle 加载前完成 Keccak 内容哈希验证。
- [ ] 客户端自报资产、比分、名次和奖励资格均不作为最终事实。
