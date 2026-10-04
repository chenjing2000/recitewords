> 此文档保留原始方案。后续已确认的界面调整以 docs/Getting-Started.md 为准；本次模块整改见 docs/Code-Review.md。

# ReciteWords 实现方案

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. 用户已授权实施；实现结果与实际验证范围见 `docs/Implementation-Report.md`。下方任务清单保留原计划，不将未执行的设备验收标为完成。

**Goal:** 制作使用现有单词本和本地音频、由用户自行安排复习的 Windows WPF 单词背诵软件 ReciteWords。

**Architecture:** 使用简单 WPF + MVVM。数据模型、文件读写、复习逻辑、音频及界面分别负责自己的工作，模块通过少量明确接口交互；启动入口手工组装对象，不使用依赖注入框架。单一产品项目配一个无第三方依赖的控制台测试项目，不拆成多个类库。

**Tech Stack:** C#、WPF、System.Text.Json、WPF MediaPlayer；目标 `net10.0-windows`，Windows Desktop Runtime `10.0.11`，SDK `10.0.400`。

**Spec:** 本文件第 1–8 节构成设计规格，第 9–11 节构成执行及验证计划。合并为一份文档，便于用户在正式编程前审阅。

## 1. 已确认要求与待审阅的设计选择

### Global Constraints

- 程序、窗口、项目和发布文件统一命名为 `ReciteWords`。
- 读取 `C:\pywork\words_review` 所定义的单词本、单词音频、例句音频；不读取或转换 Python 学习记录。
- 单词本扩展支持义项级 `register`、`antonyms` 和单词级 `etymology`；单词本 `schema_version` 接受任意正整数，旧词库缺少这些新字段时仍可导入。
- 单词内容只要求 wid、word、phonetic 和每个 sense 的 chinese_meaning；其他约定字段可省略。文件可以包含未知字段，但只导入已知字段，未知字段的内容不进入模型、界面或学习记录。
- 保留用户手动评价、手动决定何时复习的方式，不加入间隔复习算法。
- 允许未查看释义直接评价；每次换词自动隐藏释义。
- 每次显示新词自动播放已有英音；缺失时不下载、不合成、不自动替换为美音。
- 单词和例句均保留手动英音、美音按钮，缺失的按钮禁用。
- 记住上次单词本文件夹；每本学习记录保存在该单词本所在目录的 `userdata/<单词本文件名去掉扩展名>.progress.json`。打开单词本时自动创建缺失的目录及记录，并读取记录恢复进度。
- 窗口外部宽度不超过所在屏幕宽度的一半，外部高度不超过该屏幕工作区高度；支持缩放。
- 释义自动换行，允许用户手动滚动，不由程序自动滚动。
- 复习按钮旁保留 `<` / `>` 单词导航；允许退回已评价词并重新评价，立即覆盖原状态。不增加损坏记录恢复入口，不自动维护改名后的进度文件名；先进行小样本验收。
- 简单 WPF + MVVM，不使用数据库、嵌入浏览器、通用仓储、依赖注入容器或 MVVM 工具包。
- 优先普通类、属性、显式循环和清楚的方法；不用 record、复杂 LINQ、反射、代码生成或为未来功能预留抽象。
- 产品不依赖 Python 程序或其虚拟环境，不复制 Python 项目进入仓库。
- `.gitignore` 随源代码交付，个人词库、学习记录、设置和发布产物不提交 GitHub。

### 本方案提出的选择

1. 三种评价状态统一为“不懂 / Unknown”“认识 / Familiar”“掌握 / Mastered”。未评价的词通过记录中不存在该词条表示，界面显示“未学”；它不是第四种评价值。
2. 用户已确认：释义区允许手动滚动，不自动滚动或播放时跟随滚动；换词后归位到顶部。
3. 自动读词固定采用英音；第一版不增加口音偏好设置。
4. 按后续界面要求固定所有文字为 11pt（WPF 逻辑像素约 14.67），移除字号控制，不读取或保存旧字号；仍保存窗口位置及尺寸。

这些选择与用户已确认要求分开列出，审阅方案时可以直接调整。

## 2. 环境和交付依据

2026-10-04 在本机实际核对：

- 最新已安装 SDK：`10.0.400`。
- 最新已安装 `Microsoft.NETCore.App` 和 `Microsoft.WindowsDesktop.App`：`10.0.11`。
- `C:\Projects\englishbench\EnglishBench\EnglishBench.csproj` 使用 `net10.0-windows` 和 `RuntimeFrameworkVersion=10.0.11`。
- englishbench 的 `docs/Getting-Started.md` 规定框架依赖发布，四个运行文件放在 `artifacts/app/`；先发布至 `artifacts/app-update/`，验证后更新运行文件并保留设置。

ReciteWords 使用同样的发布方式，不做安装包、不做自包含发布、不合成单个 EXE。`global.json` 选择 SDK `10.0.400`，`rollForward=latestPatch`。运行时允许兼容的后续 .NET 10 补丁，构建时不启用预览语言功能。

环境限制：`dotnet --info` 的工作负载枚举报告服务管理器访问被拒绝，但 `--list-sdks`、`--list-runtimes` 成功。当前只验证了版本，尚未验证 WPF 构建、音频解码或实际窗口行为；这些属于实施验收。

## 3. 模块与文件组织

```text
ReciteWords/
  App.xaml / App.xaml.cs             启动、对象组装、退出
  ReciteWords.csproj
  Models/
    WordList.cs / Word.cs / WordSense.cs
    StudyLevel.cs / ReviewFilter.cs
    ReviewProgress.cs / WordProgress.cs / AppSettings.cs
  Storage/
    IWordListRepository.cs / WordListRepository.cs
    IProgressRepository.cs / ProgressRepository.cs
    ISettingsRepository.cs / SettingsRepository.cs
    JsonFileWriter.cs               同目录临时文件与替换写入
  Review/
    IReviewSession.cs / ReviewSession.cs
  Audio/
    IAudioCatalog.cs / AudioCatalog.cs
    IAudioPlayer.cs / WpfAudioPlayer.cs
  ViewModels/
    MainViewModel.cs
    SimpleCommand.cs                非泛型 ICommand 实现
  Views/
    MainWindow.xaml / MainWindow.xaml.cs
    DefinitionView.xaml / DefinitionView.xaml.cs
  Platform/
    WindowBounds.cs                 屏幕工作区、DPI、窗口约束
  Resources/
    Styles.xaml                     集中维护颜色、间距、字体和控件样式
    Icons/                          产品图标与英/美音图标
ReciteWords.Tests/
  ReciteWords.Tests.csproj
  Program.cs / TestAssert.cs
  WordListTests.cs / ProgressTests.cs / ReviewSessionTests.cs
  AudioCatalogTests.cs / MainViewModelTests.cs
  Fixtures/                         自包含的小词库与测试音频
docs/
  superpowers/plans/2026-10-04-recitewords-implementation-plan.md
  Getting-Started.md
README.md
.gitignore
global.json
```

只在现有边界创建接口，不给每个数据类、控件或辅助方法创建接口。`MainViewModel` 管理当前选择、按钮状态及操作顺序；不承担 JSON 解析、复习筛选算法或音频解码。`DefinitionView` 只生成视觉内容和例句音频按钮，不读文件、不更新学习记录。

各项目的必要平台 API 放在 `WindowBounds.cs`；不把 Windows 互操作细节传播到其他模块。方法拆分优先表达职责，允许少量重复，不创建通用事件总线或插件结构。

## 4. 输入格式与本地音频

### 单词本

读取直属 `*.json`，不递归；逐文件校验，错误文件不妨碍其他有效词库加载。保持原单词顺序，不排序单词，不修改源文件。识别文件内容而不是将所有 JSON 都当成词库。

- 完整词库继续使用根对象内的非空 `words` 数组作为结构容器，每个单词使用非空 `senses` 数组承载义项；这两个数组是容纳必选内容所需的结构，不增加其他必选内容字段。根元数据 `schema_version`、`name`、`description` 均可省略；缺少或空白 name 时用单词本文件名去掉扩展名显示，缺少 description 时使用空字符串。
- 提供 schema_version 时接受任意正整数，不限定为 `1`；版本号只作来源标记，所有版本均按相同字段结构校验。必须是 JSON 整数数字，不接受字符串、布尔值、null、0、负数、小数或指数写法。不增加 Int32/Int64 范围限制：加载器以原始数字文本校验，并用标准库 `System.Numerics.BigInteger?` 保存 `WordList.SchemaVersion`，省略时为 null；只读导入不需要自定义 JSON 转换器。
- 单词：`wid`、`word`、`phonetic` 必须存在且为字符串；wid、word 非空且唯一，phonetic 允许空字符串但不能省略或为 null；`notes`、`etymology` 可省略；`senses` 非空。
- 每个义项：`chinese_meaning` 必须存在且为非空字符串；`pos`、`english_meaning`、`eid`、`example`、`example_translation` 均为可选字符串；`register`、`synonyms`、`antonyms`、`collocations` 均为可选字符串数组。
- `register` 表示该义项的语域或使用标签，例如 `formal`；保留用户提供的标签和顺序，不限定标签集合、不自动翻译。`antonyms` 为该义项的反义词；`etymology` 为整个单词的词源说明，不放入单个 sense。
- 缺少可选字符串字段时默认为空字符串，缺少可选数组字段时默认为空数组；空内容不占用展示区域。字段存在但类型错误时拒绝该词库，不静默忽略或转换；可选表示可省略，不表示可以使用 null 或错误类型。
- eid 可独立省略，即使存在 example 也不强制提供。eid 省略或为空时，仍显示例句及译文，但不关联例句音频；提供非空 eid 时须为六位数字字符串，保留前导零，在该词库内唯一。只有 eid 而没有例句也可导入，但不显示例句播放按钮。
- 字段名按大小写精确匹配，在根对象、单词对象、义项对象分别只读取该层级的已知字段；未知字段可以是任意合法 JSON 值，直接跳过，不报未知字段错误，也不存入扩展字段容器。已知字段放错层级或大小写不匹配时，在该位置视为未知字段；如果因此缺失必选字段，仍按缺少必选字段报错。同一对象重复键继续报错。
- 不兼容旧 `id` 字段结构；不接受把 null、数字或对象自动转换成字符串。
- 输入字符串按现有程序规则裁剪首尾空白。拼写重复判断需覆盖 Python `casefold` 与 .NET 比较可能不同的 Unicode 输入：不得静默加载歧义词库；发现这种输入时明确报错或实现经测试的等价规则。

已知字段如下；未列出的字段允许存在，但不导入其内容：

| 层级 | 允许的字段 |
|---|---|
| 词库根对象 | schema_version、name、description、words |
| 单词对象 | wid、word、phonetic、senses、notes、etymology |
| senses 中的义项对象 | pos、english_meaning、chinese_meaning、register、eid、example、example_translation、synonyms、antonyms、collocations |

用户提供的对象是 `words` 数组中的一个单词条目；完整文件使用 words 容器，根元数据可省略。使用严格 JSON，不允许注释、尾随逗号或重复键；例如 collocations 数组是义项最后一个字段时，其结束方括号后不能加逗号。最小合法完整词库示例：

```json
{
  "words": [
    {
      "wid": "cognizant",
      "word": "cognizant",
      "phonetic": "/ˈkɒɡnɪzənt/",
      "senses": [
        { "chinese_meaning": "意识到的；了解的" }
      ]
    }
  ]
}
```

新增字段在普通 C# 模型中直接定义：`WordSense.Register` / `WordSense.Antonyms` 为 `List<string>`，`Word.Etymology` 为 `string`，分别对应 JSON 名 `register`、`antonyms`、`etymology`。不增加额外扩展字段容器或语域分类模块。

单词本的任意正整数版本规则不改变 `audio.json`、`examples.json`、ReciteWords 学习记录及程序设置的版本约定；这些文件仍按各自已定义格式校验。

### 音频

资源目录为 `<词库文件所在目录>/<词库文件名去掉扩展名>/`。

- `audio.json`：版本 `1`，按拼写匹配 `words` 项，含 `uk` / `us` 路径数组；选择数组中第一个实际存在的文件。
- `examples.json`：版本 `1`，`words` 下各数组条目含 `eid` 和 `uk` / `us` 路径；只按 `eid` 精确匹配，不按句子文本或顺序猜测。
- 相对路径以资源目录为基准，允许空格及中文路径。
- 配置或文件缺失时保留普通复习能力并禁用对应按钮；配置损坏显示明确警告。
- 单个 MediaPlayer 负责单词和例句播放；新播放替换旧播放，不叠加。
- 换词先停止旧音频、更新显示、隐藏释义，再排队开始新词英音播放；快速切词时用简单递增请求编号丢弃过期播放请求。
- 显示当前词、上一词、下一词、评价后的新词、切换词库恢复的词及点击复习显示的词，都触发一次自动英音。只打开释义或显示错误不触发重播。
- 无词、空筛选或复习完成时不自动播放；切换到这些状态停止旧音频。
- 本地播放错误显示简短提示，不改变学习状态，也不偷偷选其他音频。

## 5. 评价与复习规则

| 中文名称 | 英文状态值 | 含义 |
|---|---|---|
| 不懂 | Unknown | 当前不知道或不能理解该词 |
| 认识 | Familiar | 当前认识该词，但尚未认为掌握 |
| 掌握 | Mastered | 用户当前认为已经掌握该词 |

状态没有强制晋级关系，每次评价覆盖上次状态。无评价记录显示“未学”。筛选菜单为“全部、未学、不懂、认识、掌握”，内部筛选值为 `All`、`Unseen`、`Unknown`、`Familiar`、`Mastered`；筛选类型与评价类型分别定义。

- 本轮词序固定；分类复习开始时创建 `wid` 快照队列。评价使单词退出该类别，但不重新生成本轮队列。
- “从第 N 个开始”表示所选筛选结果内的一基序号；0 或小于 1 修正为 1，超出数量修正为最后一个；非整数输入显示提示并恢复有效值。
- 切换筛选只更新待开始的选择及建议序号，不立即改变当前轮。点击“复习”才创建新队列。
- 建议序号沿用当前词在原词库中的位置作为锚点，取筛选结果中最近的不晚于当前词的位置；没有这样的词则选第一个。
- 上一词、下一词只导航，不更改评价、不增加评价次数；到边界禁用按钮，不绕回。
- `<` / `>` 位于“复习”按钮旁。退回已评价词后可以立即重新评价，覆盖该词原状态；这次评价仍增加评价次数一次，并立即进入下一词。本轮快照包含已评价词，不因其状态变化而阻止退回。
- 点击评价增加该词评价次数一次，保存状态，转入下一词；最后一个评价成功后显示“本轮复习完成”。
- 完成视图中，非空队列的 `<` 返回最后一个词并退出完成视图，允许纠正最后一次评价；`>` 禁用。返回后再次评价最后一个词即重新完成本轮；空队列时两个按钮均禁用。
- 最后一个词可以直接导航到达，只有评价或恢复已完成记录才使本轮完成。
- 成功保存前不显示下一词、不开始下一词音频；失败时恢复操作前的内存记录及当前词，显示可重试错误。
- 结束后不自动开始另一轮、不自动重复不懂词。用户点击“复习”自行开始新轮。

## 6. ReciteWords 独立进度与设置

### 学习记录文件

进度路径固定为 `<单词本所在目录>/userdata/<单词本文件名去掉扩展名>.progress.json`。“单词本名”取文件名而不是 JSON 的 `name` 显示名称。例如选择 `C:\WordLists\education.json`，对应记录为 `C:\WordLists\userdata\education.progress.json`。

```text
C:\WordLists\
  education.json
  science.json
  education\                      单词和例句音频资源
  science\                        单词和例句音频资源
  userdata\
    education.progress.json       education.json 的独立学习记录
    science.progress.json         science.json 的独立学习记录
```

同一目录的单词本共用 `userdata` 文件夹，各自使用独立进度文件；不同目录中的同名词库使用各自目录下的记录。不使用总进度文件，不读取、覆盖或改名 Python 原有的 `<单词本名>/progress.json`。音频目录和格式不变。

修改单词本文件名后，只按新文件名查找对应进度文件：存在就读取，不存在就自动新建。不搜索旧文件、不自动改名、不自动迁移或合并记录，用户自行维护旧记录及对应文件名。

每次打开单词本（包括启动恢复、切换词库、重新打开同一本）执行以下顺序：

1. 成功读取并校验所选单词本，计算其对应进度路径。
2. 查找所在目录下的 `userdata`，不存在时自动创建。
3. 对应进度文件存在时从磁盘读取；不存在时以所有词未评价、全部词的 `wid` 队列、位置 0、未完成状态初始化，并立即写入文件。
4. 将读取或初始化的记录交给 ReviewSession 恢复状态、评价次数、本轮筛选、队列、位置及完成标志，再显示当前词；有效当前词按既定规则自动播放英音。

扫描词库列表时不创建记录；只有实际打开单词本时创建。不用内存中另一词库的进度替代磁盘读取，也不覆盖已经存在的有效记录。创建目录或初始记录失败时显示错误并禁止需要保存进度的操作，允许查看词库；不宣称初始化已成功。

```json
{
  "application": "ReciteWords",
  "schema_version": 1,
  "words": {
    "address": { "level": "Familiar", "review_count": 3 }
  },
  "session": {
    "filter": "All",
    "queue": ["address", "substantial", "controversial"],
    "position": 1,
    "completed": false
  }
}
```

- `words` 的键是单词本 `wid`，不是拼写或数组位置；只存在已经评价过的词条。
- `level` 只接受上述三个英文值；`review_count` 为非负整数。
- `queue` 为本轮 `wid` 数组，`position` 是零基队列位置。非空队列中有效范围为 `0..count-1`；完成后保留最后位置，使用 `completed` 表达完成。空队列位置为 0 且标为完成。
- 队列仅保存唯一、有效的 `wid`；恢复时移除已删除的词，优先保留当前 `wid`，当前词被删除时转入其后第一个保留词，没有后续词则本轮完成。增加的新词不插入已经开始的快照，下一轮才纳入。
- 原学习记录中已删除词的评价条目保留，避免临时移除再加回时丢失历史；不展示在当前词库筛选中。
- 无记录时从全部单词第一词开始。记录损坏、版本不支持、枚举值非法或结构不一致时，不静默重置、不覆盖原文件：显示错误并禁止写入，允许查看词库；用户可修复或另行移走损坏记录后重新加载。
- 不提供“恢复记录”“备份并重置”等入口；损坏文件由用户自行维护，正常的缺失文件初始化不属于损坏记录恢复。
- 不保存释义是否展开，也不保存“已播放”状态；重新打开当前词隐藏释义并自动读英音。
- 写入同目录唯一临时文件，关闭文件后采用替换方式提交；初次写入使用移动。提交失败保留原文件并清理本次临时文件；不使用直接覆盖原文件的写法。

### 程序设置

`recitewords.settings.json` 位于 EXE 同目录，与 englishbench 的设置存放及更新方式一致：保存格式版本、上次词库文件夹、上次词库文件名、窗口位置和尺寸。字号固定为 11pt，不保存或恢复字号。文件夹使用绝对路径，词库使用相对于该文件夹的文件名。

切换文件夹或词库成功后立即保存选择；窗口尺寸、位置在正常退出时保存。设置缺失或损坏时使用默认值并提示，设置写入失败只报告错误，不阻止学习。学习记录保存失败则阻止该次导航或评价提交。

首次默认扫描 EXE 旁边的 `wordlist/`；上次文件夹存在时恢复该文件夹。上次文件夹不存在时提示并回到默认位置；上次词库不存在时选有效列表第一本。没有有效词库时显示空状态和文件夹选择按钮，不伪造示例词。

## 7. 界面和窗口

沿用截图的主要关系，同时适应半屏宽度：

1. 第一行：单词本选择框、文件夹按钮。
2. 第二行：筛选、起始序号、`<` / `>`、复习按钮；两个导航按钮紧邻复习按钮，不换行。筛选下拉框与第一行词库下拉框左边框对齐。
3. 单词行：突出显示拼写，音标和标明 UK/US 的两个发音按钮；右侧释义按钮。
4. 释义区：按义项展示词性、语域标签、中英文释义、双语例句和音频按钮、同义词、反义词、搭配；全部义项之后展示词级 Notes，再展示词源 Etymology。语域标签紧邻对应义项词性；长词源自动换行并可手动滚动查看。新字段均属于释义内容，未点击“释义”前不显示。
5. 底部：等宽“不懂、认识、掌握”按钮；右下角显示当前轮位置/总数及当前评价，保留“未学”状态文字，字号与界面一致。
6. 左下角固定单行信息提示栏，警告或错误显示 5 秒，新提示重新计时；长信息省略显示并可悬停看全文。不提供字号入口，不为成功操作弹窗。

释义使用原生 WPF 文本控件及 ItemsControl，不嵌入 HTML 浏览器，不执行来自 JSON 的标记。所有内容视为普通文本，保持换行及中英文混排。每个义项只有其自身的例句按钮，缺失时禁用，不隐去其释义。

新词显示后释义区为空；点击“释义”一次显示全部义项，再点击不隐藏。允许立即评价。内容自动换行，用户可以手动滚动；不定时滚动、不随音频滚动。换词时复位到顶部。

窗口约束：

- 根据窗口所在显示器查询完整屏幕矩形、工作区矩形及 DPI，统一换算为 WPF DIP。
- 最大外部宽度为该屏幕完整宽度的一半，最大外部高度为该屏幕工作区高度；位置也限制在工作区内。
- 初始宽度取允许的最大宽度，初始高度取工作区高度的 90%；首次放置在工作区内。保存值恢复后重新限制到当前屏幕。
- 正常最小宽度为 590 DIP，保证顶部操作控件不换行且可完整显示。不设置可能大于半屏上限的最小尺寸；当屏幕确实无法容纳时降低最小宽度并允许顶部手动横向滚动，不让控件换行或被裁掉。
- 拖动到另一屏幕、DPI 改变、工作区改变时重新计算；最大化也受相同边界约束，不能通过标题栏或系统菜单变成全屏宽度。
- 必要的 Windows 消息处理和屏幕查询集中放在 WindowBounds；测试覆盖尺寸计算，实际屏幕行为需要人工验收。

## 8. 模块接口

以下为实施时采用的核心边界，不要求引入接口实现以外的抽象层。

| 接口 | 核心方法及职责 |
|---|---|
| `IWordListRepository` | `ScanResult Scan(string folder)`；`WordList Load(string path)`。ScanResult 含有效词库和逐文件错误，不弹窗。 |
| `IProgressRepository` | `ReviewProgress LoadOrCreate(string wordListPath, WordList wordList)`；`void Save(string wordListPath, ReviewProgress progress)`。按单词本文件路径定位其所在目录的 `userdata/<文件名去掉扩展名>.progress.json`；打开时创建缺失目录/初始文件或读取已有记录，保存时写回同一路径。 |
| `ISettingsRepository` | `AppSettings Load()`；`void Save(AppSettings settings)`。路径由启动入口指定。 |
| `IReviewSession` | `void Load(WordList words, ReviewProgress progress)`；`int GetCount(ReviewFilter filter)`；`int SuggestStart(ReviewFilter filter)`；`bool Start(ReviewFilter filter, int number)`；`bool Move(int offset)`；`void Rate(StudyLevel level)`；`ReviewProgress Capture()`；`void Restore(ReviewProgress progress)`。提供 `CurrentWord`、`Position`、`Count`、`Completed`、`CanMovePrevious`、`CanMoveNext` 只读属性。 |
| `IAudioCatalog` | `AudioLoadResult Load(string wordListPath)`；`string? FindWord(string word, string accent)`；`string? FindExample(string eid, string accent)`。accent 只使用 uk/us；返回存在的本地路径或 null。 |
| `IAudioPlayer` | `void Play(string path)`；`void Stop()`；`void Dispose()`；`PlaybackFailed` 事件传递错误信息。实际实例仅在 WPF UI 线程使用。 |

`ScanResult`、`AudioLoadResult` 为普通结果类，分别定义在所属模块；`WordListEntry` 只包含路径和加载结果。不引入通用 Result<T>。

ViewModel 通过普通属性、INotifyPropertyChanged 和非泛型 SimpleCommand 绑定界面。界面事件只处理窗口平台行为、文件夹对话框、文本布局或将操作转给 ViewModel，不复制业务流程。文件夹选择结果调用 `MainViewModel.SelectFolder(string folder)`；打开释义调用 `RevealDefinition()`；播放例句调用 `PlayExample(string eid, string accent)`。

保存事务采用简单深复制：操作前调用 Capture；操作后保存；失败时 Restore。Capture/Restore 必须深复制词条及队列，不能让快照与活动状态共享可变集合。只在持久化成功后刷新新词及触发自动播放。

## 9. 实施任务

### Task 1：项目基础与词库读取

**Files:** global.json、.gitignore、ReciteWords/ReciteWords.csproj、App、Models 中的词库类、Storage/IWordListRepository.cs、WordListRepository.cs；测试项目、TestAssert、WordListTests、Fixtures。

**Interfaces:** 产出第 8 节的词库接口以及 WordList/Word/WordSense 普通数据类；后续任务直接使用，不另行建立转换层。

- [ ] 创建可执行断言：只含四项必选内容及 words/senses 容器的最小词库可加载；缺少 wid、word、phonetic 或任意义项中文释义均拒绝；phonetic 空字符串接受，空 wid/word/中文释义拒绝；合法完整示例保留所有可选内容；eid `001234` 保持字符串；有例句无 eid 可加载但不关联音频；重复 wid、重复拼写、非空重复 eid、错误字段类型均拒绝。
- [ ] 添加用户的 cognizant 条目作为合法完整词库夹具，验证 register 为 formal、antonyms 为 unaware/oblivious、etymology 完整保留；多义项的 register/antonyms 不混用；旧词库缺少三个字段仍加载，字段类型错误则拒绝。
- [ ] 添加版本断言：版本可省略；提供 1、2、2147483648、9223372036854775808 及更长正整数均接受且精确保留；提供 0、负数、小数、指数写法、数字字符串、布尔值、null 均拒绝；不同正整数版本下都执行同样字段校验。
- [ ] 添加已知字段导入断言：根对象、单词及义项中增加未知字符串、数字、布尔值、null、数组或嵌套对象，合法文件仍可加载；模型仅包含已知内容，界面及学习记录不包含未知字段，源文件不被改写。
- [ ] 验证放错层级或大小写不匹配的字段被跳过，但不能替代正确位置的必选字段；已知字段类型错误仍拒绝。每个可选内容字段单独省略仍接受，提供错误类型或 null 拒绝；缺少词库 name 时显示文件名。重复键、注释和尾随逗号仍拒绝。
- [ ] 加入目录扫描断言：无效 JSON 与合法 JSON 共存时只返回有效词库并报告错误；不递归扫描；多义项不拆成多个词。
- [ ] 在加载器未完成时运行测试，确认上述检查失败；实现最小加载与校验代码，然后再次运行。
- [ ] 配置 SDK/运行时，创建最小 WPF 启动入口，加入忽略规则；只提交人工维护源文件和固定测试数据。
- [ ] 验证：`dotnet build ReciteWords -c Release`；`dotnet run --project ReciteWords.Tests -c Release`。要求构建成功、断言全部通过且测试进程退出码为 0。

### Task 2：独立进度、设置及复习逻辑

**Files:** Models 中的学习/会话/设置类、Storage 的进度/设置模块及 JsonFileWriter、Review 模块；ProgressTests.cs、ReviewSessionTests.cs。

**Interfaces:** 消费词库模型；产出第 8 节的 IProgressRepository、ISettingsRepository、IReviewSession。序列化字段及取值严格使用第 6 节。`LoadOrCreate` 使用已校验词库的 `wid` 顺序构造初始队列，不依赖 UI 或音频模块。

- [ ] 写断言：打开 `education.json` 自动创建 `userdata/education.progress.json`；文件名不采用 JSON 的显示名称；初始所有词未学、全部队列、位置 0、未完成；创建后文件可重新读取。扫描但未打开不创建记录。
- [ ] 写断言：已有目录而目标记录不存在时只初始化该文件；已有有效记录不会重置；同目录不同词库及不同目录同名词库的记录相互独立；Python progress.json 内容始终不变。
- [ ] 写断言：三种状态可任意互相改变；评价次数每次增加 1；记录保存/加载后队列、位置和完成标志一致；每次重新打开从磁盘恢复最新记录。
- [ ] 写断言：筛选快照在评价后不重建；起始序号按筛选位置；0 和超大序号修正；导航不评价；边界不绕回；最后一次评价完成；重新开始可创建新队列。
- [ ] 写断言：退回已评价词再次评价后覆盖旧状态、评价次数加 1、立即进入下一词；分类快照仍可退回该词；完成视图可退回最后一词并再次评价，空队列不能导航。
- [ ] 写断言：词库改名后，有对应新名记录则读取，没有则新建；不改名、删除或合并原进度文件；损坏记录保持不变，不提供自动重置操作。
- [ ] 写断言：恢复按 wid，不因重排而错配；移除当前词按照后继规则恢复；添加词不插入旧轮；损坏/不支持记录不被覆盖；深复制快照不受后续修改影响。
- [ ] 写持久化失败断言：制造 userdata 无法创建、初始记录无法写入或已有目标无法替换场景，初始化失败不得宣称成功，写入失败后旧文件字节不变；userdata 内临时文件不被误当词库；设置选项保存/恢复正确。
- [ ] 运行确认失败，按第 5–6 节实现后运行测试，全部断言通过。

### Task 3：本地音频解析和播放边界

**Files:** Audio 全部文件；AudioCatalogTests.cs；测试用微型音频与 JSON。

**Interfaces:** 消费 Word/WordSense 的拼写和 eid，产出 IAudioCatalog、IAudioPlayer；后续界面不访问配置原文或 MediaPlayer。

- [ ] 写断言：单词候选路径选择第一个存在的；中文/空格路径可用；eid `001234` 精确匹配；没有 eid 的旧例句条目不猜测关联。
- [ ] 写断言：uk/us 独立可用；缺失英音但存在美音时自动播放入口得到 null；重复或非法 eid 报错，缺失配置返回空资源。
- [ ] 运行确认失败，实现解析，再运行确认通过；WpfAudioPlayer 在 UI 线程实现，释放事件和资源。
- [ ] 人工验收真实 MP3 播放与切换停止；编码或音频设备错误提示可见，复习仍可操作。

### Task 4：MVVM 操作与界面

**Files:** MainViewModel.cs、SimpleCommand.cs、Views 全部文件、Styles.xaml、图标、App 对象组装；MainViewModelTests.cs。

**Interfaces:** 消费前面六个接口。提供 SelectFolder、RevealDefinition、PlayExample；界面绑定词库列表、筛选、起始序号、当前词、释义可见性、导航/评价命令与警告。字号使用集中样式中的固定值。

- [ ] 使用少量手写测试替身验证：新词显示自动调用 UK 路径播放一次；换词停止旧播放；显示释义不重播；未展开释义也可评价；完成轮次不朗读。
- [ ] 验证：切换筛选不会立即换轮；空类别明确提示；错误词库不会阻止其他词库；每次打开先调用 LoadOrCreate 再恢复会话；进度损坏或初始化失败禁止写入但可查看内容。
- [ ] 验证保存失败：仍显示原词，状态/计数恢复，不播放下一词；选择失败不覆盖上次成功设置。
- [ ] 验证快速切词只执行最新待播放请求；缺少英音不会调用美音或联网；按钮可用性与真实文件对应。
- [ ] 运行确认失败，实现 ViewModel，确认测试通过；按第 7 节创建实际界面。
- [ ] 人工验收多义项、语域标签、反义词、很长英文、中文注释、长词源、换行、手动滚动和切词复位，确保新增字段仅随释义显示、空字段不留空标题、无水平截断、没有自动滚动。
- [ ] 先用独立的 6–10 词小样本完成端到端验收，包含多义项、仅必选字段、未知字段、缺少英音但有美音、双口音例句、长词源。验证显示与自动英音、展开释义、直接评价、前后导航及重评覆盖、userdata 初始化、保存并重启恢复。只写样本副本，不修改原词库；发现问题先修正再进入正式发布验收。

### Task 5：窗口约束与发布交付

**Files:** Platform/WindowBounds.cs、MainWindow 平台连接、README.md、docs/Getting-Started.md；窗口尺寸计算测试加入测试项目。

**Interfaces:** WindowBounds 提供 `Attach(Window window)`、`ApplySavedBounds(Window window, AppSettings settings)`；窗口约束只影响显示，不改变学习逻辑。

- [ ] 写尺寸断言：1920×1080、工作区1920×1040、DPI96时外部最大宽度为960 DIP、高度1040 DIP；DPI144时为640和约693.33 DIP；尺寸恢复后不超过约束。
- [ ] 实现屏幕与 DPI 查询、移动/工作区变化处理及最大化约束；检查固定最小尺寸不会突破最大值。
- [ ] 人工验收：100%/150%缩放、多屏切换（有设备时）、任务栏不同位置、最大化/系统菜单、旧尺寸恢复；缺少设备的场景明确标为未验证。
- [ ] 在完成窗口约束后再次使用上述小样本确认半屏尺寸下的长内容及手动滚动，确认小样本验收通过后再进行完整词库和发布验证。
- [ ] 完成 `.gitignore` 检查：忽略 bin/obj、artifacts、IDE文件、设置、真实 wordlist、个人 userdata 目录和临时文件；固定 Fixtures 明确保留，不全局忽略 JSON/MP3，文件写入测试在临时目录创建 userdata。
- [ ] 执行完整构建和测试，通过后暂存发布并检查真实启动；只在用户审阅方案并进入实施后执行。
- [ ] 保存源代码、测试、运行说明及正式发布目录；核对更新不替换设置或词库。

## 10. Review Focus

以下五类输入或运行条件必须检查，其所属任务已安排对应测试或人工验收：

1. 音频缺失、路径包含中文/空格、配置坏掉：界面仍可背词；绝不偷偷替换口音（Task 3–4）。
2. 快速连续评价或切词：不多计数、不播放旧词、不重复执行同一次点击（Task 4）。
3. 写入失败或进度损坏：旧文件不丢失，评价不被误认为已保存（Task 2、4）。
4. 小屏幕、高 DPI、最大化及跨屏移动：外部尺寸始终满足半屏宽度和工作区高度限制（Task 5）。
5. 复习过程中词库重排/删词、六位 eid 前导零及 Unicode 拼写：不把记录或音频匹配到错误单词（Task 1–3）。

## 11. 发布命令与完成标准

实施时在仓库根目录运行：

```powershell
dotnet build ReciteWords -c Release
dotnet run --project ReciteWords.Tests -c Release
dotnet publish ReciteWords -c Release --self-contained false -p:DebugType=None -p:DebugSymbols=false -o artifacts/app-update
```

测试项目使用普通断言和非零失败退出码，不引入额外测试框架。文件测试只写测试临时目录，不写原 Python 词库。原目录可以作为只读兼容输入；真实复习和写入验收使用复制的词库。

正式运行目录 `artifacts/app/` 保持以下四个文件在一起：

- `ReciteWords.exe`
- `ReciteWords.dll`
- `ReciteWords.deps.json`
- `ReciteWords.runtimeconfig.json`

可选 `recitewords.settings.json` 由程序建立，更新时保留；用户词库和音频独立放置，不捆绑个人内容。发布更新前关闭程序，暂存发布验证成功后只更新四个运行文件。

完成必须有：构建成功、业务及文件测试通过、实际启动和本地音频播放验证、真实词库内容兼容检查、半屏窗口与长释义人工验收、发布后启动验证。无法验证的多屏或特定音频格式单列说明，不以编译成功代替界面或声音验收。

## 12. 本轮结果与审阅点

方案阶段已结束，用户已授权并进入实施。已建立产品、测试、gitignore、文档及本地发布目录；没有 Git 提交或 GitHub 上传。实际验证结果单独记录在 `docs/Implementation-Report.md`。

方案已包含逐词库 userdata 进度、可选扩展字段、未知字段忽略、前后导航和重评覆盖；用户已确认不提供损坏记录恢复入口、不自动维护改名记录、先做小样本验收，并允许手动滚动。实施在当前对话进行；依据 executing-plans 技能进行了一次独立只读代码审查，没有创建新对话或远程仓库，没有上传 GitHub。
