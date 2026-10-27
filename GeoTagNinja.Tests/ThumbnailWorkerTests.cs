using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GeoTagNinja.Tests;

/// <summary>
///     The ordering and lifetime rules of <see cref="ThumbnailWorker" />: visible rows first, each element once, and
///     the worker ending exactly when it should. (No images are touched: the work done per element is a stand-in, so
///     these tests see only the scheduling.)
/// </summary>
[TestFixture]
public class ThumbnailWorkerTests
{
    private static List<DirectoryElement> MakeElements(int count)
    {
        return Enumerable.Range(start: 0, count: count)
                         .Select(selector: i => new DirectoryElement(itemNameWithoutPath: $"f{i}.jpg",
                             type: DirectoryElement.ElementType.File,
                             fileNameWithPath: $@"C:\nonexistent\f{i}.jpg"))
                         .ToList();
    }

    private static void WaitUntil(Func<bool> condition,
                                  string because)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(value: 10);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                Assert.Fail(message: $"Timed out waiting for: {because}");
            }

            Thread.Sleep(millisecondsTimeout: 10);
        }
    }

    [Test]
    public void PrioritisedElementsAreTakenFirst_InTheOrderGiven_ThenTheRestInQueueOrder()
    {
        List<DirectoryElement> elements = MakeElements(count: 50);
        ConcurrentQueue<string> processed = new();
        using ManualResetEventSlim firstIsRunning = new(initialState: false);
        using ManualResetEventSlim release = new(initialState: false);

        // One worker. It takes f0 straight away and parks inside the callback, which gives us a deterministic
        // moment to change the priorities before it takes anything else.
        ThumbnailWorker worker = new(
            process: de =>
            {
                processed.Enqueue(item: de.ItemNameWithoutPath);
                if (processed.Count == 1)
                {
                    firstIsRunning.Set();
                    release.Wait();
                }
            },
            ct: CancellationToken.None,
            workerCount: 1);

        foreach (DirectoryElement element in elements)
        {
            worker.Enqueue(directoryElement: element);
        }

        Assert.That(firstIsRunning.Wait(millisecondsTimeout: 10000), Is.True);

        worker.Prioritise(elements: [elements[49], elements[48], elements[47]]);
        worker.CompleteAdding();
        release.Set();

        WaitUntil(condition: () => worker.IsDrained, because: "worker to drain");

        string[] expected = new[] { "f0.jpg", "f49.jpg", "f48.jpg", "f47.jpg" }
                            .Concat(second: Enumerable.Range(start: 1, count: 46).Select(selector: i => $"f{i}.jpg"))
                            .ToArray();
        Assert.That(processed.ToArray(), Is.EqualTo(expected));
    }

    [Test]
    public void EveryElementIsProcessedExactlyOnce_EvenWhenQueuedTwiceAndPrioritised()
    {
        List<DirectoryElement> elements = MakeElements(count: 200);
        ConcurrentQueue<string> processed = new();

        ThumbnailWorker worker = new(
            process: de => processed.Enqueue(item: de.ItemNameWithoutPath),
            ct: CancellationToken.None,
            workerCount: 4);

        foreach (DirectoryElement element in elements)
        {
            worker.Enqueue(directoryElement: element);
            worker.Enqueue(directoryElement: element); // duplicate
        }

        worker.Prioritise(elements: elements.Skip(count: 100));
        worker.CompleteAdding();

        WaitUntil(condition: () => worker.IsDrained, because: "worker to drain");

        Assert.That(processed.Count, Is.EqualTo(expected: 200));
        Assert.That(processed.Distinct().Count(), Is.EqualTo(expected: 200));
    }

    [Test]
    public void ElementsQueuedAfterCompleteAddingAreIgnored()
    {
        // The first version of the worker was closed while the metadata read was still handing it files, silently
        // dropping them. Closing means closed: this pins down what "ignored" looks like.
        List<DirectoryElement> elements = MakeElements(count: 10);
        ConcurrentQueue<string> processed = new();

        ThumbnailWorker worker = new(
            process: de => processed.Enqueue(item: de.ItemNameWithoutPath),
            ct: CancellationToken.None,
            workerCount: 2);

        worker.Enqueue(directoryElement: elements[0]);
        worker.CompleteAdding();
        worker.Enqueue(directoryElement: elements[1]);

        WaitUntil(condition: () => worker.IsDrained, because: "worker to drain");

        Assert.That(processed.ToArray(), Is.EqualTo(new[] { "f0.jpg" }));
    }

    [Test]
    public void WorkerKeepsWaitingForMoreUntilCompleteAddingIsCalled()
    {
        List<DirectoryElement> elements = MakeElements(count: 3);
        ConcurrentQueue<string> processed = new();

        ThumbnailWorker worker = new(
            process: de => processed.Enqueue(item: de.ItemNameWithoutPath),
            ct: CancellationToken.None,
            workerCount: 2);

        worker.Enqueue(directoryElement: elements[0]);
        WaitUntil(condition: () => processed.Count == 1, because: "first element");
        Assert.That(worker.IsDrained, Is.False, message: "must not exit while more may arrive");

        worker.Enqueue(directoryElement: elements[1]);
        WaitUntil(condition: () => processed.Count == 2, because: "second element, queued later");

        worker.CompleteAdding();
        WaitUntil(condition: () => worker.IsDrained, because: "worker to drain");
    }

    [Test]
    public void CancellationStopsTheWorkerWithoutProcessingTheRest()
    {
        List<DirectoryElement> elements = MakeElements(count: 100);
        int processed = 0;
        using CancellationTokenSource cts = new();
        using ManualResetEventSlim firstIsRunning = new(initialState: false);
        using ManualResetEventSlim release = new(initialState: false);

        ThumbnailWorker worker = new(
            process: _ =>
            {
                if (Interlocked.Increment(location: ref processed) == 1)
                {
                    firstIsRunning.Set();
                    release.Wait();
                }
            },
            ct: cts.Token,
            workerCount: 1);

        foreach (DirectoryElement element in elements)
        {
            worker.Enqueue(directoryElement: element);
        }

        Assert.That(firstIsRunning.Wait(millisecondsTimeout: 10000), Is.True);

        cts.Cancel();
        release.Set();

        WaitUntil(condition: () => worker.IsDrained, because: "cancelled worker to stop");
        Assert.That(processed, Is.EqualTo(expected: 1), message: "nothing after the cancel should have been processed");
    }
}
