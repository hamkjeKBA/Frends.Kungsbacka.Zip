using System;
using System.ComponentModel;
using Frends.Kungsbacka.Zip.Definitions;
using System.IO;
using System.IO.Compression;
using SevenZip;

#pragma warning disable 1591

namespace Frends.Kungsbacka.Zip
{
    public static class ZipTasks
    {
        private static readonly string LIBRARY_FILE_PATH = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"7z.dll");

        /// <summary>
        /// Use this task to decompress/unpack .zip or .7z files.
        /// Documentation: https://github.com/CommunityHiQ/Frends.Kungsbacka.Zip
        /// </summary>
        /// <param name="input">Mandatory parameters</param>
        public static void UnpackZipFile([PropertyTab] UnpackZipInput input)
        {
            if (input is null) throw new ArgumentNullException(nameof(input));
            if (input.zipFilePath is null) throw new ArgumentNullException(nameof(input.zipFilePath));
            if (input.destinationFolderPath is null) throw new ArgumentNullException(nameof(input.destinationFolderPath));

            string fileExtension = Path.GetExtension(input.zipFilePath).ToLower();

            if (fileExtension == ".zip")
            {
                ZipFile.ExtractToDirectory(input.zipFilePath, input.destinationFolderPath);
            }
            else if (fileExtension == ".7z")
            {
                SevenZipBase.SetLibraryPath(LIBRARY_FILE_PATH);

                using (var extractor = new SevenZipExtractor(input.zipFilePath))
                {
                    extractor.ExtractArchive(input.destinationFolderPath);
                }
            }
            else
            {
                throw new ArgumentException("Unsupported file format.");
            }
        }
    }
}
