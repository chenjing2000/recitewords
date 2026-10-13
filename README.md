# ReciteWords

一个使用 C#、WPF 和简单 MVVM 编写的单词背诵软件。使用 JSON 词库及已有本地音频，由用户自行安排复习。

## 运行

本机需要 x64 版本的 .NET 10 Windows Desktop Runtime 10.0.11 或兼容的后续补丁；不需要 SDK。发布为不包含运行时的单文件 EXE，缺少运行时时由 .NET 启动宿主提示安装。

```text
dist/
  ReciteWords.exe
  ReciteWords.settings.json  # 首次运行自动创建
```

双击 `ReciteWords.exe`，点击“文件夹”选择单词本所在目录。首次默认读取程序旁的 `wordlist/`，以后恢复上次选择。单词本与音频由用户独立准备。个人词库、音频、设置及学习记录不纳入 Git。

## 使用

- 底部筛选“全部、未学、不懂、认识、掌握”，选择后从该分类第一个词开始；各选项显示实时数量。
- 新词显示时隐藏释义，并自动播放已有英音；缺失英音时保持静音，不使用美音替代。
- 点击“释义”显示全部义项，再次点击清空内容区。可以手动滚动，也可以不查看释义直接评价。
- 点击“不懂、认识、掌握”保存并进入下一词；`‹`、`›` 只导航，不评价。
- 退回已评价词后再次评价，只覆盖当前状态，不记录评价次数。完成后 `‹` 可返回最后一个词纠正评价。
- UK/US 按钮分别播放单词及例句本地音频；缺失时禁用。软件不联网下载、不合成语音。

## 数据

每本词库采用含 `words` 数组的 JSON 对象。每词必选 `word`、`phonetic_uk`、`phonetic_us` 和非空 `senses`，每个义项必选非空 `pos`、`chinese_meaning`。两个音标字段允许空字符串。其他已知字段可省略，未知字段允许存在但不导入；不接受注释、尾随逗号和重复键。

义项支持词性、中英文释义、语域标签、例句及译文、同义词、反义词、搭配；单词支持 Notes、Etymology。`schema_version` 可省略，提供时为任意正整数。顶部下拉框始终显示不带扩展名的实际文件名，`name` 字段不参与显示。

本次为破坏性升级：删除 `wid`，用标准化后的 `word` 关联进度，不兼容旧 `phonetic`；未知字段仍忽略，不能代替必填字段。完整规则见 [单词本格式 skill](docs/skills/recitewords-wordlist-format/SKILL.md)。

```text
WordLists/
  education.json
  education/
    audio/loyalty_uk.mp3
    audio/loyalty_us.mp3
    audio_sentences/loyalty_e01_uk.mp3
    audio_sentences/loyalty_e01_us.mp3
  userdata/
    education.progress.json
```

每次打开时读取对应 `userdata/<文件名去掉扩展名>.progress.json`；缺少目录或文件则自动初始化。记录使用去除首尾空白并经过 Unicode casefold 的 word，不依赖单词数组位置。Python 的旧记录不会被读取或覆盖。

损坏记录仅提示错误并禁止提交进度，用户自行修复或移走损坏文件；没有自动恢复或重置入口。词库改名后按新名称查找或新建记录，不迁移旧文件。

进度只接收版本 2，删除 `review_count`、`position` 和持久化会话，只保存单词当前状态；重新打开从全部分类第一词建立新队列。旧进度读取失败，程序不迁移或覆盖。见 [学习进度格式](docs/Progress-Format.md)。

音频主体 stem 由 word 去除首尾空白、按 invariant-culture 转小写后，把每个非 ASCII `a-z`/`0-9` 字符替换为 `_` 得到；合并连续下划线并去掉首尾下划线。同一 stem 按单词本顺序仅允许首词使用音频，后续词条仍可学习，但禁用全部单词与例句音频；首词文件不存在也不释放资格。显示和进度键不变。

单词音频直接读取同名子目录 `audio/` 中的 `{stem}_uk.mp3/.wav` 和 `{stem}_us.mp3/.wav`，MP3 存在时优先，MP3 缺失才选 WAV，解码失败不回退，不再读取 audio.json。例句直接读取 audio_sentences/ 下的 `{stem}_e{eid}_uk/us.mp3/.wav`，不读取 examples.json。eid 是每个单词内从 01 开始的两位序号，不要求跨单词唯一。

## 构建与测试

本机验证使用 SDK 10.0.400，不需要第三方 NuGet 包。

```powershell
dotnet build ReciteWords -c Release
.\tools\Test.ps1 -Suite Core
.\tools\Test.ps1 -Suite UI
.\tools\Test.ps1 -Suite Full
```

测试使用 Windows 临时目录中的独立词库，完成后自动删除。UI 和 Full 另外创建真实 WPF 窗口，检查布局、焦点、快捷键、滚动及本地 WAV 解码与播放结束（静音）；不生成截图或报告文件，结果仅输出到终端，不会修改原词库。播放检查不能代替真实发音试听。脚本检查所有 .ps1 为 English ASCII；详细入口及串行验收规则见 [测试说明](docs/Testing.md)。

## 发布

发布为 Windows x64 单文件程序，不捆绑 .NET 运行时。以下命令从仓库根目录执行，输出到 dist：

```powershell
.\tools\Publish.ps1
```

更新前关闭运行中的软件；发布只更新 EXE，保留 `<主程序名>.settings.json`、词库和 userdata。当前配置文件为 `dist/ReciteWords.settings.json`，首次启动时自动创建；EXE 改名后使用相应的新配置文件名。详细说明见 [运行与交付](docs/Getting-Started.md)。

## 模块

`Common` 仅保存共享 JSON 校验及拼写比较；`Models` 保存数据；`Storage` 处理文件；`Review` 负责复习队列和评价；`Audio` 查找与播放本地文件；`ViewModels` 编排操作与绑定；`Views` 绘制界面；`Platform` 管理显示器与窗口约束。启动时手工组装，模块通过明确的小接口协作，不使用数据库或依赖注入容器。

Unicode 拼写匹配采用随程序嵌入的 Unicode 15.1 casefold 数据，保持 Python 格式的大小写比较语义。`tools/generate_case_folding.py` 仅是开发时的数据生成工具；发布程序不依赖 Python。

本轮审查、整改理由和验证范围见 [代码审查报告](docs/Code-Review-2026-10-13.md)；[此前审查](docs/Code-Review.md) 保留为历史记录。

当前音频与界面规则见 [音频文件规范](docs/Audio-Format.md)。

设计目的、模块功能、新格式规范及其他说明见 [文档索引](docs/README.md)。

## 统一发布与本次体检

发布入口统一为 `.\tools\Publish.ps1`，参数统一保存在主项目的 `Properties/PublishProfiles/Portable.pubxml`。详见[统一发布流程](docs/Publishing.md)、[2026-10-13 体检报告](docs/Health-Check-2026-10-13.md)和[发布说明](docs/Release-Notes.md)。


发布后运行 `.\tools\Test-PublishedApp.ps1` 验证独立目录启动、配置位置和运行时缺失提示；具体流程见 [测试说明](docs/Testing.md)。

音频统一协议不兼容旧目录、旧命名和索引，不自动迁移资源。空 stem 保留词条但禁用全部音频。旧 JSON 音频路径读取时忽略，外部工具保存时删除；程序不重写源词库。
