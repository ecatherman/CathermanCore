using Newtonsoft.Json;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace CathermanCore;

public static class Extensions
{
    // 1. Fast-path for standard objects / reference types
    // Returning true guarantees 'obj' is not null.
    public static bool HasValue([NotNullWhen(true)] this object? obj)
    {
        if (obj is null) return false;
        if (obj is string s) return !string.IsNullOrWhiteSpace(s);
        if (obj is IList list) return list.Count > 0;

        return true;
    }

    // 2. Generic fallback for Nullable<T> structs
    // Returning true guarantees 'obj' is not null (has a value).
    public static bool HasValue<T>([NotNullWhen(true)] this T? obj) where T : struct
    {
        return obj.HasValue && !obj.Value.Equals(default(T));
    }

    // 3. Explicit overload for int
    public static bool HasValue(this int i) => i != 0;
    public static bool HasValue([NotNullWhen(true)] this int? i) => i.HasValue && i.Value != 0;

    // 4. Optimized generic Collection parsing
    // Returning true guarantees 'list' is not null.
    public static bool HasValue<T>([NotNullWhen(true)] this IList<T>? list, Func<T, bool> lambda)
    {
        if (list is null || list.Count == 0) return false;

        for (int i = 0; i < list.Count; i++)
            if (lambda(list[i])) return true;
        return false;
    }

    // 5. Inverted helper methods (HasNoValue)
    // Returning false guarantees the input is NOT null.
    public static bool HasNoValue([NotNullWhen(false)] this object? obj) => !obj.HasValue();
    public static bool HasNoValue<T>([NotNullWhen(false)] this T? obj) where T : struct => !obj.HasValue();
    public static bool HasNoValue([NotNullWhen(false)] this int? i) => !i.HasValue();
    public static bool HasNoValue(this int i) => i == 0;

    public static T FromJson<T>(this string? str) where T : new() =>
        str.HasValue() ? JsonConvert.DeserializeObject<T>(str) ?? new T() : new T();
    
    public static string ToJson<T>(this T obj) 
    {
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        return JsonConvert.SerializeObject(obj, settings);
    }

    public static string ToJson<T>(this T obj, JsonSerializerSettings settings) => 
        JsonConvert.SerializeObject(obj, settings);

    public static IDictionary<string, object?> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
    {
        return source.GetType().GetProperties(bindingAttr).ToDictionary
        (
            propInfo => propInfo.Name,
            propInfo => propInfo.GetValue(source, null)
        );
    }

    public static void MoveToFront<T>(this List<T> list, Predicate<T> predicate)
    {
        var item = list.Find(predicate) ?? default;
        if (item != null)
        {
            list.Remove(item);
            list.Insert(0, item);
        }
    }

    public static string RemoveDigits(this string str) =>
        Regex.Replace(str, @"[\d-]", string.Empty);

    public static List<int> GetNumbers(this string str)
    {
        var numStrs = str.Split(' ')
            .Select(s => string.Concat(s.Where(Char.IsDigit)))
            .Select(s => Int32.TryParse(s, out int n) ? n : (int?)null)
            .OfType<int>()
            .ToList();

        return numStrs;
    }

    //https://stackoverflow.com/a/4405876
    public static string FirstCharToUpper(this string input) => input switch
    {
        null or "" => "",
        _ => string.Concat(input[0].ToString().ToUpperInvariant(), input.Substring(1).ToLowerInvariant()).Trim()
    };

    public static void AddIfMissing<T>(this List<T> list, T item)
    {
        if (item != null && !list.Contains(item))
            list.Add(item);
    }

    public static void AddIfMissing<T>(this List<T> list, IEnumerable<T> items)
    {
        var existing = list.ToHashSet();

        foreach (var item in items)
            if (!existing.Contains(item))
                list.Add(item);
    }

    public static List<string> SplitAndTrim(this string input, string delimiter) =>
        input.Split(delimiter,StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    
    public static T? Pop<T>(this List<T> list)
    {
        var item = list.FirstOrDefault();
        if (item == null)
            return default;

        list.RemoveAt(0);
        return item;
    }

    public static string ToBase64(this string input) => Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
    public static string FromBase64(this string input) => Encoding.UTF8.GetString(Convert.FromBase64String(input));
    public static string ToBase64(this object input) => input.ToJson().ToBase64();
    public static T FromBase64<T>(this string input) where T : new() => input.FromBase64().FromJson<T>();

    public static bool IsSimpleType(this Type type) => 
        type.IsPrimitive || type.IsValueType || type == typeof(string);

    public static bool IsList(this object o)
    {
        if (o == null) return false;
        return o is IList &&
               o.GetType().IsGenericType &&
               o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
    }

    public static bool IsDictionary(this object o)
    {
        if (o == null) return false;
        return o is IDictionary &&
               o.GetType().IsGenericType &&
               o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
    }

    public static List<T> Column<T>(this List<T> source, int index, int total)
    {
        int count = source.Count / total + (source.Count % total == 0 ? 0 : 1);
        int skip = index * count;
        var list = source.Skip(skip).ToList();

        if(list.Count > count)
            list = list.Take(count).ToList();

        return list;
    }


    //https://stackoverflow.com/a/16193323/959977
    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> valueFactory)
    {
        if(!key.HasValue())
            throw new ArgumentNullException($"Key of Type {typeof(TKey).Name} has no value");

		if (!dict.TryGetValue(key, out var val))
        {
            val = valueFactory();
            dict.Add(key, val);
        }

        return val;
    }

    public static List<string> GetSubstrings(this string str, string delimiter1, string delimiter2)
    {
        var result = str.Split().Where(x => x.StartsWith(delimiter1) && x.EndsWith(delimiter2))
                     .Select(x => x.Replace(delimiter1, string.Empty).Replace(delimiter2, string.Empty))
                     .ToList();

        return result;
    }

    public static Dictionary<int, T> ToIndexDictionary<T>(this IEnumerable<T> source) =>
        source.Select((value, index) => new { Value = value, Index = index })
            .ToDictionary(item => item.Index, item => item.Value);


    // https://stackoverflow.com/questions/3624332/how-do-you-remove-all-the-alphabetic-characters-from-a-string
    public static string RemoveLetters(this string self) =>
        new string(self.Where(c => Char.IsSymbol(c) || Char.IsDigit(c) || Char.IsPunctuation(c) || c == ' ').ToArray());

    public static string GetLetters(this string input) =>
        new string(input.Where(char.IsLetter).ToArray());

    public static bool ContainsSubstringTwice(this string mainString, string subString)
    {
        int firstIndex = mainString.IndexOf(subString);
        if (firstIndex == -1) // Substring not found at all
        {
            return false;
        }
        int lastIndex = mainString.LastIndexOf(subString);
        return firstIndex != lastIndex; // True if first and last occurrences are at different positions
    }

    public static bool HaveSameUniqueInts(this IEnumerable<int>? list1, IEnumerable<int>? list2)
    {
        if (list1 is null || list2 is null) return ReferenceEquals(list1, list2);
        if (ReferenceEquals(list1, list2)) return true;

        // SetEquals natively runs in O(N) time and allocates minimally
        var set = new HashSet<int>(list1);
        return set.SetEquals(list2);
    }

    public static string CapitalizeEveryWord(this string input)
    {
        // Convert to lowercase first to ensure proper handling of acronyms and existing caps
        string lowerCaseInput = input.ToLower();
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

        return textInfo.ToTitleCase(lowerCaseInput);
    }

    public static DateTime Minute(this DateTime dateTime) =>
        new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);

    public static void AddOrReplaceEntity<T, TId>(this List<T> list, T entity) where T : IHasId<TId>
    {
        int index = list.FindIndex(s => EqualityComparer<TId>.Default.Equals(s.Id, entity.Id));
        if (index >= 0)
            list[index] = entity;
        else
            list.AddIfMissing(entity);
    }

    public static bool AllIdsMatch<T, TId>(this IList<T> list, IEnumerable<TId> ids) where T : IHasId<TId>
    {
        if (list.HasNoValue() || ids.HasNoValue()) return false;

        // HashSet reduces lookup time from O(N) per item to O(1)
        var idSet = ids is HashSet<TId> set ? set : ids.ToHashSet();

        return list.Count == idSet.Count && list.All(item => idSet.Contains(item.Id));
    }

}
