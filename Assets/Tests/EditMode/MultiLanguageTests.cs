using NUnit.Framework;

public class MultiLanguageTests
{
    [Test]
    public void DecodeEscapes_ConvertsLiteralNewlines()
    {
        Assert.AreEqual(
            "First line\nSecond line",
            MultiLanguage.DecodeEscapes("First line\\nSecond line"));
    }

    [Test]
    public void DecodeEscapes_ConvertsTabsAndCarriageReturns()
    {
        Assert.AreEqual(
            "A\tB\rC",
            MultiLanguage.DecodeEscapes("A\\tB\\rC"));
    }

    [Test]
    public void DecodeEscapes_LeavesPlainTextUnchanged()
    {
        Assert.AreEqual("plain text", MultiLanguage.DecodeEscapes("plain text"));
    }
}
