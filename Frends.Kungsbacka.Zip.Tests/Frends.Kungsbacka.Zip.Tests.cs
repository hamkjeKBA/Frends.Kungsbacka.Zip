using NUnit.Framework;
using System;
using System.IO;
using System.IO.Compression;
using Frends.Kungsbacka.Zip.Definitions;
using SevenZip;

namespace Frends.Kungsbacka.Zip.Tests
{
    [TestFixture]
    public class UnpackZipFile
    {
        private string testFolderPath;
        private string LIBRARY_FILE_PATH = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"7z.dll");

        [SetUp]
        public void SetUp()
        {
            SevenZipBase.SetLibraryPath(LIBRARY_FILE_PATH);
            testFolderPath = Path.Combine(Path.GetTempPath(), "Unpack7zipTest");
            Directory.CreateDirectory(testFolderPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testFolderPath))
            {
                Directory.Delete(testFolderPath, true);
            }
        }

        [Test]
        public void UnpackZipFile_ValidZipFile_SuccessfullyUnpacksFiles()
        {
            // Arrange
            string zipFilePath = Path.Combine(testFolderPath, "archive.zip");
            string destinationFolderPath = Path.Combine(testFolderPath, "extracted");

            // Create a test zip file
            using (var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("file.txt");
                using (var writer = new StreamWriter(entry.Open()))
                {
                    writer.Write("Test content");
                }
            }

            // Act
            ZipTasks.UnpackZipFile(new UnpackZipInput { zipFilePath = zipFilePath, destinationFolderPath = destinationFolderPath });

            // Assert
            string extractedFilePath = Path.Combine(destinationFolderPath, "file.txt");
            Assert.IsTrue(File.Exists(extractedFilePath), "Extracted file does not exist.");
            string extractedContent = File.ReadAllText(extractedFilePath);
            Assert.AreEqual("Test content", extractedContent, "Extracted file content is incorrect.");
        }

        [Test]
        public void Unpack7z_Valid7zFile_SuccessfullyUnpacksFiles()
        {
            // Arrange
            string zipFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Test7ZipFile", "archive.7z");
            string destinationFolderPath = Path.Combine(testFolderPath, "extracted");

            // Extract the test .7z file to the destination folder
            using (var extractor = new SevenZipExtractor(zipFilePath))
            {
                extractor.ExtractArchive(destinationFolderPath);
            }

            // Assert
            string extractedFilePath = Path.Combine(destinationFolderPath, "test.txt");
            Assert.IsTrue(File.Exists(extractedFilePath), "Extracted file does not exist.");
            string extractedContent = File.ReadAllText(extractedFilePath);
            Assert.AreEqual("Test content", extractedContent, "Extracted file content is incorrect.");
        }

        [Test]
        public void UnpackZipFile_UnsupportedFileFormat_ThrowsArgumentException()
        {
            // Arrange
            string zipFilePath = Path.Combine(testFolderPath, "archive.rar");
            string destinationFolderPath = Path.Combine(testFolderPath, "extracted");

            // Create a test rar file
            File.WriteAllText(zipFilePath, "Test content");

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                ZipTasks.UnpackZipFile(new UnpackZipInput { zipFilePath = zipFilePath, destinationFolderPath = destinationFolderPath }));
        }
    }
}
