using System.Net;

namespace WarcArchiver.Core.Parsing;

public enum ResourceKind
{
    Document,
    Script,
    Stylesheet,
    Image,
    Font,
    Media,
    Frame,
    Other,
}

public readonly record struct ResourceRef(Uri Url, ResourceKind Kind);

internal static class UrlResolver
{
    const int MaxUrlLength = 4096;

    /// <summary>Resolves a reference against a base and keeps only plain http(s) URLs without credentials or fragment.</summary>
    public static Uri? Resolve(Uri baseUri, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }
        raw = raw.Trim();
        if (raw.Length > MaxUrlLength || raw[0] == '#')
        {
            return null;
        }
        if (!Uri.TryCreate(baseUri, raw, out Uri? absolute))
        {
            return null;
        }
        if (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps)
        {
            return null;
        }
        if (!string.IsNullOrEmpty(absolute.UserInfo) || string.IsNullOrEmpty(absolute.Host))
        {
            return null;
        }
        return Uri.TryCreate(absolute.GetLeftPart(UriPartial.Query), UriKind.Absolute, out Uri? clean) ? clean : null;
    }
}

/// <summary>
/// A small, tolerant, linear-time HTML tag scanner that lists the sub-resources a browser would load. It is
/// deliberately not a full HTML parser: it never executes anything and cannot be driven into pathological backtracking.
/// </summary>
public static class HtmlResourceExtractor
{
    const int MaxAttributesPerTag = 64;
    const int MaxTagLength = 64 * 1024;

    public static List<ResourceRef> Extract(string html, Uri documentUri)
    {
        var found = new List<ResourceRef>();
        Uri baseUri = documentUri;
        bool baseSet = false;
        int i = 0;
        int n = html.Length;

        void Add(Uri root, string? raw, ResourceKind kind)
        {
            Uri? url = UrlResolver.Resolve(root, raw);
            if (url is not null)
            {
                found.Add(new ResourceRef(url, kind));
            }
        }

        while (i < n)
        {
            int lt = html.IndexOf('<', i);
            if (lt < 0 || lt + 1 >= n)
            {
                break;
            }

            if (string.CompareOrdinal(html, lt, "<!--", 0, 4) == 0)
            {
                int endComment = html.IndexOf("-->", lt + 4, StringComparison.Ordinal);
                i = endComment < 0 ? n : endComment + 3;
                continue;
            }

            int p = lt + 1;
            if (html[p] == '/' || !char.IsAsciiLetter(html[p]))
            {
                int gt = html.IndexOf('>', p);
                i = gt < 0 ? n : gt + 1;
                continue;
            }

            int nameStart = p;
            while (p < n && !char.IsWhiteSpace(html[p]) && html[p] != '/' && html[p] != '>')
            {
                p++;
            }
            string name = html[nameStart..p].ToLowerInvariant();
            Dictionary<string, string> attrs = ParseAttributes(html, ref p);
            i = p;

            string? Attr(string key) => attrs.TryGetValue(key, out var v) ? v : null;
            Uri root = baseUri;

            switch (name)
            {
                case "base":
                    if (!baseSet && UrlResolver.Resolve(documentUri, Attr("href")) is { } newBase)
                    {
                        baseUri = newBase;
                        baseSet = true;
                    }
                    break;

                case "script":
                    Add(root, Attr("src"), ResourceKind.Script);
                    break;

                case "link":
                    AddLink(attrs, root, Add);
                    break;

                case "img":
                    Add(root, Attr("src"), ResourceKind.Image);
                    foreach (string candidate in ParseSrcSet(Attr("srcset")))
                    {
                        Add(root, candidate, ResourceKind.Image);
                    }
                    break;

                case "source":
                    // <source src> belongs to <video>/<audio>; <source srcset> belongs to <picture>.
                    Add(root, Attr("src"), ResourceKind.Media);
                    foreach (string candidate in ParseSrcSet(Attr("srcset")))
                    {
                        Add(root, candidate, ResourceKind.Image);
                    }
                    break;

                case "video":
                    Add(root, Attr("src"), ResourceKind.Media);
                    Add(root, Attr("poster"), ResourceKind.Image);
                    break;

                case "audio":
                    Add(root, Attr("src"), ResourceKind.Media);
                    break;

                case "track":
                case "embed":
                    Add(root, Attr("src"), ResourceKind.Other);
                    break;

                case "object":
                    Add(root, Attr("data"), ResourceKind.Other);
                    break;

                case "iframe":
                case "frame":
                    Add(root, Attr("src"), ResourceKind.Frame);
                    break;

                case "input":
                    if (string.Equals(Attr("type"), "image", StringComparison.OrdinalIgnoreCase))
                    {
                        Add(root, Attr("src"), ResourceKind.Image);
                    }
                    break;

                case "image":
                    Add(root, Attr("href") ?? Attr("xlink:href"), ResourceKind.Image);
                    break;
            }

            Add(root, Attr("background"), ResourceKind.Image);
            if (Attr("style") is { } inlineStyle)
            {
                found.AddRange(CssResourceExtractor.Extract(inlineStyle, root));
            }

            if (name is "script" or "style" or "textarea" or "title")
            {
                int close = html.IndexOf("</" + name, i, StringComparison.OrdinalIgnoreCase);
                int contentEnd = close < 0 ? n : close;
                if (name == "style")
                {
                    found.AddRange(CssResourceExtractor.Extract(html[i..contentEnd], root));
                }
                i = contentEnd;
            }
        }
        return found;
    }

    static void AddLink(Dictionary<string, string> attrs, Uri root, Action<Uri, string?, ResourceKind> add)
    {
        attrs.TryGetValue("href", out string? href);
        string rel = attrs.TryGetValue("rel", out string? r) ? r.ToLowerInvariant() : string.Empty;
        string[] tokens = rel.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Contains("stylesheet"))
        {
            add(root, href, ResourceKind.Stylesheet);
        }
        else if (tokens.Any(t => t.Contains("icon", StringComparison.Ordinal)))
        {
            add(root, href, ResourceKind.Image);
        }
        else if (tokens.Contains("modulepreload"))
        {
            add(root, href, ResourceKind.Script);
        }
        else if (tokens.Contains("preload"))
        {
            string kind = attrs.TryGetValue("as", out string? a) ? a.ToLowerInvariant() : string.Empty;
            ResourceKind mapped = kind switch
            {
                "script" => ResourceKind.Script,
                "style" => ResourceKind.Stylesheet,
                "image" => ResourceKind.Image,
                "font" => ResourceKind.Font,
                "audio" or "video" or "track" => ResourceKind.Media,
                _ => ResourceKind.Other,
            };
            add(root, href, mapped);
        }
    }

    static Dictionary<string, string> ParseAttributes(string html, ref int p)
    {
        var attrs = new Dictionary<string, string>(StringComparer.Ordinal);
        int n = html.Length;
        int limit = Math.Min(n, p + MaxTagLength);

        while (p < limit)
        {
            while (p < limit && (char.IsWhiteSpace(html[p]) || html[p] == '/'))
            {
                p++;
            }
            if (p >= limit)
            {
                break;
            }
            if (html[p] == '>')
            {
                p++;
                return attrs;
            }

            int nameStart = p;
            while (p < limit && !char.IsWhiteSpace(html[p]) && html[p] != '=' && html[p] != '>' && html[p] != '/')
            {
                p++;
            }
            string attrName = html[nameStart..p].ToLowerInvariant();

            while (p < limit && char.IsWhiteSpace(html[p]))
            {
                p++;
            }

            string value = string.Empty;
            if (p < limit && html[p] == '=')
            {
                p++;
                while (p < limit && char.IsWhiteSpace(html[p]))
                {
                    p++;
                }
                if (p < limit && (html[p] == '"' || html[p] == '\''))
                {
                    char quote = html[p++];
                    int end = html.IndexOf(quote, p, limit - p);
                    if (end < 0)
                    {
                        end = limit;
                    }
                    value = html[p..end];
                    p = Math.Min(end + 1, limit);
                }
                else
                {
                    int start = p;
                    while (p < limit && !char.IsWhiteSpace(html[p]) && html[p] != '>')
                    {
                        p++;
                    }
                    value = html[start..p];
                }
            }

            if (attrName.Length > 0 && attrs.Count < MaxAttributesPerTag && !attrs.ContainsKey(attrName))
            {
                attrs[attrName] = WebUtility.HtmlDecode(value);
            }
        }
        return attrs;
    }

    static IEnumerable<string> ParseSrcSet(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        int i = 0;
        int n = value.Length;
        while (i < n)
        {
            while (i < n && (char.IsWhiteSpace(value[i]) || value[i] == ','))
            {
                i++;
            }
            int start = i;
            while (i < n && !char.IsWhiteSpace(value[i]))
            {
                i++;
            }
            string url = value[start..i];
            if (url.EndsWith(','))
            {
                url = url.TrimEnd(',');
            }
            else
            {
                while (i < n && value[i] != ',')
                {
                    i++;
                }
            }
            if (url.Length > 0)
            {
                yield return url;
            }
        }
    }
}

/// <summary>Finds url(...) and @import references in CSS text.</summary>
public static class CssResourceExtractor
{
    static readonly string[] FontExtensions = [".woff", ".woff2", ".ttf", ".otf", ".eot"];

    public static List<ResourceRef> Extract(string css, Uri cssUri)
    {
        var found = new List<ResourceRef>();
        int i = 0;
        int n = css.Length;

        while (i < n)
        {
            char c = css[i];

            if (c == '/' && i + 1 < n && css[i + 1] == '*')
            {
                int end = css.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? n : end + 2;
                continue;
            }

            if (c == '@' && IsWordAt(css, i, "@import"))
            {
                i += 7;
                SkipWhitespace(css, ref i);
                string? target = null;
                if (i < n && (css[i] == '"' || css[i] == '\''))
                {
                    target = ReadQuoted(css, ref i);
                }
                else if (IsWordAt(css, i, "url("))
                {
                    i += 4;
                    target = ReadUrlBody(css, ref i);
                }
                Uri? url = UrlResolver.Resolve(cssUri, target);
                if (url is not null)
                {
                    found.Add(new ResourceRef(url, ResourceKind.Stylesheet));
                }
                continue;
            }

            if ((c == 'u' || c == 'U') && IsWordAt(css, i, "url(") && (i == 0 || !IsIdentifierChar(css[i - 1])))
            {
                i += 4;
                Uri? url = UrlResolver.Resolve(cssUri, ReadUrlBody(css, ref i));
                if (url is not null)
                {
                    found.Add(new ResourceRef(url, KindFromPath(url.AbsolutePath)));
                }
                continue;
            }

            i++;
        }
        return found;
    }

    static ResourceKind KindFromPath(string path)
    {
        foreach (string ext in FontExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            {
                return ResourceKind.Font;
            }
        }
        return path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ? ResourceKind.Stylesheet : ResourceKind.Image;
    }

    static bool IsWordAt(string s, int index, string word) =>
        index + word.Length <= s.Length && string.Compare(s, index, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) == 0;

    static bool IsIdentifierChar(char c) => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_';

    static void SkipWhitespace(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i]))
        {
            i++;
        }
    }

    static string ReadQuoted(string s, ref int i)
    {
        char quote = s[i++];
        int start = i;
        while (i < s.Length && s[i] != quote && s[i] != '\n')
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                i++;
            }
            i++;
        }
        string value = s[start..Math.Min(i, s.Length)];
        if (i < s.Length && s[i] == quote)
        {
            i++;
        }
        return value;
    }

    static string? ReadUrlBody(string s, ref int i)
    {
        SkipWhitespace(s, ref i);
        if (i >= s.Length)
        {
            return null;
        }
        string value;
        if (s[i] == '"' || s[i] == '\'')
        {
            value = ReadQuoted(s, ref i);
        }
        else
        {
            int start = i;
            while (i < s.Length && s[i] != ')' && !char.IsWhiteSpace(s[i]))
            {
                i++;
            }
            value = s[start..i];
        }
        while (i < s.Length && s[i] != ')')
        {
            i++;
        }
        if (i < s.Length)
        {
            i++;
        }
        return value;
    }
}
