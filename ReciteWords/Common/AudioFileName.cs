using System.Text.RegularExpressions;
namespace ReciteWords.Common;

public static class AudioFileName
{
    public static string Stem(string word)
    {
        return Regex.Replace(word.Trim().ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
    }
}
