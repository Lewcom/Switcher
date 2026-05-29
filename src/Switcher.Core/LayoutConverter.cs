namespace Switcher.Core;

public sealed class LayoutConverter
{
    private static readonly Dictionary<char, char> EnToUa = BuildEnToUaMap();
    private static readonly Dictionary<char, char> UaToEn = BuildReverseMap(EnToUa);

    public string Convert(string input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var current = chars[i];
            if (EnToUa.TryGetValue(current, out var ua))
            {
                chars[i] = ua;
                continue;
            }

            if (UaToEn.TryGetValue(current, out var en))
            {
                chars[i] = en;
            }
        }

        return new string(chars);
    }

    private static Dictionary<char, char> BuildEnToUaMap()
    {
        const string enLower = "qwertyuiop[]asdfghjkl;'zxcvbnm";
        const string uaLower = "йцукенгшщзхїфівапролджєячсмить";

        var map = new Dictionary<char, char>(enLower.Length * 2);
        for (var i = 0; i < enLower.Length; i++)
        {
            var en = enLower[i];
            var ua = uaLower[i];

            map[en] = ua;
            map[char.ToUpperInvariant(en)] = char.ToUpperInvariant(ua);
        }

        return map;
    }

    private static Dictionary<char, char> BuildReverseMap(Dictionary<char, char> source)
    {
        var reverse = new Dictionary<char, char>(source.Count);
        foreach (var pair in source)
        {
            reverse[pair.Value] = pair.Key;
        }

        return reverse;
    }
}
