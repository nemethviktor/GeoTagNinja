using GeoTagNinja.Model;
using GeoTagNinja.View.Forms;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     Generates icon-view thumbnails on a few background threads, independent of the metadata read pass. Thumbnails
///     used to be built inline in that pass, so every file's thumbnail delayed the next file's ExifTool read.
/// </summary>
/// <remarks>
///     One instance per folder load. It stops when the token is cancelled (folder change / app close) or once
///     <see cref="CompleteAdding" /> has been called and the queue has drained.
/// </remarks>
internal sealed class ThumbnailWorker
{
    private readonly BlockingCollection<DirectoryElement> _queue = new();
    private readonly Action<DirectoryElement> _onGenerated;
    private readonly CancellationToken _ct;

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

    internal void Enqueue(DirectoryElement directoryElement)
    {
        try
        {
            _queue.Add(item: directoryElement, cancellationToken: _ct);
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException or ObjectDisposedException)
        {
            // cancelled, or already completed: nothing more to do.
        }
    }

    /// <summary>No more elements are coming; workers exit once the queue is drained.</summary>
    internal void CompleteAdding()
    {
        try
        {
            _queue.CompleteAdding();
        }
        catch (ObjectDisposedException)
        {
            // nothing
        }
    }

    private void Run()
    {
        try
        {
            foreach (DirectoryElement directoryElement in _queue.GetConsumingEnumerable(cancellationToken: _ct))
            {
                try
                {
                    directoryElement.GenerateThumbnailIfRequired();
                    _onGenerated(obj: directoryElement);
                }
                catch (Exception ex)
                {
                    FrmMainApp.Log.Error(ex, message: $"Thumbnail worker failed for {directoryElement.ItemNameWithoutPath}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // folder changed or app closing
        }
    }
}
