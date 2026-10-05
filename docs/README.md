# 文档索引

| 文档 | 用途 |
|---|---|
| [单词本格式 skill](skills/recitewords-wordlist-format/SKILL.md) | 已确认的新字段规范、必填与可选字段、双音标、生成规则及完整 JSON 示例 |
| [设计目的与范围](Design-Purpose.md) | 程序解决的问题、使用流程、设计原则和实现状态 |
| [模块职责与数据流](Module-Design.md) | 模块功能、接口边界、打开与保存流程、文件归属和维护位置 |
| [学习进度格式](Progress-Format.md) | 新方案只保存当前状态，取消次数和上次复习位置 |
| [音频文件规范](Audio-Format.md) | 首次 stem 优先、两位 eid、直接例句文件名和最新界面规则 |
| [文件格式升级结果](Format-Upgrade.md) | 破坏性升级内容、不兼容边界、skill 同步与验证证据 |
| [运行与交付](Getting-Started.md) | 当前程序的运行、学习、界面、构建及发布方式 |
| [代码审查与整改](Code-Review.md) | 上轮审查结果、修复理由、验证与清理范围 |
| [实施结果](Implementation-Report.md) | 上轮交付与验证记录 |
| [原始实施方案](superpowers/plans/2026-10-04-recitewords-implementation-plan.md) | 历史设计依据，部分字段与界面已被后续决定替代 |

## 当前格式与升级边界

当前程序已删除 wid，要求双音标和每个词义的 pos；进度采用版本 2，只保存当前状态，不保存次数和会话位置。重新打开从全部分类第一词开始。

单词音频直接按 `{stem}_uk.mp3/.wav`、`{stem}_us.mp3/.wav` 查找，不读取 audio.json；例句按两位局部 eid 直接查找 examples/ 下的 stem_e{eid}_uk/us 文件，也不读取 examples.json。旧单音标词库和版本 1 进度不兼容，不自动迁移或覆盖。

skill 作为项目文档保存在本目录，可把内容提供给 LLM，或明确要求读取文件再生成；未自动安装到本机 skill 搜索目录。

原有 `C:/pywork/words_review/skills/ielts-wordlist-generator/SKILL.md` 也已同步新字段和音频命名规则。

音频主体 stem 的字符替换、首次 stem 优先与词组示例见格式 skill；这项转换只影响单词音频文件名。
