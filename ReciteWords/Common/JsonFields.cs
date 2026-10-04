using System.Text.Json;
namespace ReciteWords.Common;

internal static class JsonFields
{
    public static void Object(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new InvalidDataException("需要 JSON 对象");
        var names = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (var property in value.EnumerateObject())
                if (!names.Add(property.Name)) throw new InvalidDataException("重复字段: " + property.Name);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidDataException("JSON 字段名包含无效 Unicode", ex);
        }
    }
    public static string Text(JsonElement item, string key, bool required = false, bool allowEmpty = false)
    {
        if (!item.TryGetProperty(key, out var value))
        {
            if (required) throw new InvalidDataException("缺少 " + key);
            return "";
        }
        if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException(key + " 必须是字符串");
        string text = ReadString(value, key).Trim();
        if (required && !allowEmpty && text.Length == 0) throw new InvalidDataException(key + " 不能为空");
        return text;
    }
    public static JsonElement.ArrayEnumerator Array(JsonElement item, string key, bool nonEmpty = false)
    {
        if (!item.TryGetProperty(key, out var value)) throw new InvalidDataException("缺少 " + key);
        if (value.ValueKind != JsonValueKind.Array || (nonEmpty && value.GetArrayLength() == 0))
            throw new InvalidDataException(key + " 必须为" + (nonEmpty ? "非空" : "") + "数组");
        return value.EnumerateArray();
    }
    public static List<string> Strings(JsonElement item, string key)
    {
        var result = new List<string>();
        if (!item.TryGetProperty(key, out _)) return result;
        foreach (var value in Array(item, key))
        {
            if (value.ValueKind != JsonValueKind.String) throw new InvalidDataException(key + " 必须是字符串数组");
            string text = ReadString(value, key).Trim();
            if (text.Length > 0) result.Add(text);
        }
        return result;
    }
    private static string ReadString(JsonElement value, string key)
    {
        try { return value.GetString()!; }
        catch (InvalidOperationException ex) { throw new InvalidDataException(key + " 包含无效 Unicode", ex); }
    }
    public static bool Digits(string text)
    {
        if (text.Length == 0) return false;
        foreach (char c in text) if (c < '0' || c > '9') return false;
        return true;
    }
}
