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
        var wids = new HashSet<string>(StringComparer.Ordinal);
        var spellings = new HashSet<string>(StringComparer.Ordinal);
        var eids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in JsonFields.Array(root, "words", true))
        {
            JsonFields.Object(item);
            var word = new Word
            {
                Wid = JsonFields.Text(item, "wid", true),
                Text = JsonFields.Text(item, "word", true),
                Phonetic = JsonFields.Text(item, "phonetic", true, true),
                Notes = JsonFields.Text(item, "notes"),
                Etymology = JsonFields.Text(item, "etymology")
            };
            if (!wids.Add(word.Wid)) throw new InvalidDataException("重复 wid: " + word.Wid);
            if (!spellings.Add(Spelling.Fold(word.Text))) throw new InvalidDataException("重复拼写: " + word.Text);
            foreach (var senseItem in JsonFields.Array(item, "senses", true))
            {
                JsonFields.Object(senseItem);
                var sense = new WordSense
                {
                    Pos = JsonFields.Text(senseItem, "pos"),
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
                if (sense.Eid.Length != 0 && (sense.Eid.Length != 6 || !JsonFields.Digits(sense.Eid) || !eids.Add(sense.Eid)))
                    throw new InvalidDataException("eid 必须是唯一的六位数字字符串");
                word.Senses.Add(sense);
            }
            list.Words.Add(word);
        }
        return list;
    }
}
