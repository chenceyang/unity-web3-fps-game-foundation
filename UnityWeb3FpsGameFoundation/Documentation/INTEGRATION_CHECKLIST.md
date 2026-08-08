# Unity 集成检查单

## 角色稳定性与权威边界 v1.9.1

- [x] 合并 v1.7.4 的 Windows 角色稳定性修复：生成场景不实例化第三方骨骼 FBX，避免异常包围盒与巨大化几何。
- [x] Bot 与大厅预览改用模块化角色；第一人称手臂改用稳定程序化模型，并保留行走、后坐力和死亡反馈。
- [x] 新增程序化动作 EditMode 覆盖；保留 v1.9.0 权威边界、Web3 页面和六武器实现。
- [x] Unity 6000.3.21f1 引用程序集完成 Runtime/Editor/Tests 编译，静态执行器 117/117 项测试通过。
- [x] 保留 v1.7.4 的 RIFT RELAY 宇宙轨道战斗背景；材质/背景 PNG 尚未导入时先强制同步导入一次，确实缺失才使用可读颜色回退并记录警告。
- [x] 包源码镜像同步到 `work/UnityWeb3FpsGameFoundation-v1.4/Packages/com.web3fps.game-foundation`。
- [ ] 在 Unity 中重新生成 `AshLedgerLobby`/`RiftRelay`，执行 Test Runner 并完成整局 Play Mode 验收。

## 权威边界落地与四页补全 v1.9.0

- [x] `LocalAuthoritativeMatchDriver` 在本地切片中扮演专用服务器：确认页在进战斗场景前冻结 entitlement 快照，战斗帧零网关调用，结束后经 `MatchPublishCoordinator` 发布一次并把 published/duplicate/conflict/failed 显示到赛后页与大厅；`AuthoritativeMatchSession` 保留给联网阶段（服务器同时拥有逐击杀计分）。
- [x] 包侧 DTO 对齐真实后端：entitlement 请求改为 `{playerId, matchId, wallet, tokenIds}` 并映射 `EntitlementResult` 响应（含每槽 contentHash 与 degraded 原因）；结果发布直接 POST 规范化 MatchResult 原文（后端自行 canonicalize + 哈希）；`MatchRewardSlot.slot` 改为 uint8 整数并由本地驱动派生确定性的冠军奖励槽。
- [x] 登录页：demo `POST /v1/auth/login` 客户端（Mock/Http 双实现，明确 demo-only），token 仅经 `SetAccessToken` 驻内存；游客入口永不依赖钱包。
- [x] 匹配确认页：模式/地图、冻结 loadout 每槽皮肤名与确认态、降级警示行；entitlement 失败只降级默认外观，不阻断部署。
- [x] 资产详情页：serial/maxSupply/season/rarity/wear、完整十进制 tokenId、contentHash 缩写、状态解释、装备/默认操作与 ChainConfig marketplaceUrl 系统浏览器跳转。
- [x] 独立赛后页：比分/名次、发布结果与 `GetMatchAsync` 存证状态；存证失败或档案不可达绝不遮挡比分（MAT-006），REMATCH/返回大厅始终可用。
- [x] NFT 外观目录：`FormalSkinCatalog` 覆盖后端目录与 SeedSkins.s.sol 的全部 skinDefId，未知 id 落中性默认；`FormalSkinApplicator` 仅经 MaterialPropertyBlock 改视觉；`VerifiedSkinBundleLoader` 获得调用点，hash 不符降级默认外观并告警。
- [x] `WeaponDefinition` 目录承载六把武器的服务器数值并可写入 `HitscanWeapon`；生成器新增 WITNESS/BREACH-12/ANCHOR 灰盒 Prefab 与大厅陈列。
- [x] `GameFoundationBootstrap` 的 mock/live 开关同时组合 entitlement 网关、结果发布器与登录客户端；service token 仅内存注入且仅在本进程扮演服务器角色时有意义。
- [ ] 在 Unity 6000.3.21f1 重新运行 Test Runner，并人工验收 v1.9.1 生成场景。
- [ ] 实机验收：登录/游客 → 装备预览变色 → 确认页 → RIFT RELAY → 赛后页 REMATCH/返回大厅 → 大厅显示上局发布状态。
- [ ] 对着运行中的 web3-fps-assets 后端用 live 模式验证 entitlement-check 与 /internal/v1/matches 的真实往返（本机只做了 schema 对齐）。
- [ ] 战斗内武器切换（数字键）仍未实现；六把武器目前只有定义与 Prefab。

## 手感与契约对齐 v1.8.0

- [x] 输入驱动先于 Motor/武器执行并同帧应用视角旋转，消除移动与射击方向落后一帧的问题。
- [x] Motor 在下坡时将控制器贴回可行走面，消除坡道节奏性颠簸;贴地距离受 stepOffset 钳制。
- [x] 清除稳定的逐帧 GC 分配源:骨骼保险缓存渲染器、机器人物理查询 NonAlloc、战斗特效池化、HUD 只在数值变化时更新、武器音效预生成。
- [x] `Game.Web3` 与赛事层逐字镜像现役 web3-fps-assets unity-sdk:新增 `GetConfigAsync`/`ChainConfig`、完整奖励状态机、`Amount` 金额;赛事改为单一 intent 路由 + `GetMatchAsync`,移除交易轮询。
- [x] 空 2xx 响应在绑定/领奖轮询中按瞬态处理;结果发布对 HTTP 409 抛专用冲突异常并快速失败,不再重试注定失败的负载。
- [x] `package.json` 声明内置模块依赖;`tools/generate_unity_metas.py` 为全部资产生成确定性 GUID 的 `.meta`。
- [ ] 在 Unity 6000.3.21f1 重新运行静态审计与 Test Runner(61 个用例),确认 0 错误后再实机验收。
- [ ] 实机确认新贴地逻辑在 RIFT RELAY 坡道与跳跃时的手感,必要时调整 `GroundSnapDistance` 的斜率上限。
- [ ] 后端就绪后用 `HttpGameAssetGateway`/`HttpTournamentGateway` 替换 Mock,验证"换实现不改其余代码"。

## ASH//LEDGER 垂直切片 v1.7.0

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
- [x] v1.6.1 修正敌人被中央中继柱永久遮挡的问题，加入绕障、识别灯和第一人称视野优化；静态审计 43/43 通过。
- [x] v1.6.2 缩小玩家手臂/武器灰盒，敌方 FBX 保留纹理并增加阵营染色、阴影和高对比标记。
- [x] v1.6.3 缩小中央中继建筑并将双方开场移到无遮挡车道；布局回归测试保证敌人不会再被建筑或掩体遮住。
- [x] v1.6.4 修正 FBX 单位/轴缩放动画污染，并加入运行时蒙皮包围盒保险，避免敌人再次膨胀成巨型结构。
- [x] v1.7.0 将 FBX 动作复制为独立 `.anim`，生成骨骼第一人称手臂，并在复活后重新执行骨骼尺寸保险。
- [x] 加入头/躯干/腿命中区域、出生保护、动态腰射/ADS/移动/连射散布及只对实际伤害显示的命中确认。
- [x] 机器人加入 LOS、反应时间、丢失目标记忆、距离控制、侧移、散布和三级难度参数；地图掩体/中央目标完成重排。
- [x] Runtime、Editor、Tests 静态编译通过；NFT 槽位、内容目录及战斗升级均有覆盖，静态执行器 53/53 通过。
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
