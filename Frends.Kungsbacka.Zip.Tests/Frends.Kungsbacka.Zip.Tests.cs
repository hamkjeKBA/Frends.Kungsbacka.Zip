using NUnit.Framework;
using System;
using System.IO;
using System.IO.Compression;
using Frends.Kungsbacka.Zip.Definitions;
using System.Diagnostics;
using System.Threading.Tasks;

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
            var options = new UnpackZipFileOptions() { SkipFileOnUnsupported = false };

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
        [SetUp]
        public new void SetUp()
        {
            string file1Path = Path.Combine(TEST_FOLDER_PATH, "file.txt");
            string file2Path = Path.Combine(TEST_FOLDER_PATH, "text.txt");
            string archivePath = Path.Combine(TEST_FOLDER_PATH, "Test7ZipFile.7z");

            File.WriteAllText(file1Path, "This is a sample file.");
            File.WriteAllText(file2Path, "This is another file.");

            string command = $"a \"{archivePath}\" \"{file1Path}\" \"{file2Path}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = @"C:\Program Files\7-Zip\7z.exe",
                Arguments = command,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(processStartInfo))
            {
                process.WaitForExit();
            }

            File.Delete(file1Path);
            File.Delete(file2Path);
        }

        [TearDown]
        public new void TearDown()
        {
            if (Directory.Exists(TEST_FOLDER_PATH))
            {
                string filePath = Path.Combine(TEST_FOLDER_PATH, "Test7ZipFile.7z");
                int maxRetries = 5;
                int retryDelayMilliseconds = 500;

                bool fileDeleted = false;

                while (!fileDeleted && 0 < maxRetries)
                {
                    try
                    {
                        File.Delete(filePath);
                        fileDeleted = true;
                    }
                    catch (IOException)
                    {
                        Task.Delay(retryDelayMilliseconds).Wait();
                        maxRetries--;
                    }
                }

                if (!fileDeleted)
                {
                    throw new Exception("Deletion of test folder failed. Probably due to the 7zip console not having released the test archive yet.");
                }
            }
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
                archive.CreateEntry("file1.txt");
                archive.CreateEntry("file2.txt");
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

        [Test]
        public void ExtractFilesBySearchString_ReturnsAllMatchingFiles()
        {
            // Arrange
            // Create a test zip archive
            using (var archive = ZipFile.Open(Path.Combine(TEST_FOLDER_PATH, "ExtractFilesBySearchString_FirstMatchOnlyOption_ReturnsFirstMatchingFileOnly_ZIPArchive.zip"), ZipArchiveMode.Create))
            {
                archive.CreateEntry("nomatch.txt");
                archive.CreateEntry("file1.txt");
                archive.CreateEntry("file2.txt");
            }

            var input = new ExtractFilesBySearchStringInput
            {
                FolderPath = TEST_FOLDER_PATH,
                SearchPattern = "file",
                TargetDirectory = TEST_FOLDER_PATH
            };
            var options = new ExtractFilesBySearchStringOptions();

            // Act
            var result = ZipTasks.ExtractFilesBySearchString(input, options);

            // Assert
            Assert.IsTrue(result.IsSuccessful);
            Assert.IsNotNull(result.MatchingFiles);
            Assert.AreEqual(3, result.MatchingFiles.Count);
        }

        [Test]
        public void ExtractAllZipArchives_SuccessfulExtraction()
        {
            // Arrange
            string sourceFolderPath = TEST_FOLDER_PATH;
            string destinationFolderPath = TEST_FOLDER_PATH;
            var input = new ExtractAllZipFilesInFolderInput { SourceFolderPath = sourceFolderPath, DestinationFolderPath = destinationFolderPath };
            var options = new ExtractAllZipFilesInFolderOptions { SetTimeoutForUnpackOperation = 5000 };

            // Act
            var result = ZipTasks.ExtractAllZipArchives(input, options);

            // Assert
            Assert.True(Directory.Exists(destinationFolderPath));
            Assert.True(result.IsSuccessful);
        }

        [Test]
        public void ExtractAllZipArchives_SourceFolderDoesNotExist_ThrowDirectoryNotFoundException()
        {
            // Arrange
            string sourceFolderPath = "Fel Folder";
            string destinationFolderPath = TEST_FOLDER_PATH;
            var input = new ExtractAllZipFilesInFolderInput { SourceFolderPath = sourceFolderPath, DestinationFolderPath = destinationFolderPath };
            var options = new ExtractAllZipFilesInFolderOptions();

            // Act 
            var result = ZipTasks.ExtractAllZipArchives(input, options);

            //Assert
            Assert.AreEqual(true, result.IsSuccessful);
        }

        [Test]
        public void ExtractAllZipArchives_DestinationFolderDoesNotExist_ThrowDirectoryNotFoundException()
        {
            // Arrange
            string sourceFolderPath = TEST_FOLDER_PATH;
            string destinationFolderPath = @"C:\Path\To\Nonexistant\Directory\";
            var input = new ExtractAllZipFilesInFolderInput { SourceFolderPath = sourceFolderPath, DestinationFolderPath = destinationFolderPath };
            var options = new ExtractAllZipFilesInFolderOptions();

            //Act & Assert
            Assert.Throws< DirectoryNotFoundException>(() => ZipTasks.ExtractAllZipArchives(input, options));
        }
    }
}
