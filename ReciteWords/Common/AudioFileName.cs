using System.Text.RegularExpressions;
namespace ReciteWords.Common;

public static class AudioFileName
{
    public static string Stem(string word)
    {
        return Regex.Replace(word.Trim(), "[^0-9a-zA-Z]", "_");
    }
}
