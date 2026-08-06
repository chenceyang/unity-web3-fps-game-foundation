# Unity Web3 FPS Game Foundation

本目录保存 Unity 游戏侧底层包、设计需求交付物和临时验证产物。实时 FPS 与 Web3 资产层分离：战斗由 Unity/权威服务器处理，钱包、NFT、奖励、赛事和结果存证只通过游戏后端异步接入。

## 现役来源

- `UnityWeb3FpsGameFoundation/`：唯一可编辑的包源码。
- `UnityWeb3FpsGameFoundation/README.md`：包安装、接口和集成合同。
- `UnityWeb3FpsGameFoundation/Documentation/INTEGRATION_CHECKLIST.md`：尚未完成的游戏/服务器接入清单。
- `UnityWeb3FpsGameFoundation/Documentation/FORMAL_CONTENT_DESIGN.md`：正式 UI、美术、地图、武器与 Web3 创意的制作规格。
- `outputs/UnityWeb3FpsGameFoundation_v1.4.5.zip`：由包源码生成的当前分发产物，不是编辑源。
- `tools/build_game_design_doc.py`：需求文档 DOCX 的可复现生成器。
- `references/`：带来源和哈希说明的上游只读快照。
- `work/`：一次性编译审计与 PDF 检查产物，不是项目事实来源。

## 当前已验证状态（2026-08-06）

- 包已在 Unity `6000.3.21f1` 中导入；运行时和 EditMode 测试程序集均已编译。
- `com.unity.test-framework 1.6.0` 与 `com.unity.ext.nunit 2.0.5` 已在当前集成工程解析。
- 纯 C# Keccak/结果序列化审计通过，Unity API 语法审计为 0 错误、0 警告。
- 已实现 Mock/HTTP 资产网关、钱包/奖励轮询、loadout 校验、赛事交易意图、结果哈希、AssetBundle 内容校验，以及基础移动/射击/伤害组件。
- v1.1 已加入本地可玩 Prototype 的场景/Prefab 生成器、机器人、复活、比分、倒计时、胜负和 HUD；
  尚待在 Unity `6000.3.21f1` 重新编译并手工完成一局。
- v1.3 Runtime、Editor 和 Tests 程序集已使用 Unity 6000.3.21f1 的 Roslyn/引用程序集独立编译；
  权威服务器边界加入后共有 32 个 EditMode 测试由静态执行器运行并全部通过。
- v1.2 已加入独立 Web3 Lobby 生成器和状态控制器：Mock NFT loadout、钱包、奖励和赛事流程均有测试；
  完整 Unity 批处理验证仍被 Licensing Client 阻塞。
- v1.3 已加入 entitlement-check 适配器、默认外观降级、冻结 loadout 快照、权威对局会话和结果发布去重/重试边界；
  尚未接入具体联网 SDK、真实专用服务器进程和持久化后端队列。
- v1.4.5 已开始正式内容制作：加入 UI Toolkit 大厅、RIFT RELAY 三路地图、正式战斗 HUD、三把武器 Prefab、
  Mock Web3 状态绑定、轨道背景与工业照明，并补充独立的低多边形装甲敌人和第一人称双臂；静态编译通过，35 个测试全部通过。

## 尚未完成

- 完整正式前端仍缺登录、匹配确认、资产详情、独立赛后页和市场外跳页；v1.4 已完成首个大厅/HUD 垂直切片。
- 生产级玩家模型、动画、VFX、音频和最终地图资产；v1.4 的地图与三把武器仍是可编辑灰盒。
- 具体联网 SDK 传输层、断线重连、快照广播和双客户端联调。
- 真实后端 entitlement-check、持久化存证队列、奖励幂等链路及端到端联调。
- 需求文档中的 14 项端到端验收；现有测试仅覆盖底层组件。

下一阶段先在 Unity 中实机验收 ASH//LEDGER 垂直切片，再制作角色/武器正式资产并选定联网 SDK；随后把现有权威边界接入专用服务器和真实后端，完成双客户端端到端验收。不要把生成器或包成功编译等同于完整游戏已经完成。
