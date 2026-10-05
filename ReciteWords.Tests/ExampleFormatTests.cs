using ReciteWords.Storage;
using System.Text.Json;
namespace ReciteWords.Tests;

internal static partial class Program
{
    static string ExampleList(params string[] words)
    {
        var items = new List<object>();
        foreach (string word in words)
            items.Add(new { word, phonetic_uk = "", phonetic_us = "", senses = new[] {
                new { pos = "n.", chinese_meaning = "示例", example = "First example.", eid = "01" },
                new { pos = "n.", chinese_meaning = "示例", example = "Second example.", eid = "02" }
            } });
        return JsonSerializer.Serialize(new { words = items });
    }
    static void ExampleFormatTests()
    {
        Check("two-digit eid repeats across words", () => {
            var list = new WordListRepository().Load(Write("two-digit.json", ExampleList("first", "second")));
            Equal("01", list.Words[0].Senses[0].Eid); Equal("01", list.Words[1].Senses[0].Eid);
        });
        Check("legacy six-digit eid rejected", () => Reject(() => new WordListRepository().Load(Write("legacy-eid.json", ExampleList("first").Replace("01", "001234")))));
        foreach (string value in new[] { "00", "1", "001", "03", "" })
            Check("invalid first eid rejected: " + value, () => Reject(() => new WordListRepository().Load(Write("invalid-eid.json", ExampleList("first").Replace("\"01\"", "\"" + value + "\"")))));
        Check("duplicate eid within word rejected", () => Reject(() => new WordListRepository().Load(Write("duplicate-eid.json", ExampleList("first").Replace("\"02\"", "\"01\"")))));
        Check("missing earlier eid still counts its example", () => {
            string json = ExampleList("first").Replace(",\"eid\":\"01\"", "");
            var word = new WordListRepository().Load(Write("missing-earlier-eid.json", json)).Words[0];
            Equal("", word.Senses[0].Eid); Equal("02", word.Senses[1].Eid);
        });
    }
}
