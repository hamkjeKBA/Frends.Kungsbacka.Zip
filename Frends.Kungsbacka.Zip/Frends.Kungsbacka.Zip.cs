using System;
using System.ComponentModel;
using Frends.Kungsbacka.Zip.Definitions;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO.Compression;

#pragma warning disable 1591

namespace Frends.Kungsbacka.Zip
{
    public static class ZipTasks
    {
        private static readonly string EXE_FILE_PATH = Path.Combine(@"C:\Program Files\7-Zip\", "7z.exe");

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
                    string arguments = $"x \"{input.ZipFilePath}\" -aoa -o\"{input.DestinationFolderPath}\" -y";
                    ProcessStartInfo startInfo = new ProcessStartInfo(EXE_FILE_PATH, arguments);
                    Process process = Process.Start(startInfo);
                    bool wasProcessSucessful = process.WaitForExit(options.SetTimeoutForUnpackOperation); 
                    if (!wasProcessSucessful)
                    {
                        throw new TimeoutException($"TimeoutExeption: The process of unpacking {Path.GetFileName(input.ZipFilePath)} took too long.");
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

            foreach (var archivePath in archivePaths)
            {
                if (Path.GetExtension(archivePath).ToLower() == ".7z")
                {
                    matchingFiles.AddRange(SevenZipExtractAndCollectMatchingFiles(archivePath, input, options));
                }
                else if (Path.GetExtension(archivePath).ToLower() == ".zip")
                {
                    matchingFiles.AddRange(ZipExtractAndCollectMatchingFiles(archivePath, input, options));
                }

                if (options.FirstMatchOnly && matchingFiles.Count > 0) break;
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

        private static List<string> SevenZipExtractAndCollectMatchingFiles(
            string archivePath, 
            ExtractFilesBySearchStringInput input, 
            ExtractFilesBySearchStringOptions options
        )
        {
            var matchingFiles = new List<string>();

            string command = $"l -slt \"{archivePath}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = EXE_FILE_PATH,
                Arguments = command,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(processStartInfo))
            {
                bool stopLookingForMatches = false;
             
                while (!process.StandardOutput.EndOfStream && !stopLookingForMatches)
                {
                    string line = process.StandardOutput.ReadLine();

                    if (!string.IsNullOrEmpty(line))
                    {
                        string entry = line.Trim();
                        if (entry.Contains(input.SearchPattern) && entry.ToUpperInvariant().Contains("PATH ="))
                        {
                            matchingFiles.Add(entry);

                            if (!options.DoNotExtractToTargetDirectory)
                            {
                                string extractCommand = $"e \"{archivePath}\" \"{entry}\" -o\"{input.TargetDirectory}\" -r";
                                var extractProcessStartInfo = new ProcessStartInfo
                                {
                                    FileName = EXE_FILE_PATH,
                                    Arguments = extractCommand,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    UseShellExecute = false
                                };

                                using (var extractProcess = Process.Start(extractProcessStartInfo))
                                {
                                    extractProcess.WaitForExit(options.SetTimeoutForUnpackOperation);
                                }
                            }

                            if (options.FirstMatchOnly) stopLookingForMatches = true;
                        }
                    }
                }

                process.WaitForExit(options.SetTimeoutForUnpackOperation);
            }

            return matchingFiles;
        }

        private static List<string> ZipExtractAndCollectMatchingFiles(string archivePath, ExtractFilesBySearchStringInput input, ExtractFilesBySearchStringOptions options)
        {
            var matchingFiles = new List<string>();

            using (var archive = ZipFile.OpenRead(archivePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.Contains(input.SearchPattern))
                    {
                        matchingFiles.Add(entry.FullName);

                        if (!options.DoNotExtractToTargetDirectory)
                        {
                            string targetFilePath = Path.Combine(input.TargetDirectory, entry.FullName);
                            entry.ExtractToFile(targetFilePath);
                        }
                    }

                    if (options.FirstMatchOnly) break;
                }
            }

            return matchingFiles;
        }
    }
}
