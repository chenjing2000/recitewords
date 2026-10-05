# ReciteWords

一个使用 C#、WPF 和简单 MVVM 编写的单词背诵软件。使用 JSON 词库及已有本地音频，由用户自行安排复习。

## 运行

本机需要 .NET 10 Windows Desktop Runtime 10.0.11 或兼容的后续补丁。发布目录中的四个文件须放在一起：

```text
artifacts/app/
  ReciteWords.exe
  ReciteWords.dll
  ReciteWords.deps.json
  ReciteWords.runtimeconfig.json
```

双击 `ReciteWords.exe`，点击“文件夹”选择单词本所在目录。首次默认读取程序旁的 `wordlist/`，以后恢复上次选择。随本地发布附有六词小样本；示例不包含真实发音文件，发音按钮会禁用。个人词库、音频、设置及学习记录不纳入 Git。

## 使用

- 底部筛选“全部、未学、不懂、认识、掌握”，选择后从该分类第一个词开始；各选项显示实时数量。
- 新词显示时隐藏释义，并自动播放已有英音；缺失英音时保持静音，不使用美音替代。
- 点击“释义”显示全部义项。可以手动滚动，也可以不查看释义直接评价。
- 点击“不懂、认识、掌握”保存并进入下一词；`‹`、`›` 只导航，不评价。
- 退回已评价词后再次评价，只覆盖当前状态，不记录评价次数。完成后 `‹` 可返回最后一个词纠正评价。
- UK/US 按钮分别播放单词及例句本地音频；缺失时禁用。软件不联网下载、不合成语音。

## 数据

每本词库采用含 `words` 数组的 JSON 对象。每词必选 `word`、`phonetic_uk`、`phonetic_us` 和非空 `senses`，每个义项必选非空 `pos`、`chinese_meaning`。两个音标字段允许空字符串。其他已知字段可省略，未知字段允许存在但不导入；不接受注释、尾随逗号和重复键。

义项支持词性、中英文释义、语域标签、例句及译文、同义词、反义词、搭配；单词支持 Notes、Etymology。`schema_version` 可省略，提供时为任意正整数。没有 `name` 时按词库文件名显示。

本次为破坏性升级：删除 `wid`，用标准化后的 `word` 关联进度，不兼容旧 `phonetic`；未知字段仍忽略，不能代替必填字段。完整规则见 [单词本格式 skill](docs/skills/recitewords-wordlist-format/SKILL.md)。

```text
WordLists/
  education.json
  education/
    audio/loyalty_uk.mp3
    audio/loyalty_us.mp3
    examples/loyalty_e01_uk.mp3
    examples/loyalty_e01_us.mp3
  userdata/
    education.progress.json
```

每次打开时读取对应 `userdata/<文件名去掉扩展名>.progress.json`；缺少目录或文件则自动初始化。记录使用去除首尾空白并经过 Unicode casefold 的 word，不依赖单词数组位置。Python 的旧记录不会被读取或覆盖。

损坏记录仅提示错误并禁止提交进度，用户自行修复或移走损坏文件；没有自动恢复或重置入口。词库改名后按新名称查找或新建记录，不迁移旧文件。

进度只接收版本 2，删除 `review_count`、`position` 和持久化会话，只保存单词当前状态；重新打开从全部分类第一词建立新队列。旧进度读取失败，程序不迁移或覆盖。见 [学习进度格式](docs/Progress-Format.md)。

音频主体 stem 由 word 去除首尾空白后，把每个 `[^0-9a-zA-Z]` 字符替换为 `_` 得到；不合并下划线、不转换大小写。同一 stem 按单词本顺序仅允许首词使用音频，后续词条仍可学习，但禁用全部单词与例句音频；首词文件不存在也不释放资格。显示和进度键不变。

单词音频直接读取同名子目录 `audio/` 中的 `{stem}_uk.mp3/.wav` 和 `{stem}_us.mp3/.wav`，MP3 优先，不再读取 audio.json。例句直接读取 examples/ 下的 `{stem}_e{eid}_uk/us.mp3/.wav`，不读取 examples.json。eid 是每个单词内从 01 开始的两位序号，不要求跨单词唯一。

## 构建与测试

本机验证使用 SDK 10.0.400，不需要第三方 NuGet 包。

```powershell
dotnet build ReciteWords -c Release
dotnet run --project ReciteWords.Tests -c Release
dotnet run --project ReciteWords.Tests -c Release -- --ui-qa
```

普通测试使用项目 `artifacts/tests/` 下的独立临时词库。`--ui-qa` 另外创建真实 WPF 窗口并生成 `artifacts/qa/sample-definition.png`，检验本地测试 WAV 的解码和播放结束（静音），不会修改原词库。这个测试验证播放路径，不能代替对真实英美音文件的试听。

## 发布

发布方式与 EnglishBench 相同，依赖已安装运行时，不制作安装包或单文件包。

```powershell
dotnet publish ReciteWords -c Release --self-contained false -p:DebugType=None -p:DebugSymbols=false -o artifacts/app-update
```

关闭运行中的软件，验证暂存发布后，将四个运行文件更新到 `artifacts/app/`。保留 `recitewords.settings.json`、词库和 userdata。详细说明见 [运行与交付](docs/Getting-Started.md)。

## 模块

`Common` 仅保存共享 JSON 校验及拼写比较；`Models` 保存数据；`Storage` 处理文件；`Review` 负责复习队列和评价；`Audio` 查找与播放本地文件；`ViewModels` 编排操作与绑定；`Views` 绘制界面；`Platform` 管理显示器与窗口约束。启动时手工组装，模块通过明确的小接口协作，不使用数据库或依赖注入容器。

Unicode 拼写匹配采用随程序嵌入的 Unicode 15.1 casefold 数据，保持 Python 格式的大小写比较语义。`tools/generate_case_folding.py` 仅是开发时的数据生成工具；发布程序不依赖 Python。

本轮审查、整改理由和验证范围见 [代码审查报告](docs/Code-Review.md)。

当前音频与界面规则见 [音频文件规范](docs/Audio-Format.md)。

设计目的、模块功能、新格式规范及其他说明见 [文档索引](docs/README.md)。
