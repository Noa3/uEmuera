using System;
using System.IO;
using System.Text;
using MinorShift.Emuera.Sub;
using NUnit.Framework;

namespace MinorShift.Emuera.Tests.EditMode
{
    public class EncodingAndVirtualFileSystemTests
    {
        [Test]
        public void DetectEncoding_UsesUtf8AndCp932WithoutBom()
        {
            byte[] utf8 = new UTF8Encoding(false).GetBytes("猫");
            byte[] cp932 = EraEncoding.Cp932.GetBytes("猫");

            Assert.AreEqual("utf-8", EraEncoding.DetectBytes(utf8).WebName);
            Assert.AreEqual(EraEncoding.Cp932.WebName, EraEncoding.DetectBytes(cp932).WebName);
        }

        [Test]
        public void VirtualFileSystem_ReadTextUsesSameEncodingAsScanner()
        {
            string root = Path.Combine(Path.GetTempPath(), "uemuera-vfs-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                string path = Path.Combine(root, "sample.erb");
                File.WriteAllBytes(path, EraEncoding.Cp932.GetBytes("PRINTL 猫\r\n"));
                GameVirtualFileSystem vfs = new GameVirtualFileSystem(root);

                Assert.AreEqual("PRINTL 猫\r\n", vfs.ReadText("sample.erb"));
                Assert.IsTrue(vfs.TryResolve("sample.erb", out string resolved));
                Assert.AreEqual(Path.GetFullPath(path), resolved);
                Assert.IsFalse(vfs.TryResolve("..\\outside.erb", out _));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
