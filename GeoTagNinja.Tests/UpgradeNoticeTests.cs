using GeoTagNinja.Helpers.Startup;
using NUnit.Framework;
using System.Linq;

namespace GeoTagNinja.Tests;

/// <summary>
///     Who gets the "this changed" message after an upgrade: users on an older build, once, and never new users.
/// </summary>
[TestFixture]
public class UpgradeNoticeTests
{
    private static readonly UpgradeNotice[] Notices =
    [
        new(showIfLastRunBuildAtMost: 9778, message: "a"),
        new(showIfLastRunBuildAtMost: 9900, message: "b")
    ];

    private static string[] Keys(string storedBuild,
                                 bool settingsAlreadyExist,
                                 int currentBuild)
    {
        return UpgradeNotices.Evaluate(storedBuild: storedBuild, settingsAlreadyExist: settingsAlreadyExist,
            currentBuild: currentBuild, notices: Notices).Pending.Select(selector: n => n.Message).ToArray();
    }

    [Test]
    public void NewUserSeesNothing_AndIsStampedWithTheCurrentBuild()
    {
        (var pending, int buildToStore) = UpgradeNotices.Evaluate(storedBuild: null, settingsAlreadyExist: false,
            currentBuild: 9800, notices: Notices);

        Assert.That(actual: pending, expression: Is.Empty);
        Assert.That(actual: buildToStore, expression: Is.EqualTo(expected: 9800));
    }

    [Test]
    public void ExistingUserWithNoStoredBuild_IsTreatedAsTheLastUntrackedBuild()
    {
        Assert.That(actual: Keys(storedBuild: null, settingsAlreadyExist: true, currentBuild: 9800),
            expression: Is.EqualTo(expected: new[] { "a", "b" }));
    }

    [Test]
    public void UnparseableStoredBuild_IsTreatedLikeNoStoredBuild()
    {
        Assert.That(actual: Keys(storedBuild: "oops", settingsAlreadyExist: true, currentBuild: 9800),
            expression: Is.EqualTo(expected: new[] { "a", "b" }));
    }

    [Test]
    public void ThresholdIsInclusive()
    {
        Assert.That(actual: Keys(storedBuild: "9778", settingsAlreadyExist: true, currentBuild: 9800),
            expression: Is.EqualTo(expected: new[] { "a", "b" }));
        Assert.That(actual: Keys(storedBuild: "9779", settingsAlreadyExist: true, currentBuild: 9800),
            expression: Is.EqualTo(expected: new[] { "b" }));
        Assert.That(actual: Keys(storedBuild: "9901", settingsAlreadyExist: true, currentBuild: 9950),
            expression: Is.Empty);
    }

    [Test]
    public void AfterTheStampIsRaised_NothingIsShownAgain()
    {
        (var _, int buildToStore) = UpgradeNotices.Evaluate(storedBuild: "9778", settingsAlreadyExist: true,
            currentBuild: 9910, notices: Notices);

        Assert.That(actual: Keys(storedBuild: buildToStore.ToString(), settingsAlreadyExist: true, currentBuild: 9910),
            expression: Is.Empty);
    }

    [Test]
    public void RunningAnOlderBuild_NeverLowersTheStamp()
    {
        int buildToStore = UpgradeNotices.Evaluate(storedBuild: "9910", settingsAlreadyExist: true, currentBuild: 9800,
            notices: Notices).BuildToStore;

        Assert.That(actual: buildToStore, expression: Is.EqualTo(expected: 9910));
    }
}
