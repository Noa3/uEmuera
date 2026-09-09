using NUnit.Framework;
using uEmuera.Runtime.EraElectron;

namespace uEmuera.Tests.EditMode
{
    [TestFixture]
    public class EraElectronCompatibilityTests
    {
        [Test]
        public void EmptyRequirement_IsAllowed()
        {
            Assert.IsTrue(EraElectronCompatibility.CanRunEmbedded(
                string.Empty, out string reason));
            Assert.IsNull(reason);
        }

        [Test]
        public void CurrentTargetRequirement_IsAllowed()
        {
            Assert.IsTrue(EraElectronCompatibility.CanRunEmbedded(
                EraElectronCompatibility.EmulatedEngineVersion.ToString(),
                out string reason));
            Assert.IsNull(reason);
        }

        [Test]
        public void NewerRequirement_IsRejected()
        {
            string required =
                (EraElectronCompatibility.EmulatedEngineVersion + 1).ToString();

            Assert.IsFalse(EraElectronCompatibility.CanRunEmbedded(
                required, out string reason));
            StringAssert.Contains("requires EraElectron engine", reason);
        }

        [Test]
        public void InvalidRequirement_IsRejected()
        {
            Assert.IsFalse(EraElectronCompatibility.CanRunEmbedded(
                "not-a-version", out string reason));
            StringAssert.Contains("Invalid .ere-min-version", reason);
        }
    }
}
