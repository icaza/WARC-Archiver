using System.Text;

namespace WarcArchiver.Core.Util;

/// <summary>RFC 4648 Base32 encoder/decoder (upper-case alphabet, '=' padding), the conventional encoding for WARC digest headers.</summary>
public static class Base32
{
    const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Encode(ReadOnlySpan<byte> data, bool padding = true)
    {
        var sb = new StringBuilder((data.Length * 8 + 4) / 5 + 8);
        int buffer = 0, bits = 0;
        foreach (byte b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
            buffer &= (1 << bits) - 1;
        }
        if (bits > 0)
        {
            sb.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        }
        if (padding)
        {
            while (sb.Length % 8 != 0)
            {
                sb.Append('=');
            }
        }
        return sb.ToString();
    }

    public static bool TryDecode(string text, out byte[] data)
    {
        var list = new List<byte>(text.Length * 5 / 8);
        int buffer = 0, bits = 0;
        foreach (char ch in text)
        {
            if (ch == '=')
            {
                break;
            }
            int v = Alphabet.IndexOf(char.ToUpperInvariant(ch));
            if (v < 0)
            {
                data = [];
                return false;
            }
            buffer = (buffer << 5) | v;
            bits += 5;
            if (bits >= 8)
            {
                list.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
                buffer &= (1 << bits) - 1;
            }
        }
        data = list.ToArray();
        return true;
    }
}
