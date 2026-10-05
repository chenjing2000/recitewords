using ReciteWords.Models;
using System.Numerics;
using System.Text.Json;
using ReciteWords.Common;
namespace ReciteWords.Storage;

public class WordListRepository : IWordListRepository
{
    public ScanResult Scan(string folder)
    {
        var result = new ScanResult();
        if (!Directory.Exists(folder)) return result;
        string[] paths = Directory.GetFiles(folder, "*.json");
        Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
        foreach (string path in paths)
        {
            try { result.Entries.Add(new WordListEntry { Path = path, Name = Load(path).Name }); }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is JsonException)
            { result.Errors.Add(Path.GetFileName(path) + ": " + ex.Message); }
        }
        return result;
    }
    public WordList Load(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        JsonFields.Object(root);
        var list = new WordList { Name = JsonFields.Text(root, "name"), Description = JsonFields.Text(root, "description") };
        if (list.Name.Length == 0) list.Name = Path.GetFileNameWithoutExtension(path);
        if (root.TryGetProperty("schema_version", out var version))
        {
            string text = version.GetRawText();
            if (version.ValueKind != JsonValueKind.Number || !JsonFields.Digits(text) || !BigInteger.TryParse(text, out var number) || number <= 0)
                throw new InvalidDataException("schema_version 必须为正整数");
            list.SchemaVersion = number;
        }
        var spellings = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in JsonFields.Array(root, "words", true))
        {
            JsonFields.Object(item);
            var word = new Word
            {
                Text = JsonFields.Text(item, "word", true),
                phonetic_uk = JsonFields.Text(item, "phonetic_uk", true, true),
                phonetic_us = JsonFields.Text(item, "phonetic_us", true, true),
                Notes = JsonFields.Text(item, "notes"),
                Etymology = JsonFields.Text(item, "etymology")
            };
            if (!spellings.Add(Spelling.Fold(word.Text))) throw new InvalidDataException("重复拼写: " + word.Text);
            int exampleNumber = 0;
            foreach (var senseItem in JsonFields.Array(item, "senses", true))
            {
                JsonFields.Object(senseItem);
                var sense = new WordSense
                {
                    Pos = JsonFields.Text(senseItem, "pos", true),
                    EnglishMeaning = JsonFields.Text(senseItem, "english_meaning"),
                    ChineseMeaning = JsonFields.Text(senseItem, "chinese_meaning", true),
                    Register = JsonFields.Strings(senseItem, "register"),
                    Eid = JsonFields.Text(senseItem, "eid"),
                    Example = JsonFields.Text(senseItem, "example"),
                    ExampleTranslation = JsonFields.Text(senseItem, "example_translation"),
                    Synonyms = JsonFields.Strings(senseItem, "synonyms"),
                    Antonyms = JsonFields.Strings(senseItem, "antonyms"),
                    Collocations = JsonFields.Strings(senseItem, "collocations")
                };
                if (sense.Example.Length > 0) exampleNumber++;
                if (exampleNumber > 99) throw new InvalidDataException(word.Text + " 的例句不能超过 99 个");
                if (senseItem.TryGetProperty("eid", out _) &&
                    (sense.Example.Length == 0 || sense.Eid.Length != 2 || !JsonFields.Digits(sense.Eid) || sense.Eid != exampleNumber.ToString("D2")))
                    throw new InvalidDataException(word.Text + " 的 eid 必须按例句顺序从 01 开始，当前应为 " + exampleNumber.ToString("D2"));
                word.Senses.Add(sense);
            }
            list.Words.Add(word);
        }
        return list;
    }
}
