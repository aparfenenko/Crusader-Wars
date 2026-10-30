using System.Text;

namespace CrusaderWars.Core.Utils;

/// <summary>
/// Text helpers shared by the log parser and the UI. RemoveASCII is a byte-for-byte
/// port of the legacy HomePage.RemoveASCII: it keeps digits, ASCII letters, the given
/// punctuation set and any char.IsLetter codepoint (i.e. non-ASCII letters pass too).
/// </summary>
public static class TextUtils
{
    public static string RemoveASCII(string inputString)
    {
        StringBuilder sb = new StringBuilder();
        string apostrophe = "'";
        foreach (char c in inputString)
        {
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_' || c == '\n' || c == '-' || c == ':' || c == ' ' || char.IsLetter(c) || c == '?' || c == apostrophe[0] || c == '%')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
