# AGENTS.md

## 项目定位

Unity + C# 的 Web3 FPS 游戏侧底层包；实时战斗与 Web3 资产/奖励/赛事/存证严格分离。

## 技术与验证

- 包声明最低 Unity 2022.3；当前仅验证 Unity 6000.3.21f1。
- 快速门禁：`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run_unity_static_audit.ps1`。
- 最终验收：Unity 编译、Unity Test Runner 的 EditMode 测试，以及生成场景的一局 Play Mode 手工验收。
- `work/` 下的审计工程和渲染图是一次性证据，不是产品源码。
- 现役 Git 历史位于 `work/upload-git`，命令使用 `git --git-dir=work/upload-git --work-tree=.`；根 `.git` 是未使用的空仓库。在专项迁移前不要删除或混用两者。

## 目录

- `UnityWeb3FpsGameFoundation/`：唯一可编辑的 UPM 包源码。
- `outputs/`：用户交付物；ZIP 是派生产物。
- `tools/`：可复现的文档/验证脚本。
- `references/`：上游只读快照及来源说明，不在其中开发。
- `work/`：临时审计、构建和检查文件；`upload-git` 是上述 Git 元数据例外，`UnityWeb3FpsGameFoundation-v1.4` 是当前 Unity 集成验收工程。

## 必须保持的边界

- Unity 客户端不保存私钥、助记词、RPC 密钥或服务端签名密钥。
- 战斗帧内不得调用 Web3/资产网关；Web3 故障不得阻断普通对局。
- `tokenId` 全链路使用十进制字符串，不转换为整数类型。
- 客户端资产、比分、名次和交易状态不作为最终事实。
- NFT 只能改变视觉/音效，不得影响战斗数值。
- 修改运行时代码时同步相应 EditMode 测试和包 README/接入清单。

## 当前状态与下一步

v1.7.0 已包含可生成的 UI Toolkit 大厅、RIFT RELAY 本地战斗垂直切片、骨骼角色/第一人称手臂、分区命中、枪械表现和战术 Bot；静态 Runtime/Editor/Tests 编译及 53 项测试通过。源码现为 v1.9.0：v1.8.0 修复移动/瞄准手感与逐帧 GC 卡顿源并对齐 `Game.Web3` 契约；v1.9.0 接线权威边界（本地进程扮演专用服务器角色：开局 entitlement 冻结 → 战斗零 gateway 调用 → 结算哈希发布，DTO 已逐字段对齐真实后端）、NFT 皮肤目录/应用器、登录/匹配确认/资产详情/赛后四页与六把武器定义（详见 CHANGELOG）；**v1.8.0 与 v1.9.0 均未运行任何静态审计或 Unity 验证（本机为 macOS，无审计环境），115 个测试用例待验证**。下一步先在有 Unity 的环境跑静态审计 + Test Runner + 实机验收，再接入联网 SDK。详见根 `README.md`。
