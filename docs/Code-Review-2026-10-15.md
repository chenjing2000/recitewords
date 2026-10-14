# ReciteWords 代码审查（2026-10-15）

按 [project-code-review](skills/project-code-review/SKILL.md) 审查当前 main 分支。此前的 [审查记录](Code-Review-2026-10-13.md) 保留为历史资料。

## 模块与代码质量

检查词库解析、重复和 Unicode 校验，进度与设置读写，复习队列与快照，评价失败回滚，筛选计数与排队播放，音频目录与播放器生命周期，窗口快捷键、释义滚动及测试、发布流程。

- App 保持单一装配点；Storage 负责文件校验与持久化；Review 不依赖 WPF 或磁盘；ViewModel 协调保存、通知和播放，Views 处理呈现与输入。
- 评价前捕获快照、保存失败恢复队列与状态；导航不保存进度。快照与持久化模型职责不同，保留有必要。
- AudioCatalog 根据词库顺序确定首次 stem 音频资格，播放器负责原生媒体。ASCII stem、MP3 存在优先、新目录和取消 JSON 索引的规则一致。
- 服务接口有真实替身测试用途，JsonFields、JsonFileWriter 和 Spelling 有实际复用价值。没有引入新的仓储、容器、事件总线或配置层；少量简单重复和当前命名保持不变。

## 必须修复：状态值严格校验

Progress-Format 规定只接受大小写准确的 Unknown、Familiar、Mastered。原 LoadOrCreate 在比较之前 Trim，实际接受 ` Unknown`、`Familiar ` 和包含制表符、换行的 Mastered。

先增加三个非法状态且原文件保留的回归：修复前 98 通过、3 失败，失败均为非法输入被接受。去掉状态读取的 Trim 后，按原协议精确比较；不修改词条键的标准化、合法状态、保存结构、坏文件保留或复习行为。

## 文档与清理

Progress-Format 明确状态不接受首尾空白；该文档和 Design-Purpose 删除仍暗示独立音频索引存在的过期表述。文档索引指向本报告。现有词库 skill 符合当前协议，无需修改。

未确认任何废弃源码或应删除的项目资源；本轮没有删除文件。测试使用临时目录并自行清理，现有 .gitignore 已覆盖构建输出与临时文件。dist、配置、学习记录和用户词库均保留。

## 验证与边界

- 最终 `dotnet run --project ReciteWords.Tests -c Release -- --ui-qa`：106 通过、0 失败，包含三个新增非法状态用例、失败保存保护、回滚、筛选计数、键盘、滚动及本地 WAV 打开和结束。
- Release 构建：0 警告、0 错误；Git 差异空白检查通过。
- PowerShell 源 ASCII/语法检查通过；Test-Publish 五项保护检查、Test-Workflow 三项编排检查通过。脚本测试使用隔离临时文件和替身命令，不代表真实发布 EXE 验收。
- 没有人工听音、混合 DPI 设备验证、完整依赖安全审计或本次 dist 发布验收。本次不重新发布 EXE。

本轮没有发现需要重大架构调整的问题；上述验证不是对所有运行环境及所有并发编辑情形的证明。
