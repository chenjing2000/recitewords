# 模块职责与数据流

本文描述升级后的源代码职责划分，数据协议见 [格式 skill](skills/recitewords-wordlist-format/SKILL.md) 和 [进度格式](Progress-Format.md)。

## 模块与边界

| 模块 | 主要文件 | 主要功能 | 边界 |
|---|---|---|---|
| 启动 | `ReciteWords/App.xaml.cs` | 创建文件仓库、复习会话、音频服务、ViewModel 和窗口；启动加载 | 手工组装，不包含词库校验或复习算法 |
| Models | `Word`、`WordSense`、`WordList`、`ReviewProgress`、`AppSettings` 等 | 表达单词、词义、进度、设置及状态枚举 | 数据容器，不访问文件，不播放音频，不操作控件 |
| Common | `JsonFields`、`Spelling`、`AudioFileName` | 校验 JSON 对象、字段类型、重复键、Unicode；提供共享拼写 casefold 比较与 AudioFileName 音频主体转换 | 不依赖 Storage 或 Audio，不处理窗口或业务状态 |
| Storage | `WordListRepository` | 扫描当前目录直属 JSON；校验并加载词库；返回目录条目与错误 | 只读词库；不修改原词库，不决定评价和播放 |
| Storage | `ProgressRepository` | 计算每本词库的进度路径；读取、校验和保存进度 | 不兼容 Python 进度，不安排复习 |
| Storage | `SettingsRepository`、`JsonFileWriter` | 读取设置；采用临时文件写入后替换目标的方式保存 JSON | 只负责持久化；保存失败交给调用者处理 |
| Review | `ReviewSession`、`ReviewSnapshot` | 按状态筛选、建立内存固定队列、前后导航、评价、分类计数、会话快照与回滚 | 不知道文件路径，不显示 UI，不操作音频 |
| Audio | `AudioCatalog` | 按 words 顺序预留首次 stem 资格，直接查找单词与局部 eid 例句文件 | 不读取词库正文、不修改学习进度、不下载音频 |
| Audio | `WpfAudioPlayer` | 使用 WPF MediaPlayer 播放；新播放停止旧播放，结束释放，报告失败 | 不选择当前词，不决定自动播放时机 |
| ViewModels | `MainViewModel`、`SimpleCommand`、`FilterChoice` | 编排打开、筛选、评价、保存与回滚；提供绑定和命令；更新数量、释义可见性与警告 | 通过服务接口调用，不在其中重写文件格式解析或复习算法 |
| Views | `MainWindow` | 布局、目录选择、提示计时、窗口事件、尺寸测量 | 不直接读写学习记录 |
| Views | `DefinitionView` | 用 FlowDocument 绘制词义、字段、缩进、例句与内联音频按钮；转发手动滚动 | 不决定学习状态；播放操作交给 ViewModel |
| Resources / ui | 样式、矢量图标、主图标、casefold 数据 | 保存显示资源与拼写比较数据 | 原始图标和开发生成资源分开，运行时不依赖生成工具 |
| Platform | `WindowBounds` | 根据显示器、工作区和 DPI 管理窗口尺寸与最大化约束 | 不包含词库或学习逻辑 |
| Tests / tools | `ReciteWords.Tests`、开发脚本 | 验证文件、复习、音频、ViewModel 和 WPF 界面；生成图标及 casefold 资源 | 不属于正式程序的运行依赖 |

## 服务接口

`MainViewModel` 依赖 `IWordListRepository`、`IProgressRepository`、`ISettingsRepository`、`IReviewSession`、`IAudioCatalog`、`IAudioPlayer`。这些接口对应实际模块边界，既便于替换测试对象，也避免界面层依赖具体文件和播放实现。

Models 是共同数据协议，Common 是低层共享工具。Storage、Review、Audio 各自依赖所需数据和工具，不通过调用另一个模块的具体实现来绕过接口。Views 可以使用 WPF 控件与 ViewModel，但 Review 不依赖 WPF。Rate 直接编排快照、评价、保存和失败回滚，不经过通用更新回调。

接口并不意味着为每个类建立接口；简单数据类、JSON 辅助函数、样式和控件无需额外包装。

## 打开单词本

1. App 组装服务，MainViewModel 读取设置，确定默认目录或上次目录；选择没有有效词库的目录也会记住该目录。
2. WordListRepository 扫描、验证词库，保留名称与路径；选择后重新读取文件，避免缓存过期的整本词库。
3. ProgressRepository 查找版本 2 userdata 记录并恢复单词状态，缺少时初始化；ReviewSession 从全部分类第一词建立新的内存队列。
4. AudioCatalog 接收已经校验的 WordList，确定同名子目录 audio/ 和 examples/，按 words 顺序预留首次 stem；不读取任何音频索引 JSON。
5. MainViewModel 更新绑定、隐藏释义、停止旧播放，并安排当前词的 UK 自动播放。连续切词时使旧的排队播放请求失效。

Common.AudioFileName.Stem 去除首尾空白后，将每个非 ASCII 字母/数字替换为 `_`。Storage 不再拒绝 stem 冲突，仅保持原始拼写唯一；AudioCatalog 用不区分大小写的集合按 words 顺序预留 stem，无需判断文件是否存在。后续词禁用单词和例句音频，显示及进度键不变。

单词音频查 audio/{stem}_uk/us.mp3/.wav；例句查 examples/{stem}_e{eid}_uk/us.mp3/.wav，MP3 优先。eid 只在单词内按实际例句顺序从 01 起唯一，所以 FindExample 接收 word、eid 和口音，不再只接收 eid。

AudioCatalog 不解析 JSON 索引，Load 无需返回配置错误集合；词库数据从 Storage 经 ViewModel 传入，Audio 不依赖具体 Storage 实现。只有文件存在时喇叭才可点击。

## 导航、评价与保存

导航和分类选择只改变内存会话，不写文件。Start 只接收分类，每次从第一词开始；LoadOrCreate 只接收单词本路径，不接收词库内容。评价前 Capture 深复制状态字典和队列到 ReviewSnapshot，再覆盖当前状态并前进；GetProgress 只返回可保存状态，ProgressRepository 写入版本 2 文件。

成功后更新界面、数量和播放；失败时 Restore 恢复状态、分类、队列、位置与完成标记，不停留在未保存成功的新状态。

评价只覆盖 Unknown、Familiar 或 Mastered，不增加次数。本轮队列固定，选择分类时重建；完成后允许后退纠正最后一个词。重新打开只恢复单词状态，从全部分类第一词开始。

Models 的 ReviewProgress 只含可保存数据，Review 的 ReviewSnapshot 含内存会话。word 去除首尾空白并经过 Common.Spelling 的 Unicode casefold 后作为进度键，修改实际拼写不会自动关联原状态。

## 文件的职责

| 文件 | 用途 | 谁维护 |
|---|---|---|
| `<单词本名>.json` | 单词内容与释义 | 用户或生成工具；程序只读 |
| `<单词本名>/audio/{stem}_uk.mp3/.wav`、`{stem}_us.mp3/.wav` | 单词英美音频文件 | 用户或音频准备工具 |
| `<单词本名>/examples/{stem}_e{eid}_uk.mp3/.wav`、`{stem}_e{eid}_us.mp3/.wav` | 例句英美音频文件 | 用户或音频准备工具 |
| `userdata/<单词本名>.progress.json` | 单词当前状态（版本 2） | 程序保存 |
| `recitewords.settings.json` | 上次目录、词库、窗口信息 | 程序保存 |

上表的单词本名指文件名去掉 `.json`，不是 JSON 中的显示名称 `name`。单词本的 `schema_version` 可以是任意正整数；进度和设置有各自独立的版本校验；音频文件不再依赖 JSON 索引，不能把单词本规则套用到这些文件。

## 维护与验证

修改文件规则时检查 Storage 与对应测试；修改学习规则时检查 Review 及保存回滚路径；修改显示时检查 Views、ViewModel 绑定与真实 WPF 窗口测试。正式发布前再验证构建、代表性数据及启动关闭，不能仅凭文档或代码外观声称功能完成。

当前升级已同步双音标模型、读取、绑定、显示、测试夹具，以及必填 pos 校验。删除了旧 WordProgress 次数模型，复习快照仅用于内存回滚，不进入文件。

破坏性升级不迁移旧进度，不读取旧 audio.json；旧单音标词库和版本 1 进度读取失败。没有增加数据库、播放器或通用字段框架。

当前详细音频协议见 [音频文件规范](Audio-Format.md)。释义外框最低 5em，Etymology 在 Notes 前；筛选 ItemTemplate 用一个英文空格的实际宽度设置左边距。
