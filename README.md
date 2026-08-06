# Unity Web3 FPS Game Foundation

本目录保存 Unity 游戏侧底层包、设计需求交付物和临时验证产物。实时 FPS 与 Web3 资产层分离：战斗由 Unity/权威服务器处理，钱包、NFT、奖励、赛事和结果存证只通过游戏后端异步接入。

## 现役来源

- `UnityWeb3FpsGameFoundation/`：唯一可编辑的包源码。
- `UnityWeb3FpsGameFoundation/README.md`：包安装、接口和集成合同。
- `UnityWeb3FpsGameFoundation/Documentation/INTEGRATION_CHECKLIST.md`：尚未完成的游戏/服务器接入清单。
- `outputs/UnityWeb3FpsGameFoundation_v1.2.zip`：由包源码生成的当前分发产物，不是编辑源。
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
- v1.2 Runtime、Editor 和 Tests 程序集已使用 Unity 6000.3.21f1 的 Roslyn/引用程序集独立编译；
  v1.2 Web3 Lobby 加入后共有 24 个 EditMode 测试由静态执行器运行并全部通过。
- v1.2 已加入独立 Web3 Lobby 生成器和状态控制器：Mock NFT loadout、钱包、奖励和赛事流程均有测试；
  完整 Unity 批处理验证仍被 Licensing Client 阻塞。

## 尚未完成

- 正式视觉版大厅、衣柜、钱包、奖励、赛事、市场和赛后 UI；当前只有功能型 IMGUI Web3 Lobby。
- 生产级玩家 Prefab、正式地图、武器表现和内容资产；生成式本地 Prototype 已有，但尚未实机复验。
- 具体联网 SDK、权威服务器适配、断线重连与资产 snapshot。
- 后端 entitlement-check、存证队列、奖励幂等链路及端到端联调。
- 需求文档中的 14 项端到端验收；现有测试仅覆盖底层组件。

下一阶段先在 Unity 中实机验收 Prototype 与 Web3 Lobby，再推进“联网 SDK/权威服务器边界 → 真实后端联调 → 端到端验收”。不要把生成器或包成功编译等同于完整游戏已经完成。
