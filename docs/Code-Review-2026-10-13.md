# 代码审查与局部简化（2026-10-13）

本报告记录统一测试、发布流程之后的本轮审查。旧 [Code-Review.md](Code-Review.md) 记录的是此前整改，其测试计数和清理结果不代表本轮验证。目标是检查实际依赖、责任、冗余和缺陷，保持接口、文件协议和学习行为。

## 模块评估

| 模块 | 实际职责、耦合及结论 |
| --- | --- |
| App | 手工创建六个服务并连接 ViewModel 和 Window；唯一组装点，无需 DI 容器。 |
| Models | 单词、义项、进度、设置和枚举的数据容器；不调用存储、播放或控件。保留现有属性名称和可变列表，避免绑定及调用兼容变化。 |
| Common | JsonFields 集中字段类型、重复键及 Unicode 错误处理；Spelling 保持统一 casefold；AudioFileName 按已确认协议生成 stem。均有直接调用者。 |
| Storage | WordListRepository 只读扫描与解析；ProgressRepository 校验版本 2 状态；SettingsRepository 负责设置；JsonFileWriter 负责临时文件、刷盘和替换。共同写入器有实际复用价值。 |
| Review | 固定队列、分类、导航、评价、计数和回滚状态；不依赖磁盘或 WPF。快照复制隔离字典和队列，Word 对象共享是当前只读词库数据流的约定。 |
| Audio | AudioCatalog 根据已解析词库确认首次 stem 资格并查文件；WpfAudioPlayer 管理 MediaPlayer 生命周期。两者责任不同，保留接口。 |
| ViewModels | MainViewModel 编排打开、保存、失败回滚和播放请求；未混入 JSON 解析或 Win32 调用。SimpleCommand 与 FilterChoice 分别服务命令与数量绑定。当前规模无需拆成多个协调器。 |
| Views | MainWindow 处理键盘、目录对话框、提示计时、测量和窗口事件；DefinitionView 负责 FlowDocument 与内联按钮。布局逻辑有实际需求，保留 WPF 事件及依赖属性。 |
| Platform | WindowBounds 集中显示器和 DPI 的必要 Win32 调用；独立于学习逻辑。混合 DPI 设备行为仍需目标设备验收。 |
| Resources / ui | 嵌入 casefold、生成矢量资源和图标；源 SVG/PNG、ICO 及生成工具存在实际资源或文档引用，保留。 |
| Tests / tools | 测试替身隔离真实存储错误和播放编排；统一 Core/UI/Full、发布和验收脚本保留。临时文件使用系统临时目录，不写用户词库。 |

主要数据流为词库解析 → 复习会话和音频目录 → ViewModel → 绑定显示。评价先捕获会话，再修改并保存；保存失败恢复捕获状态。设置与学习进度分别持久化。

## 必要修正

**相对设置路径无法保存。** `SettingsRepository("Relative.settings.json")` 是现有接口可接受的文件路径形式，但 JsonFileWriter 将目录部分（空字符串）传给 CreateDirectory，导致 ArgumentException。新增回归检查首先得到 94 项通过、1 项失败，堆栈确认失败在目录创建。写入器现在先调用 Path.GetFullPath，随后继续原有临时文件和替换流程。回归验证初次创建、覆盖保存、重读和临时文件清理。程序启动原本传入绝对路径，默认设置位置不变。

## 推荐整改与实际变更

- ProgressRepository 已遍历 JsonProperty，却再用属性名调用 TryGetProperty。JSON 对象查找需逐项搜索，n 个进度条目因此可能产生平方级查找工作。现在直接读取 property.Value，通过现有 JsonFields.ReadString 保留字符串与 Unicode 校验；后续状态判断和进度键校验不变。没有新增缓存或解析框架。
- JsonFields.ReadString 集中字符串类型检查；Text 仍负责必填、去空白和空内容检查；字符串数组保留专用错误信息。新增损坏 Unicode 进度值检查，确认拒绝且原文件不被覆盖。
- .gitignore 补充开发 Python 工具可能生成的 `__pycache__` 和字节码；已有 bin/obj、dist、userdata、个人设置和默认词库排除规则保留。
- 发布脚本的 legacyDirectory 恒为 null，相关迁移、冲突比较、旧 EXE 删除及两项测试分支从未执行；删除这些模板残留及仅供它们使用的变量。当前 EXE 的独占锁检查保留，暂存校验、同卷替换、失败保护及安全清理不变。验收脚本的 integrationProject 恒为 null，createsSettingsAtStartup 恒为 true；移除不可达的集成与不创建设置分支，直接验证启动设置。没有删减实际执行的检查。
- 更新文档索引、应用概述和模块说明，移除概述中其他应用说明；README 的测试命令统一指向现有入口。统一入口、参数、发布 profile 均保留，所有 .ps1 保持 English ASCII。

## 可选事项与保留的复杂度

六个服务接口均用于真实模块边界和替身测试，合并会增加耦合。ReviewSnapshot 是保存失败回滚所需状态，不与持久化进度混用。BigInteger 对应版本号可为任意正整数的协议；固定 casefold 数据保证重复检测和进度键一致，不能换成简单 ToLower。

短事件回调、可空类型、资源 using、ICommand、依赖属性和平台互操作直接服务于 WPF 或生命周期；未发现应去除的业务反射、动态代码、通用仓储、事件总线、复杂继承或异步框架。没有为缩短行数而引入模式匹配、LINQ 链或通用辅助层。

扫描验证与打开重新读取保留，避免引入缓存失效；英美音标、按钮和少量段落构建重复保留，当前不足以证明通用字段框架更简单。全属性通知及每次发布重新统计分类对当前规模可读且一致，大词库若出现实测性能问题，再针对测量结果优化。词库元数据仍是已接受协议，不能作为冗余删除。

## 验证证据与限制

- 统一 `tools/Test.ps1 -Suite Core`：96 项应用检查通过、0 失败；5 项发布保护检查通过、0 失败；脚本 ASCII 与语法检查通过；汇总 3 组通过、0 失败、0 跳过。
- 脚本局部简化后再次运行 Core，结果相同；`tools/Test-Workflow.ps1` 的成功汇总、失败命令拒绝和自包含 Full 编排三项检查通过。Full 编排使用替身，不是 GUI 验收。
- `dotnet build ReciteWords -c Release --no-restore`：0 警告、0 错误。差异空白检查通过，保留原文件已有的 CRLF，使用 `core.whitespace=cr-at-eol` 识别 Windows 行尾。
- 回归先复现相对路径错误，再确认修复。进度格式、坏文件不覆盖、保存失败字节保护、学习快照回滚、分类计数和排队播放编排均通过现有检查。
- 第一次受限运行无法更新 WPF 生成缓存，之后在已授权的提升环境中完成测试。NU1900 表明无法取得在线 NuGet 漏洞信息，本次通过不能等同于在线依赖审计通过。
- 本轮代理只运行 Core；未运行 `--ui-qa`、真实音频、原生输入或发布 EXE 验收。窗口排版、快捷键、媒体解码、混合 DPI 和实际体验需统一串行验收。未发布、提交或推送。

## 清理核查

检查 Git 管理文件和引用后，未找到可以确认无用的源文件。历史设计、格式升级、审查和实施报告继续保留并索引；喇叭 SVG 即使当前使用字符显示，仍是原始素材。没有删除用户文件、词库、音频、配置、进度或 dist。

可再生成的缓存位于根 `obj/`、`ReciteWords/bin/`、`ReciteWords/obj/`、`ReciteWords.Tests/bin/`、`ReciteWords.Tests/obj/`，均已忽略。按协作约定由最终验收者统一清理，本文不把尚未执行的删除记录为完成。

## 统一收尾验证

101 application/UI/audio checks passed, including the new relative-settings-path and malformed-Unicode cases. 发布 EXE 自动验收也通过；本子任务阶段的未测项由主任务补齐。最终证据见 [Health Check](Health-Check-2026-10-13.md)。

统一清理已完成：四个仓库的编译缓存与已确认的测试输出共 29 个目录、约 100 MB；保留 dist、配置、个人素材、原始资源和历史资料。后续构建与测试会重新生成所需缓存。
