using Frends.Kungsbacka.Zip.Definitions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

#pragma warning disable 1591

namespace Frends.Kungsbacka.Zip
{
    public static class ZipTasks
    {
        private static readonly string EXE_FILE_FOLDER_PATH = @"C:\Program Files\7-Zip\";
        private static readonly string EXE_FILE_PATH = Path.Combine(@"C:\Program Files\7-Zip\", "7z.exe");

        /// <summary>
        /// Use this task to decompress/unpack .zip or .7z files.
        /// Documentation: https://github.com/hamkjeKBA/Frends.Kungsbacka.Zip
        /// </summary>
        /// <param name="input">Mandatory parameters</param>
        /// <param name="options">Optional parameters</param>
        /// <returns>(bool) Result.Success</returns>
        /// <returns>(List of strings) Result.SkippedFiles</returns>
        public static UnpackZipFileResult UnpackZipFile([PropertyTab] UnpackZipInput input, [PropertyTab] UnpackZipFileOptions options)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.ZipFilePath is null) throw new ArgumentNullException(nameof(input.ZipFilePath));
            if (input.DestinationFolderPath is null) throw new ArgumentNullException(nameof(input.DestinationFolderPath));
            Exception ex = null;

            string fileExtension = Path.GetExtension(input.ZipFilePath).ToLower();
            string skippedFile = String.Empty;

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
                if (options.SkipFileOnUnsupported)
                {
                    skippedFile = Path.GetFileName(input.ZipFilePath);
                }
                else
                {
                    ex = new ArgumentException($"Unsupported file format: \"{Path.GetFileName(input.ZipFilePath)}\".");
                }
            }

            if (options.ThrowErrorOnFailure && ex != null) throw ex;

            return new UnpackZipFileResult { Success = ex == null, SkippedFile = skippedFile };
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

            if (!Directory.Exists(input.TargetDirectory) && options.CreateTargetFolderIfMissing)
            {
                Directory.CreateDirectory(input.TargetDirectory);
            }

            var archivePaths = FindAllZipArchivesInFolder(new FindAllZipArchivesInFolderInput { FolderPath = input.FolderPath }, new FindAllZipArchivesInFolderOptions()).Archives;

            foreach (var archivePath in archivePaths)
            {
                if (Path.GetExtension(archivePath).ToLower() == ".7z" || Path.GetExtension(archivePath).ToLower() == ".zip")
                {
                    matchingFiles.AddRange(SevenZipExtractAndCollectMatchingFiles(archivePath, input, options));
                }

                if (options.FirstMatchOnly && matchingFiles.Count > 0) break;
            }

            return new ExtractFilesBySearchStringResult { IsSuccessful = true, MatchingFiles = matchingFiles };
        }

        /// <summary>
        /// Use this task to locate all archives within a folder. (Finds .zip and .7z archives)
        /// Documentation: https://github.com/hamkjeKBA/Frends.Kungsbacka.Zip
        /// </summary>
        /// <param name="input">Mandatory parameters</param>
        /// <param name="options">Optional parameters</param>
        /// <returns>Result{ Archives }</returns>
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

        /// <summary>
        /// Use this task to extract all archives within a source folder to a destination folder. (Extracts .zip and .7z archives)
        /// Documentation: https://github.com/hamkjeKBA/Frends.Kungsbacka.Zip
        /// </summary>
        /// <param name="input">Mandatory parameters</param>
        /// <param name="options">Optional parameters</param>
        /// <returns>Result{ IsSuccessful = bool }</returns>
        public static ExtractAllZipFilesInFolderResult ExtractAllZipArchives([PropertyTab] ExtractAllZipFilesInFolderInput input, [PropertyTab] ExtractAllZipFilesInFolderOptions options)
        {
            if (!Directory.Exists(input.SourceFolderPath))
            {
                throw new DirectoryNotFoundException("Source folder does not exist.");
            }

            if (!Directory.Exists(input.DestinationFolderPath))
            {
                throw new DirectoryNotFoundException("Destination folder does not exist.");
            }

            bool success = true;

            string arguments7z = $"x \"{Path.Combine(input.SourceFolderPath, "*.7z")}\" -o\"{input.DestinationFolderPath}\" -r";
            var startInfo7z = new ProcessStartInfo(EXE_FILE_PATH, arguments7z)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            string argumentsZip = $"x \"{Path.Combine(input.SourceFolderPath, "*.zip")}\"  -o\"{input.DestinationFolderPath}\" -r";
            var startInfoZip = new ProcessStartInfo(EXE_FILE_PATH, argumentsZip)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var processStartInfos = new List<ProcessStartInfo>() { startInfo7z, startInfoZip };

            try
            {
                foreach (var startInfoToRun in processStartInfos)
                {
                    using (Process process = Process.Start(startInfoToRun))
                    {
                        process.WaitForExit(options.SetTimeoutForUnpackOperation);
                    }
                }
            }
            catch (Exception e)
            {
                if (options.ThrowErrorOnFailure)
                {
                    throw e;
                }
                else
                {
                    success = false;
                }
            }

            return new ExtractAllZipFilesInFolderResult { IsSuccessful = success };
        }

        private static List<string> SevenZipExtractAndCollectMatchingFiles(
            string archivePath,
            ExtractFilesBySearchStringInput input,
            ExtractFilesBySearchStringOptions options
        )
        {
            var matchingFiles = new List<string>();
            string command = $"l -slt \"{archivePath}\" -y";

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
                Dictionary<WriteMode, string> writeModeLookup = new Dictionary<WriteMode, string> { { WriteMode.SKIP, "-aos -y" }, { WriteMode.RENAME, "-aou -y" }, { WriteMode.OVERWRITE, "-aoa -y" } };

                bool stopLookingForMatches = false;

                while (!process.StandardOutput.EndOfStream && !stopLookingForMatches)
                {
                    string line = process.StandardOutput.ReadLine();

                    if (!string.IsNullOrEmpty(line))
                    {
                        string entry = line.Trim();
                        if (entry.Contains(input.SearchPattern) && entry.ToUpperInvariant().Contains("PATH ="))
                        {
                            var trimmedEntry = entry.Substring(entry.LastIndexOf('=') + 1).Trim();
                            if (!options.DoNotExtractToTargetDirectory)
                            {
                                ICollection<PSObject> results;
                                string extractCommand = $"Invoke-Expression \".\\7z.exe e '{archivePath}' '{trimmedEntry}' -o'{input.TargetDirectory}' -r {writeModeLookup[options.WriteMode]}\"";

#if NETSTANDARD2_0
                                // Code specific to .NET Standard 2.0
                                using (PowerShell ps = PowerShell.Create())
                                {
                                    ps.AddScript($"cd \"{EXE_FILE_FOLDER_PATH}\"");
                                    ps.AddScript(extractCommand);
                                    results = ps.Invoke();
                                }
#else
                                // Code specific to .NET Framework 4.7.1
                                using (Runspace runspace = RunspaceFactory.CreateRunspace())
                                {
                                    runspace.Open();

                                    using (Pipeline pipeline = runspace.CreatePipeline())
                                    {
                                        pipeline.Commands.AddScript($"cd \"{EXE_FILE_FOLDER_PATH}\"");
                                        pipeline.Commands.AddScript(extractCommand);
                                        results = pipeline.Invoke();
                                    }
                                }
#endif
                                if (results.Any(r => r.ToString().Contains("Everything is Ok")))
                                {
                                    matchingFiles.Add(entry);
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
    }
}
