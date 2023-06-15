using System;
using System.ComponentModel;
using Frends.Kungsbacka.Zip.Definitions;
using System.IO;
using System.IO.Compression;
using SevenZip;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1591

namespace Frends.Kungsbacka.Zip
{
    public static class ZipTasks
    {
        private static readonly string LIBRARY_FILE_PATH = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"7z.dll");

        /// <summary>
        /// Use this task to decompress/unpack .zip or .7z files.
        /// Documentation: https://github.com/hamkjeKBA/Frends.Kungsbacka.Zip
        /// </summary>
        /// <param name="input">Mandatory parameters</param>
        /// <param name="options">Optional parameters</param>
        /// <returns>bool</returns>
        public static UnpackZipFileResult UnpackZipFile([PropertyTab] UnpackZipInput input, [PropertyTab] UnpackZipFileOptions options)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.ZipFilePath is null) throw new ArgumentNullException(nameof(input.ZipFilePath));
            if (input.DestinationFolderPath is null) throw new ArgumentNullException(nameof(input.DestinationFolderPath));
            Exception ex = null;

            string fileExtension = Path.GetExtension(input.ZipFilePath).ToLower();

            if (fileExtension == ".zip")
            {
                try
                {
                    ZipFile.ExtractToDirectory(input.ZipFilePath, input.DestinationFolderPath);
                }
                catch (Exception e)
                {
                    ex = e;
                }
            }
            else if (fileExtension == ".7z")
            {
                try
                {
                    SevenZipBase.SetLibraryPath(LIBRARY_FILE_PATH);

                    using (var extractor = new SevenZipExtractor(input.ZipFilePath))
                    {
                        extractor.ExtractArchive(input.DestinationFolderPath);
                    }
                }
                catch (Exception e)
                {
                    ex = e;
                }
            }
            else
            {
                ex = new ArgumentException("Unsupported file format.");
            }

            if (options.ThrowErrorOnFailure && ex != null) throw ex;

            return new UnpackZipFileResult { Success = ex == null };
        }

        /// <summary>
        /// Use this task to locate all files within a .zip or .7z archive that matches the provided search string.
        /// Documentation: https://github.com/hamkjeKBA/Frends.Kungsbacka.Zip
        /// </summary>
        /// <param name="input">Mandatory parameters</param>
        /// <param name="options">Optional parameters</param>
        /// <returns>bool</returns>
        public static ExtractFilesBySearchStringResult ExtractFilesBySearchString([PropertyTab] ExtractFilesBySearchStringInput input, [PropertyTab] ExtractFilesBySearchStringOptions options)
        {
            var matchingFiles = new List<string>();
            var archivePaths = FindAllZipArchivesInFolder(new FindAllZipArchivesInFolderInput { FolderPath = input.FolderPath}, new FindAllZipArchivesInFolderOptions()).Archives;
            bool matchFound = false;

            foreach (var archivePath in archivePaths)
            {
                if (Path.GetExtension(archivePath).ToLower() == ".7z")
                {
                    using (var extractor = new SevenZipExtractor(archivePath))
                    {
                        foreach (var entry in extractor.ArchiveFileData)
                        {
                            if (entry.FileName.Contains(input.SearchPattern))
                            {
                                matchingFiles.Add(entry.FileName);
                                matchFound = true;

                                if (options.DoNotExtractToTargetDirectory)
                                    continue;

                                string targetFilePath = Path.Combine(input.TargetDirectory, entry.FileName);
                                extractor.ExtractFiles(input.TargetDirectory, entry.Index);
                            }

                            if (options.FirstMatchOnly && matchFound) break;
                        }
                    }
                }
                else if (Path.GetExtension(archivePath).ToLower() == ".zip")
                {
                    using (var archive = ZipFile.OpenRead(archivePath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName.Contains(input.SearchPattern))
                            {
                                matchingFiles.Add(entry.FullName);
                                matchFound = true;

                                if (options.DoNotExtractToTargetDirectory)
                                    continue;

                                string targetFilePath = Path.Combine(input.TargetDirectory, entry.FullName);
                                entry.ExtractToFile(targetFilePath);
                            }

                            if (options.FirstMatchOnly && matchFound) break;
                        }
                    }
                }

                if (options.FirstMatchOnly && matchFound) break;
            }

            return new ExtractFilesBySearchStringResult { IsSuccessful = true, MatchingFiles = matchingFiles};
        }

        /// <summary>
        /// Use this task to locate all archives within a folder. (Finds .zip and .7z archives)
        /// Documentation: https://github.com/hamkjeKBA/Frends.Kungsbacka.Zip
        /// </summary>
        /// <param name="input">Mandatory parameters</param>
        /// <param name="options">Optional parameters</param>
        /// <returns>bool</returns>
        public static FindAllZipArchivesInFolderResult FindAllZipArchivesInFolder([PropertyTab] FindAllZipArchivesInFolderInput input, [PropertyTab] FindAllZipArchivesInFolderOptions options)
        {
            var archiveNames = new List<string>();

            List<string> archiveFiles = Directory.GetFiles(input.FolderPath)
                .Where(f => f.ToLower().EndsWith(".7z") || f.ToLower().EndsWith(".zip"))
                .ToList();

            if (options.FileNamesOnly)
            {
                archiveFiles = archiveFiles.Select(zipArchive => Path.GetFileName(zipArchive)).ToList();
            }

            return new FindAllZipArchivesInFolderResult { Archives = archiveFiles };
        }
    }
}
