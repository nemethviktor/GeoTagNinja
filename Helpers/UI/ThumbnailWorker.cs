using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Model;
using GeoTagNinja.View.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     Runs per-element thumbnail work on a few background threads, independent of the metadata read pass, and in an
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
    private readonly PriorityWorkList<DirectoryElement> _work = new();
    private readonly Action<DirectoryElement> _process;
    private readonly CancellationToken _ct;
    private bool _addingCompleted;
    private int _runningWorkers;

    /// <param name="process">
    ///     Does the work for one element (typically: make sure its thumbnail exists, then tell the view). Runs on a worker
    ///     thread.
    /// </param>
    /// <param name="ct">Cancels the worker.</param>
    /// <param name="workerCount">
    ///     How many threads. Kept small: past a few the disk (often a spinning or cloud-synced one) is the
    ///     bottleneck, not the CPU.
    /// </param>
    internal ThumbnailWorker(Action<DirectoryElement> process,
                             CancellationToken ct,
                             int workerCount)
    {
        _process = process;
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

    /// <summary>
    ///     Queues an element. Ignored if it was queued before (unless <paramref name="allowRepeat" />), or if
    ///     <see cref="CompleteAdding" /> was called.
    /// </summary>
    internal void Enqueue(DirectoryElement directoryElement,
                          bool allowRepeat = false)
    {
        lock (_lock)
        {
            if (_addingCompleted ||
                !_work.Add(item: directoryElement, allowRepeat: allowRepeat))
            {
                return;
            }

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
            _work.SetPriority(items: (IReadOnlyList<DirectoryElement>)copy);
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

                directoryElement = _work.TakeNext();
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

    private void Run()
    {
        try
        {
            while (TryTake(directoryElement: out DirectoryElement? directoryElement))
            {
                try
                {
                    _process(obj: directoryElement!);
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
