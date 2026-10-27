using GeoTagNinja.Helpers.Data;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace GeoTagNinja.Tests;

/// <summary>The ordering rules behind "visible rows first": see <see cref="PriorityWorkList{T}" />.</summary>
[TestFixture]
public class PriorityWorkListTests
{
    private static List<string> Drain(PriorityWorkList<string> list)
    {
        List<string> taken = [];
        while (list.TakeNext() is { } item)
        {
            taken.Add(item: item);
        }

        return taken;
    }

    [Test]
    public void WithoutPriority_ItemsComeInAddedOrder()
    {
        PriorityWorkList<string> list = new();
        foreach (string s in new[] { "a", "b", "c" })
        {
            _ = list.Add(item: s);
        }

        Assert.That(Drain(list: list), Is.EqualTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void PriorityItemsComeFirstInTheOrderGiven_ThenTheRest()
    {
        PriorityWorkList<string> list = new();
        foreach (string s in new[] { "a", "b", "c", "d", "e" })
        {
            _ = list.Add(item: s);
        }

        list.SetPriority(items: new[] { "d", "c" });

        Assert.That(Drain(list: list), Is.EqualTo(new[] { "d", "c", "a", "b", "e" }));
    }

    [Test]
    public void ChangingThePriorityMidWay_AppliesToWhatIsStillWaiting()
    {
        PriorityWorkList<string> list = new();
        foreach (string s in new[] { "a", "b", "c", "d" })
        {
            _ = list.Add(item: s);
        }

        Assert.That(list.TakeNext(), Is.EqualTo(expected: "a"));
        list.SetPriority(items: new[] { "d" });
        Assert.That(list.TakeNext(), Is.EqualTo(expected: "d"));
        list.SetPriority(items: new[] { "a", "d", "c" }); // a and d are gone; c is next
        Assert.That(Drain(list: list), Is.EqualTo(new[] { "c", "b" }));
    }

    [Test]
    public void AnItemIsTakenAtMostOnce_HoweverOftenAddedOrPrioritised()
    {
        PriorityWorkList<string> list = new();
        Assert.That(list.Add(item: "a"), Is.True);
        Assert.That(list.Add(item: "a"), Is.False);
        _ = list.Add(item: "b");
        list.SetPriority(items: new[] { "b", "b", "a", "a" });

        List<string> taken = Drain(list: list);
        Assert.That(taken, Is.EqualTo(new[] { "b", "a" }));

        // even after it has been taken, adding it again must not bring it back
        Assert.That(list.Add(item: "a"), Is.False);
        Assert.That(list.TakeNext(), Is.Null);
    }

    [Test]
    public void PendingCountTracksWhatIsLeft()
    {
        PriorityWorkList<string> list = new();
        foreach (string s in Enumerable.Range(start: 0, count: 5).Select(selector: i => $"x{i}"))
        {
            _ = list.Add(item: s);
        }

        Assert.That(list.PendingCount, Is.EqualTo(expected: 5));
        _ = list.TakeNext();
        _ = list.TakeNext();
        Assert.That(list.PendingCount, Is.EqualTo(expected: 3));
    }
}
