using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace - for easier discoverability
namespace System.Collections.Generic;

internal static class CollectionsExtensions
{
    public static bool TryFind<TItem, TParameter>(
        this List<TItem> list,
        Func<TItem, TParameter, bool> predicate,
        TParameter parameter,
        [MaybeNullWhen(false)] out TItem foundItem,
        out int foundIndex)
        => TryFind(CollectionsMarshal.AsSpan(list), predicate, parameter, out foundItem, out foundIndex);

    public static bool TryFind<TItem, TParameter>(
        this ReadOnlySpan<TItem> span,
        Func<TItem, TParameter, bool> predicate,
        TParameter parameter,
        [MaybeNullWhen(false)] out TItem foundItem,
        out int foundIndex)
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < span.Length; i++)
        {
            var item = span[i];
            if (predicate(item, parameter))
            {
                foundItem = item;
                foundIndex = i;
                return true;
            }
        }

        foundItem = default;
        foundIndex = -1;
        return false;
    }

    public static int FindIndex<TItem, TParameter>(
        this List<TItem> list,
        Func<TItem, TParameter, bool> predicate,
        TParameter parameter)
        => FindIndex(CollectionsMarshal.AsSpan(list), predicate, parameter);

    public static int FindIndex<TItem, TParameter>(
        this ReadOnlySpan<TItem> span,
        Func<TItem, TParameter, bool> predicate,
        TParameter parameter)
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < span.Length; i++)
        {
            var item = span[i];
            if (predicate(item, parameter))
            {
                return i;
            }
        }

        return -1;
    }
}