# Unity Web3 FPS Game Foundation

本目录保存 Unity 游戏侧底层包、设计需求交付物和临时验证产物。实时 FPS 与 Web3 资产层分离：战斗由 Unity/权威服务器处理，钱包、NFT、奖励、赛事和结果存证只通过游戏后端异步接入。

## 现役来源

- `UnityWeb3FpsGameFoundation/`：唯一可编辑的包源码。
- `UnityWeb3FpsGameFoundation/README.md`：包安装、接口和集成合同。
- `UnityWeb3FpsGameFoundation/Documentation/INTEGRATION_CHECKLIST.md`：尚未完成的游戏/服务器接入清单。
- `UnityWeb3FpsGameFoundation/Documentation/FORMAL_CONTENT_DESIGN.md`：正式 UI、美术、地图、武器与 Web3 创意的制作规格。
- `outputs/UnityWeb3FpsGameFoundation_v1.9.1.zip`：由包源码生成的当前分发产物，不是编辑源。
- `tools/build_game_design_doc.py`：需求文档 DOCX 的可复现生成器。
- `references/`：带来源和哈希说明的上游只读快照。
- `work/`：主要保存一次性审计与集成工程，不是产品源码；其中 `upload-git` 暂存现役 Git 历史，不能随普通临时文件删除。

## 当前已验证状态（2026-08-09）

- 权威包源码与交付 ZIP 均为 `1.9.1`；源码已合并 v1.9.0 功能线与 v1.7.4 角色稳定性修复。
- `tools/run_unity_static_audit.ps1` 使用 Unity 6000.3.21f1 引用程序集完成 Runtime、Editor、Tests 编译，117/117 项测试通过（5 个既有序列化字段警告）。
- 本地垂直切片已实现 UI Toolkit 大厅与战斗 HUD、RIFT RELAY 三路地图、稳定的模块化角色与程序化第一人称手臂、弹药/装填/后坐力/音效、分区伤害/出生保护、动态散布和具备视线/反应/记忆/侧移的 Bot。
- Web3 侧已实现 Mock/HTTP 资产网关、confirmed loadout、赛事交易意图、AssetBundle 哈希校验、开局 entitlement 边界、结果哈希及发布去重/重试；所有 Web3 调用仍在战斗帧之外。
- v1.7.0 尚未在 Unity 中重新执行场景生成器，也未完成 Test Runner 与一整局 Play Mode 验收；静态门禁不能替代该运行态验证。
- `1.9.1` 保留 v1.8.0 的手感/GC/契约修复与 v1.9.0 的权威边界、NFT 外观、四个 UI Toolkit 页面及六武器目录；运行时不再实例化曾产生异常包围盒和巨大化几何的第三方骨骼 FBX。
- 已同步到现役 Unity 集成工程；隔离批处理 Test Runner 因本机缺少 `com.unity.editor.headless` 许可未能启动，仍需在已授权的交互式 Unity 中运行 Test Runner、重建场景并完成整局 Play Mode 验收。
- 版本历史与每版测试增量只保留在 `UnityWeb3FpsGameFoundation/CHANGELOG.md`。

## 尚未完成

- 完整正式前端已有登录、匹配确认、资产详情和独立赛后灰盒页面；最终商业美术、完整市场流程与交互打磨仍未完成。
- 最终定制角色拓扑、面部表现、动作捕捉、录音棚音频和最终地图资产；当前是可商用 CC0 低多边形角色及功能性表现管线。
- 具体联网 SDK 传输层、断线重连、快照广播和双客户端联调。
- 真实后端 entitlement-check、持久化存证队列、奖励幂等链路及端到端联调。
- 需求文档中的 14 项端到端验收；现有测试仅覆盖底层组件。

下一阶段先在 Unity 中重建并实机验收 v1.9.1 垂直切片，再制作定制骨骼角色/武器最终资产并选定联网 SDK；随后把现有权威边界接入专用服务器和真实后端，完成双客户端端到端验收。不要把生成器或包成功编译等同于完整游戏已经完成。
