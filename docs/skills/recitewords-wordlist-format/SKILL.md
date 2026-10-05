---
name: recitewords-wordlist-format
description: Use when generating, editing, or checking ReciteWords vocabulary JSON files, including required sense fields and separate British and American IPA phonetics.
---

# ReciteWords 单词本格式

用于生成、修改或检查 ReciteWords 单词本。按下述字段生成 UTF-8 JSON；不生成学习进度、音频文件或音频索引，除非用户另外要求。

**当前程序采用本规范，属于破坏性升级：不兼容旧单音标词库和版本 1 进度，不自动迁移旧文件。**

## 单词本顶层

文件根节点为对象，单个单词放入 `words` 数组，不以单词对象或数组直接作为文件根节点。

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| `words` | 对象数组 | 是 | 至少包含一个单词 |
| `name` | 字符串 | 否 | 单词本显示名称；省略或为空时采用文件名（不含扩展名） |
| `description` | 字符串 | 否 | 单词本说明 |
| `schema_version` | 整数 | 否 | 任意正整数；不限定为 1，也不表示自动启用旧格式兼容 |

`schema_version` 使用整数写法，例如 `1`、`2`，不用 `1.0`、`1e0`、零、负数或字符串。

## 单词对象

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| `word` | 字符串 | 是 | 非空，单词或短语的原始文本；按 Unicode casefold 比较后在单词本内唯一，同时用于关联学习进度 |
| `phonetic_uk` | 字符串 | 是 | 英式 IPA 音标，允许空字符串 |
| `phonetic_us` | 字符串 | 是 | 美式 IPA 音标，允许空字符串 |
| `senses` | 对象数组 | 是 | 至少包含一个词义，每个词义遵守下一节 |
| `notes` | 字符串 | 否 | 实用用法提示 |
| `etymology` | 字符串 | 否 | 词源说明 |

不生成 `wid`。进度关联键采用去除首尾空白后、经过 Unicode casefold 的 `word`，复用现有拼写比较方式；显示仍保留原始大小写。不再建立独立单词 ID，同一个词的多个词义放在同一个 `senses` 数组中。

重新排序或修改释义、音标、例句不会改变该键；仅调整大小写也不会改变该键。修改实际拼写会使其成为另一个词，原进度不会自动关联。旧进度不读取、不自动迁移或覆盖。新进度只保存单词当前状态，不保存次数、队列或位置；具体结构见 [学习进度格式](../../Progress-Format.md)。

### 双音标

分别根据英式和美式读音生成 `phonetic_uk`、`phonetic_us`，采用 `/…/` 包围的 IPA。两种读音相同也分别填写两个字段。

无法可靠确定某种音标时，该字段填写 `""`，不猜测、不把另一口音的音标直接当作该口音。核对音标与已有对应音频的一致性需要能实际检查音频；没有音频时不要声称已经核对。

不生成旧 `phonetic` 字段，也不生成 `phonetic: { "uk": …, "us": … }`。此次为破坏性升级，没有旧字段回退或自动迁移。

目标界面顺序为“英音音标 → 英音喇叭 → 美音音标 → 美音喇叭”，整组居中。音标为空显示 `—`；缺少本地音频禁用对应喇叭，音标是否存在不决定音频是否可播放。单词出现时默认自动播放英音，不用美音替代缺失的英音。

## 每个词义对象

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| `pos` | 字符串 | 是 | 非空词性，例如 `n.`、`v.`、`adj.`、`adv.` |
| `chinese_meaning` | 字符串 | 是 | 非空中文释义 |
| `english_meaning` | 字符串 | 否 | 英文释义 |
| `register` | 字符串数组 | 否 | 语域标签，例如 `["formal"]` |
| `eid` | 字符串 | 否 | 例句标识，提供时为六位 ASCII 数字，在整个单词本内唯一；保留前导零 |
| `example` | 字符串 | 否 | 英文例句 |
| `example_translation` | 字符串 | 否 | 例句中文译文 |
| `synonyms` | 字符串数组 | 否 | 同义词 |
| `antonyms` | 字符串数组 | 否 | 反义词 |
| `collocations` | 字符串数组 | 否 | 搭配 |

每个词义最多使用一组 `eid`、`example`、`example_translation`，不增加 `examples` 数组。没有 `eid` 的例句可以显示，但不能关联例句音频。没有例句时通常省略 `eid`。

可选字段可直接省略；可选文本除 `eid` 外可为 `""`，字符串数组可为 `[]`。不用 `null` 替代字符串或数组。词性没有限定枚举，但不能遗漏或仅含空白。

## 本地音频命名与读取

单词本 `education.json` 的单词音频目录为同级 `education/audio/`，按单词文本命名：`{stem}_uk.mp3`、`{stem}_us.mp3`，也支持同名 `.wav`。同一口音两种格式都存在时优先 `.mp3`。例如 `loyalty_uk.mp3`、`loyalty_us.mp3`，不使用 `wid`。

文件名主体 `stem` 的生成规则：先去除 `word` 首尾空白，再逐个将 `[^0-9a-zA-Z]` 匹配的字符替换为英文下划线 `_`。不合并连续下划线，不删除首尾下划线，不改变字母大小写；只转换主体，不转换目录分隔符、口音后缀或扩展名。

例如 `take care of` → `take_care_of`，`one's own` → `one_s_own`，`well-being` → `well_being`，`a / b` → `a___b`，`café` → `caf_`。对应文件为 `one_s_own_uk.mp3`、`one_s_own_us.mp3`（或同名 WAV）。准备音频文件和程序查找音频必须使用相同转换规则。

导入时按不区分大小写的主体检查文件名冲突。例如 `well-being` 与 `well being` 都变成 `well_being`，整个单词本会被拒绝，并提示两词及冲突主体；不自动追加编号。生成单词本时也检查该冲突。

转换只影响单词音频文件名，word 显示文本及基于 Unicode casefold 的进度键不变。程序不重命名已有音频，也不回退查找带空格或标点的旧文件名；由用户或音频准备工具按新规则命名。例句 eid 和 examples.json 指定路径不执行此替换。

程序直接查找这些文件，不再读取旧 `audio.json`，不从索引指定的任意路径回退。缺失时禁用对应喇叭，不联网下载、不合成；自动发音仅用英音。音标字符串不作为音频文件名。

例句仍用可选 `eid` 精确关联同级 `education/examples.json`，保留该索引版本 1；路径以 `education/` 为基准。此轮不改变例句音频协议，也不要求生成单词本时生成音频索引。

## 有效完整示例

```json
{
  "schema_version": 2,
  "name": "Vocabulary Sample",
  "description": "双音标单词本示例",
  "words": [
    {
      "word": "loyalty",
      "phonetic_uk": "/ˈlɔɪəlti/",
      "phonetic_us": "/ˈlɔɪəlti/",
      "senses": [
        {
          "pos": "n.",
          "chinese_meaning": "忠诚；忠实",
          "english_meaning": "the quality of being faithful to a person, group, or cause",
          "register": [],
          "eid": "000001",
          "example": "Her loyalty to the team never changed.",
          "example_translation": "她对团队的忠诚从未改变。",
          "synonyms": ["faithfulness", "devotion"],
          "antonyms": ["disloyalty"],
          "collocations": ["loyalty to", "customer loyalty"]
        }
      ],
      "notes": "常与 to 连用，表示对某人、组织或事业的忠诚。",
      "etymology": "由 loyal 加名词后缀 -ty 构成。"
    }
  ]
}
```

最小文件只需顶层 `words`；每个单词保留 `word`、`phonetic_uk`、`phonetic_us`、`senses`，每个词义保留 `pos`、`chinese_meaning`。

## 生成与检查

1. 按用户提供的词汇范围生成内容，检查单词拼写唯一性及转换后的音频文件名冲突；保留已有例句 `eid`，需要新例句 ID 时才分配并检查唯一性。
2. 为每个单词输出两个音标字段，为每个词义输出非空 `pos` 和 `chinese_meaning`。
3. 只生成上面列出的字段。导入器会忽略未知字段的内容，但已知字段存在时必须符合类型和必填规则。
4. 检查 JSON 能严格解析：双引号、无注释、无尾随逗号、无重复键、无无效 Unicode；字段名大小写严格按表使用。
5. 用户要求 JSON 数据文件时输出纯 JSON，不把说明文字或 Markdown 围栏写入文件。没有实际执行导入时，不声称已通过程序验收。

常见错误：把词性写在单词层面；只提供一种音标；以旧 `phonetic` 代替两个新字段；把数组写成字符串；把 `eid` 写成数字丢失前导零；把学习状态、音频路径或其他自定义字段混入生成的单词对象。
