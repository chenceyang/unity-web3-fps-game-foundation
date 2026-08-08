# AGENTS.md

## 项目定位

Unity + C# 的 Web3 FPS 游戏侧底层包；实时战斗与 Web3 资产/奖励/赛事/存证严格分离。

## 技术与验证

- 包声明最低 Unity 2022.3；当前仅验证 Unity 6000.3.21f1。
- 快速门禁：`powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\run_unity_static_audit.ps1`。
- 最终验收：Unity 编译、Unity Test Runner 的 EditMode 测试，以及生成场景的一局 Play Mode 手工验收。
- `work/` 下的审计工程和渲染图是一次性证据，不是产品源码。
- 现役 Git 历史位于 `work/upload-git`，命令使用 `git --git-dir=work/upload-git --work-tree=.`；根 `.git` 是未使用的空仓库。在专项迁移前不要删除或混用两者。
- GitHub 远端只保留并默认使用 `codex/v1.9.1`；不要重新创建已退役的 `main`、`codex/v1.7.0` 或 `codex/v1.7.4`。

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

v1.9.1 合并了 v1.9.0 的权威边界、NFT 外观、四页 UI、六武器目录以及 v1.7.4 的 Windows 角色稳定性修复。生成场景不再实例化曾导致巨大化/无效包围盒的第三方骨骼 FBX，而使用模块化角色、程序化动作与稳定第一人称手臂；FBX 仅保留为可替换的 CC0 参考资产。Unity 6000.3.21f1 引用编译与静态执行器 117/117 项通过，包已同步到现役集成工程；隔离 Test Runner 因缺少 headless 许可无法启动。下一步在交互式 Unity 完成场景重建、Test Runner 与整局 Play Mode 验收，再接入联网 SDK 和真实后端。详见根 `README.md`。
