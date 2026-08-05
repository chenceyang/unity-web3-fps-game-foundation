# Unity Web3 FPS Game Foundation

本目录保存 Unity 游戏侧底层包、设计需求交付物和临时验证产物。实时 FPS 与 Web3 资产层分离：战斗由 Unity/权威服务器处理，钱包、NFT、奖励、赛事和结果存证只通过游戏后端异步接入。

## 现役来源

- `UnityWeb3FpsGameFoundation/`：唯一可编辑的包源码。
- `UnityWeb3FpsGameFoundation/README.md`：包安装、接口和集成合同。
- `UnityWeb3FpsGameFoundation/Documentation/INTEGRATION_CHECKLIST.md`：尚未完成的游戏/服务器接入清单。
- `outputs/UnityWeb3FpsGameFoundation_v1.0.zip`：由包源码生成的分发产物，不是编辑源。
- `tools/build_game_design_doc.py`：需求文档 DOCX 的可复现生成器。
- `references/`：带来源和哈希说明的上游只读快照。
- `work/`：一次性编译审计与 PDF 检查产物，不是项目事实来源。

## 当前已验证状态（2026-08-06）

- 包已在 Unity `6000.3.21f1` 中导入；运行时和 EditMode 测试程序集均已编译。
- `com.unity.test-framework 1.6.0` 与 `com.unity.ext.nunit 2.0.5` 已在当前集成工程解析。
- 纯 C# Keccak/结果序列化审计通过，Unity API 语法审计为 0 错误、0 警告。
- 已实现 Mock/HTTP 资产网关、钱包/奖励轮询、loadout 校验、赛事交易意图、结果哈希、AssetBundle 内容校验，以及基础移动/射击/伤害组件。
- 当前集成工程中的 `Prototype` 场景只有相机和方向光，尚不是可玩的 FPS 场景。

## 尚未完成

- 大厅、衣柜、钱包、奖励、赛事、市场和赛后 UI。
- 玩家 Prefab、地图、死亡/复活、比分/胜负和完整本地对局。
- 具体联网 SDK、权威服务器适配、断线重连与资产 snapshot。
- 后端 entitlement-check、存证队列、奖励幂等链路及端到端联调。
- 需求文档中的 14 项端到端验收；现有测试仅覆盖底层组件。

下一阶段按“可玩本地垂直切片 → Web3 游戏侧控制器/UI → 联网与服务器边界 → 端到端验收”推进。不要把包成功编译等同于完整游戏已经完成。
