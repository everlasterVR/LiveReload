using System.Collections.Generic;

public static class IDictionaryExtensions
{
    public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source)
    {
        foreach(var kvp in source)
        {
            target.Add(kvp.Key, kvp.Value);
        }
    }
}
