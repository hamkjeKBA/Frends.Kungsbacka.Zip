using NUnit.Framework;
using System;
using System.IO;
using System.IO.Compression;
using Frends.Kungsbacka.Zip.Definitions;
using SevenZip;

namespace Frends.Kungsbacka.Zip.Tests
{
    public class BaseTestSetUpAndTearDown
    {
        public string TEST_FOLDER_PATH { get; private set; }

        [SetUp]
        public void SetUp()
        {
            TEST_FOLDER_PATH = Path.Combine(Path.GetTempPath(), TestContext.CurrentContext.Test.Name);
            Directory.CreateDirectory(TEST_FOLDER_PATH);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(TEST_FOLDER_PATH))
            {
                Directory.Delete(TEST_FOLDER_PATH, true);
            }
        }
    }

    [TestFixture]
    public class UnpackZipFile : BaseTestSetUpAndTearDown
    {
        [Test]
        public void UnpackZipFile_ValidZipFile_SuccessfullyUnpacksFiles()
        {
            // Arrange
            var input = new UnpackZipInput
            {
                ZipFilePath = Path.Combine(TEST_FOLDER_PATH, "archive.zip"),
                DestinationFolderPath = Path.Combine(TEST_FOLDER_PATH, "extracted")
            };
            var options = new UnpackZipFileOptions { ThrowErrorOnFailure = false };

            // Create a test zip file
            using (var archive = ZipFile.Open(input.ZipFilePath, ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("file.txt");
                using (var writer = new StreamWriter(entry.Open()))
                {
                    writer.Write("Test content");
                }
            }

            // Act
            bool result = ZipTasks.UnpackZipFile(input, options).Success;

            // Assert
            string extractedFilePath = Path.Combine(input.DestinationFolderPath, "file.txt");
            Assert.IsTrue(File.Exists(extractedFilePath), "Extracted file does not exist.");
            string extractedContent = File.ReadAllText(extractedFilePath);
            Assert.AreEqual("Test content", extractedContent, "Extracted file content is incorrect.");
            Assert.AreEqual(true, result, "UnpackZipInput successfully extracted but did not return true.");
        }

        [Test]
        public void UnpackZipFile_UnsupportedFileFormat_ThrowsArgumentException()
        {
            // Arrange
            var input = new UnpackZipInput
            {
                ZipFilePath = "TestFilePath.txt",
                DestinationFolderPath = TEST_FOLDER_PATH
            };
            var options = new UnpackZipFileOptions();

            // Act
            Assert.Throws<ArgumentException>(() => ZipTasks.UnpackZipFile(input, options));
        }

        [Test]
        public void UnpackZipFile_WithThrowErrorOnFailureAndExtractionError_ThrowsException()
        {
            // Arrange
            var input = new UnpackZipInput
            {
                ZipFilePath = Path.Combine(TEST_FOLDER_PATH, "notanarchive.zip"),
                DestinationFolderPath = Path.Combine(TEST_FOLDER_PATH, "wrongpath")
            };
            var options = new UnpackZipFileOptions { ThrowErrorOnFailure = true };

            // Act & Assert
            Exception caughtException = null;
            try
            {
                ZipTasks.UnpackZipFile(input, options);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            Assert.IsNotNull(caughtException, "Expected an exception to be thrown.");
        }

        [Test]
        public void UnpackZipFile_WithoutThrowErrorOnFailureAndExtractionError_ThrowsException()
        {
            // Arrange
            var input = new UnpackZipInput
            {
                ZipFilePath = Path.Combine(TEST_FOLDER_PATH, "notanarchive.zip"),
                DestinationFolderPath = Path.Combine(TEST_FOLDER_PATH, "wrongpath")
            };
            var options = new UnpackZipFileOptions { ThrowErrorOnFailure = false };

            // Act 
            bool result = ZipTasks.UnpackZipFile(input, options).Success;

            // Assert
            Assert.AreEqual(false, result, "UnpackZipInput should return false.");
        }

        [Test]
        public void ExtractFilesBySearchString_WithValidInput_ReturnsMatchingFiles()
        {
            // Arrange
            // Create a test zip file
            using (var archive = ZipFile.Open(Path.Combine(TEST_FOLDER_PATH, "ExtractFilesBySearchString_WithValidInput_ReturnsMatchingFiles_Archive.zip"), ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("ExtractFilesBySearchString_WithValidInput_ReturnsMatchingFiles.txt");
                using (var writer = new StreamWriter(entry.Open()))
                {
                    writer.Write("Test content");
                }
            }

            var input = new ExtractFilesBySearchStringInput
            {
                FolderPath = TEST_FOLDER_PATH,
                SearchPattern = "ySearchString_WithValidInput_ReturnsMatc",
                TargetDirectory = TEST_FOLDER_PATH
            };
            var options = new ExtractFilesBySearchStringOptions();

            // Act
            var result = ZipTasks.ExtractFilesBySearchString(input, options);

            // Assert
            Assert.IsTrue(result.IsSuccessful);
            Assert.IsNotNull(result.MatchingFiles);
            Assert.Greater(result.MatchingFiles.Count, 0);
        }

        [Test]
        public void ExtractFilesBySearchString_WithNoMatchingFiles_ReturnsEmptyList()
        {
            // Arrange
            var input = new ExtractFilesBySearchStringInput
            {
                FolderPath = TEST_FOLDER_PATH,
                SearchPattern = "nonexistent",
                TargetDirectory = TEST_FOLDER_PATH
            };
            var options = new ExtractFilesBySearchStringOptions();

            // Act
            var result = ZipTasks.ExtractFilesBySearchString(input, options);

            // Assert
            Assert.IsTrue(result.IsSuccessful);
            Assert.IsNotNull(result.MatchingFiles);
            Assert.AreEqual(0, result.MatchingFiles.Count);
        }

        [Test]
        public void ExtractFilesBySearchString_WithInvalidFolderPath_ThrowsException()
        {
            // Arrange
            var input = new ExtractFilesBySearchStringInput
            {
                FolderPath = @"C:\NonExistentFolder",
                SearchPattern = "file",
                TargetDirectory = TEST_FOLDER_PATH
            };
            var options = new ExtractFilesBySearchStringOptions();

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => ZipTasks.ExtractFilesBySearchString(input, options));
        }

        [Test]
        public void FindAllZipArchivesInFolder_WithInvalidFolderPath_ThrowsException()
        {
            // Arrange
            var input = new FindAllZipArchivesInFolderInput
            {
                FolderPath = @"C:\NonExistentFolder"
            };
            var options = new FindAllZipArchivesInFolderOptions();

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => ZipTasks.FindAllZipArchivesInFolder(input, options));
        }
    }

    public class SevenZipTestsSetUpAndTearDown : BaseTestSetUpAndTearDown
    {
        public readonly string LIBRARY_FILE_PATH = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"7z.dll");
        public SevenZipCompressor compressor = new SevenZipCompressor();

        [SetUp]
        public new void SetUp()
        {
            SevenZipBase.SetLibraryPath(LIBRARY_FILE_PATH);
            compressor.ArchiveFormat = OutArchiveFormat.SevenZip;
            File.WriteAllText(Path.Combine(TEST_FOLDER_PATH, "file.txt"), "This is a sample file.");
            File.WriteAllText(Path.Combine(TEST_FOLDER_PATH, "text.txt"), "This is another file.");
            compressor.CompressFiles
            (
                Path.Combine(TEST_FOLDER_PATH, "Test7ZipFile.7z"),
                new string[] 
                {
                    Path.Combine(TEST_FOLDER_PATH, "file.txt"),
                    Path.Combine(TEST_FOLDER_PATH, "text.txt") 
                }
            );

            File.Delete(Path.Combine(TEST_FOLDER_PATH, "file.txt"));
            File.Delete(Path.Combine(TEST_FOLDER_PATH, "text.txt"));
        }
    }

    [TestFixture]
    public class SevenZipTests : SevenZipTestsSetUpAndTearDown
    {
        [Test]
        public void FindAllZipArchivesInFolder_WithValidInput_ReturnsArchiveNames()
        {
            // Arrange
            // Create test zip files
            using (var archive = ZipFile.Open(Path.Combine(TEST_FOLDER_PATH, "FindAllZipArchivesInFolder_WithValidInput_ReturnsArchiveNames_Archive1.zip"), ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("ExtractFilesBySearchString_WithValidInput_ReturnsArchiveNames.txt");
                using (var writer = new StreamWriter(entry.Open()))
                {
                    writer.Write("Test content");
                }
            }

            using (var archive = ZipFile.Open(Path.Combine(TEST_FOLDER_PATH, "FindAllZipArchivesInFolder_WithValidInput_ReturnsArchiveNames_Archive2.zip"), ZipArchiveMode.Create))
            {
                var entry = archive.CreateEntry("ExtractFilesBySearchString_WithValidInput_ReturnsArchiveNames.txt");
                using (var writer = new StreamWriter(entry.Open()))
                {
                    writer.Write("Test content");
                }
            }

            var input = new FindAllZipArchivesInFolderInput
            {
                FolderPath = TEST_FOLDER_PATH
            };
            var options = new FindAllZipArchivesInFolderOptions();

            // Act
            var result = ZipTasks.FindAllZipArchivesInFolder(input, options);

            // Assert
            Assert.IsNotNull(result.Archives);
            Assert.AreEqual(3, result.Archives.Count);
        }

        [Test]
        public void Unpack7z_Valid7zFile_SuccessfullyUnpacksFiles()
        {
            // Arrange
            var input = new UnpackZipInput
            {
                ZipFilePath = Path.Combine(TEST_FOLDER_PATH, "Test7ZipFile.7z"),
                DestinationFolderPath = Path.Combine(TEST_FOLDER_PATH, "extracted")
            };
            var options = new UnpackZipFileOptions { ThrowErrorOnFailure = false };

            // Act
            bool result = ZipTasks.UnpackZipFile(input, options).Success;

            // Assert
            Assert.IsTrue(File.Exists(Path.Combine(input.DestinationFolderPath, "file.txt")), "Extracted file does not exist.");
            Assert.IsTrue(File.Exists(Path.Combine(input.DestinationFolderPath, "text.txt")), "Extracted file does not exist.");
            Assert.AreEqual("This is a sample file.", File.ReadAllText(Path.Combine(input.DestinationFolderPath, "file.txt")), "Extracted file \"file.txt\"'s content is incorrect.");
            Assert.AreEqual("This is another file.", File.ReadAllText(Path.Combine(input.DestinationFolderPath, "text.txt")), "Extracted file \"text.txt\"'s content is incorrect.");
            Assert.AreEqual(true, result, "UnpackZipInput successfully extracted but did not return true.");
        }

        [Test]
        public void ExtractFilesBySearchString_FirstMatchOnlyOption_ReturnsFirstMatchingFileOnly()
        {
            // Arrange
            // Create a test zip archive
            using (var archive = ZipFile.Open(Path.Combine(TEST_FOLDER_PATH, "ExtractFilesBySearchString_FirstMatchOnlyOption_ReturnsFirstMatchingFileOnly_ZIPArchive.zip"), ZipArchiveMode.Create))
            {
                archive.CreateEntry("nomatch.txt");
                archive.CreateEntry("file.txt");
                archive.CreateEntry("file.txt");
            }

            var input = new ExtractFilesBySearchStringInput
            {
                FolderPath = TEST_FOLDER_PATH,
                SearchPattern = "file",
                TargetDirectory = TEST_FOLDER_PATH
            };
            var options = new ExtractFilesBySearchStringOptions
            {
                FirstMatchOnly = true
            };

            // Act
            var result = ZipTasks.ExtractFilesBySearchString(input, options);

            // Assert
            Assert.IsTrue(result.IsSuccessful);
            Assert.IsNotNull(result.MatchingFiles);
            Assert.AreEqual(1, result.MatchingFiles.Count);
        }
    }
}
