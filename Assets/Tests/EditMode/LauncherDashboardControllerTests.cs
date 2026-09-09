using NUnit.Framework;
using uEmuera.Runtime;

public class LauncherDashboardControllerTests
{
    [Test]
    public void MatchesSearch_IsCaseInsensitive()
    {
        Assert.IsTrue(LauncherDashboardController.MatchesSearch(
            "EraUma\nEraElectron\nC:/Games/EraUma", "eraelectron"));
        Assert.IsTrue(LauncherDashboardController.MatchesSearch(
            "EraUma\nEraElectron\nC:/Games/EraUma", "ERAUMA"));
    }

    [Test]
    public void MatchesSearch_EmptyQueryShowsEverything()
    {
        Assert.IsTrue(LauncherDashboardController.MatchesSearch("Anything", ""));
    }

    [Test]
    public void DescribeStatus_EmueraCertainIsReady()
    {
        var game = new GameDescriptor
        {
            RuntimeKind = RuntimeKind.Emuera,
            DetectionResult = new DetectionResult
            {
                Confidence = DetectionConfidence.Certain,
            },
        };

        StringAssert.Contains("Ready", LauncherDashboardController.DescribeStatus(game));
    }

    [Test]
    public void DescribeStatus_EraElectronIsMarkedExperimental()
    {
        var game = new GameDescriptor
        {
            RuntimeKind = RuntimeKind.EraElectron,
            DetectionResult = new DetectionResult
            {
                Confidence = DetectionConfidence.Certain,
            },
        };

        StringAssert.Contains("Experimental",
            LauncherDashboardController.DescribeStatus(game));
    }

    [Test]
    public void DescribeStatus_WarningsAreVisible()
    {
        var game = new GameDescriptor
        {
            RuntimeKind = RuntimeKind.Emuera,
            DetectionResult = new DetectionResult
            {
                Confidence = DetectionConfidence.High,
                Warnings = { "fixture warning" },
            },
        };

        StringAssert.Contains("1 warning",
            LauncherDashboardController.DescribeStatus(game));
    }
}
