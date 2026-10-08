using System.Text;

/// <summary>
/// Construye cuerpos JSON omitiendo los campos vacíos.
/// JsonUtility serializa los string nulos como "" y el backend rechaza, por ejemplo, studentId: "".
/// </summary>
public class JsonBody
{
    private readonly StringBuilder _sb = new StringBuilder("{");
    private bool _first = true;

    public JsonBody Add(string key, string value, int maxLength = 0)
    {
        if (string.IsNullOrEmpty(value)) return this;
        if (maxLength > 0 && value.Length > maxLength) value = value.Substring(0, maxLength);

        if (!_first) _sb.Append(',');
        _first = false;
        _sb.Append('"').Append(key).Append("\":\"").Append(Escape(value)).Append('"');
        return this;
    }

    public string Build() => _sb.ToString() + "}";

    private static string Escape(string s)
    {
        var sb = new StringBuilder(s.Length + 8);
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
}
