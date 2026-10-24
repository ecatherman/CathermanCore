using CathermanCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace CathermanCore.EntityFramework;


//https://stackoverflow.com/a/71063271/959977
//https://stackoverflow.com/a/59556797/959977
internal class StringCollectionConverter<T> : ValueConverter<List<T>, string>
{
    public StringCollectionConverter() : base(Serialize, Deserialize, null) { }
    static Expression<Func<string, List<T>>> Deserialize = x => x.FromJson<List<T>>() ?? new List<T>();
    static Expression<Func<List<T>, string>> Serialize = x => x.ToJson() ?? "";
}

internal class StringCollectionComparer<T> : ValueComparer<List<T>>
{
    public StringCollectionComparer()
        : base((l1, l2) => l1.SequenceEqual(l2),
            l => l.Aggregate(0, (a, v) => HashCode.Combine(a, v)),
            l => l.ToList())
    {
    }
}
