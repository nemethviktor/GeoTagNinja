using System.Collections.Generic;
using System.Linq;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     A to-do list where a priority subset is served first. Items are taken from the priority list (in the order given)
///     if they are still waiting, otherwise in the order they were added; each item is accepted, and therefore taken, at
///     most once however often it is added. Used to read metadata and build thumbnails for the rows the user can see
///     before the rest.
/// </summary>
/// <remarks>Not thread-safe: callers lock around it.</remarks>
/// <typeparam name="T">Item type (compared by reference/equality).</typeparam>
internal sealed class PriorityWorkList<T>
    where T : class
{
    private readonly Queue<T> _queue = new();

    /// <summary>Items nobody has taken yet. The queue and priority list may hold stale entries; this is the truth.</summary>
    private readonly HashSet<T> _pending = [];

    /// <summary>Everything ever accepted, so an item is never taken twice.</summary>
    private readonly HashSet<T> _accepted = [];

    private IReadOnlyList<T> _priority = [];

    /// <summary>Items added and not yet taken.</summary>
    internal int PendingCount => _pending.Count;

    /// <summary>Adds an item. Ignored (false) if it was added before.</summary>
    internal bool Add(T item)
    {
        if (!_accepted.Add(item: item))
        {
            return false;
        }

        _ = _pending.Add(item: item);
        _queue.Enqueue(item: item);
        return true;
    }

    /// <summary>
    ///     Replaces the priority list. The list is kept by reference, so pass a list you won't modify afterwards.
    /// </summary>
    internal void SetPriority(IReadOnlyList<T> items)
    {
        _priority = items;
    }

    /// <summary>Takes the next item: priority first, then in added order. Null when nothing is waiting.</summary>
    internal T? TakeNext()
    {
        foreach (T candidate in _priority)
        {
            if (_pending.Remove(item: candidate))
            {
                return candidate;
            }
        }

        while (_queue.Count > 0)
        {
            T next = _queue.Dequeue();
            if (_pending.Remove(item: next))
            {
                return next;
            }
        }

        return null;
    }

    /// <summary>Convenience for tests and callers holding an enumerable.</summary>
    internal void SetPriority(IEnumerable<T> items) => SetPriority(items: items.ToList());
}
