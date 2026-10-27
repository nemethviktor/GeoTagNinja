using GeoTagNinja.Model;
using GeoTagNinja.View.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     Generates icon-view thumbnails on a few background threads, independent of the metadata read pass, and in an
///     order that favours what the user is looking at: elements passed to <see cref="Prioritise" /> are taken first, in
///     the order given; everything else follows in the order it was queued.
/// </summary>
/// <remarks>
///     One instance per folder load. It stops when the token is cancelled (folder change / app close) or once
///     <see cref="CompleteAdding" /> has been called and the queue has drained.
/// </remarks>
internal sealed class ThumbnailWorker
{
    private readonly object _lock = new();
    private readonly Queue<DirectoryElement> _queue = new();

    /// <summary>Queued elements nobody has taken yet. The queue and priority list may hold stale entries; this is the truth.</summary>
    private readonly HashSet<DirectoryElement> _pending = [];

    /// <summary>Everything ever accepted, so an element is processed at most once per worker however often it is queued.</summary>
    private readonly HashSet<DirectoryElement> _accepted = [];

    private readonly Action<DirectoryElement> _onGenerated;
    private readonly CancellationToken _ct;
    private List<DirectoryElement> _priority = [];
    private bool _addingCompleted;
    private int _runningWorkers;

    /// <param name="onGenerated">Called on a worker thread after an element's thumbnail has been attempted.</param>
    /// <param name="ct">Cancels the worker.</param>
    /// <param name="workerCount">
    ///     How many threads. Kept small: past a few the disk (often a spinning or cloud-synced one) is the
    ///     bottleneck, not the CPU.
    /// </param>
    internal ThumbnailWorker(Action<DirectoryElement> onGenerated,
                             CancellationToken ct,
                             int workerCount)
    {
        _onGenerated = onGenerated;
        _ct = ct;
        _runningWorkers = workerCount;

        for (int i = 0; i < workerCount; i++)
        {
            _ = Task.Factory.StartNew(action: Run,
                cancellationToken: CancellationToken.None,
                creationOptions: TaskCreationOptions.LongRunning,
                scheduler: TaskScheduler.Default);
        }
    }

    /// <summary>A sensible worker count for this machine.</summary>
    internal static int DefaultWorkerCount => Math.Min(val1: 4, val2: Math.Max(val1: 2, val2: Environment.ProcessorCount - 1));

    /// <summary>True once every worker thread has finished (queue drained after <see cref="CompleteAdding" />, or cancelled).</summary>
    internal bool IsDrained => Volatile.Read(location: ref _runningWorkers) == 0;

    /// <summary>Queues an element. Ignored if it was queued before, or if <see cref="CompleteAdding" /> was called.</summary>
    internal void Enqueue(DirectoryElement directoryElement)
    {
        lock (_lock)
        {
            if (_addingCompleted ||
                !_accepted.Add(item: directoryElement))
            {
                return;
            }

            _ = _pending.Add(item: directoryElement);
            _queue.Enqueue(item: directoryElement);
            Monitor.Pulse(obj: _lock);
        }
    }

    /// <summary>
    ///     Replaces the priority list: these elements (if still waiting) are generated before anything else, in this
    ///     order. Pass the rows the user can see, then the ones about to scroll into view.
    /// </summary>
    internal void Prioritise(IEnumerable<DirectoryElement> elements)
    {
        List<DirectoryElement> copy = elements.ToList();
        lock (_lock)
        {
            _priority = copy;
        }
    }

    /// <summary>No more elements are coming; workers exit once the queue is drained. Safe to call repeatedly.</summary>
    internal void CompleteAdding()
    {
        lock (_lock)
        {
            _addingCompleted = true;
            Monitor.PulseAll(obj: _lock);
        }
    }

    private bool TryTake(out DirectoryElement? directoryElement)
    {
        lock (_lock)
        {
            for (; ; )
            {
                if (_ct.IsCancellationRequested)
                {
                    directoryElement = null;
                    return false;
                }

                directoryElement = TakeNextPending();
                if (directoryElement != null)
                {
                    return true;
                }

                if (_addingCompleted)
                {
                    return false;
                }

                // wake periodically so cancellation is noticed
                _ = Monitor.Wait(obj: _lock, millisecondsTimeout: 250);
            }
        }
    }

    /// <summary>Caller holds the lock.</summary>
    private DirectoryElement? TakeNextPending()
    {
        foreach (DirectoryElement candidate in _priority)
        {
            if (_pending.Remove(item: candidate))
            {
                return candidate;
            }
        }

        while (_queue.Count > 0)
        {
            DirectoryElement next = _queue.Dequeue();
            if (_pending.Remove(item: next))
            {
                return next;
            }
        }

        return null;
    }

    private void Run()
    {
        try
        {
            while (TryTake(directoryElement: out DirectoryElement? directoryElement))
            {
                try
                {
                    directoryElement!.GenerateThumbnailIfRequired();
                    _onGenerated(obj: directoryElement);
                }
                catch (Exception ex)
                {
                    FrmMainApp.Log.Error(ex, message: $"Thumbnail worker failed for {directoryElement!.ItemNameWithoutPath}");
                }
            }
        }
        finally
        {
            _ = Interlocked.Decrement(location: ref _runningWorkers);
        }
    }
}
