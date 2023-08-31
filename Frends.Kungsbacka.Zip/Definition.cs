#pragma warning disable 1591

using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.Kungsbacka.Zip.Definitions
{
    /// <summary>
    /// Input for tasks that unpack zip files. Specifies zip file path and unpack destinationfolder.
    /// </summary>
    public class UnpackZipInput
    {
        /// <summary>
        /// Sets path to file to unpack.
        /// </summary>
        [DisplayFormat(DataFormatString = "Expression")]
        [DefaultValue("@")]
        public string ZipFilePath { get; set; }

        /// <summary>
        /// Sets destination of unpacked files.
        /// </summary>
        [DisplayFormat(DataFormatString = "Expression")]
        [DefaultValue("@")]
        public string DestinationFolderPath { get; set; }
    }

    public class UnpackZipFileOptions
    {
        /// <summary>
        /// Set timeout in milliseconds. 
        /// If set below 1ms, defaults to 5sec (5000).
        /// </summary>
        private int _setTimeoutForUnpackOperation;
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue(5000)]
        public int SetTimeoutForUnpackOperation
        {
            get
            {
                return _setTimeoutForUnpackOperation <= 0 ? 5000 : _setTimeoutForUnpackOperation;
            }
            set
            {
                _setTimeoutForUnpackOperation = value;
            }
        }

        /// <summary>
        /// Choose if error should be thrown if Task failes.
        /// Otherwise, on error, returns false instead.
        /// </summary>
        [DefaultValue(true)]
        public bool ThrowErrorOnFailure { get; set; } = true;

        /// <summary>
        /// Choose if error should be thrown if file format is unsupported.
        /// Otherwise, on unsupported, simply skips the file and logs it to the return value "SkippedFiles".
        /// </summary>
        [DefaultValue(true)]
        public bool SkipFileOnUnsupported { get; set; } = true;
    }

    public class UnpackZipFileResult
    {
        /// <summary>
        /// Contains the input repeated the specified number of times.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Contains the input repeated the specified number of times.
        /// </summary>
        public string SkippedFile { get; set; }
    }

    public class ExtractAllZipFilesInFolderInput
    {
        public ExtractAllZipFilesInFolderInput() { }

        /// <summary>
        /// Where to look for zip/7z-archives.
        /// </summary>
        [DisplayFormat(DataFormatString = "Expression")]
        [DefaultValue("@")]
        public string SourceFolderPath { get; set; }

        /// <summary>
        /// Sets destination of unpacked files.
        /// </summary>
        [DisplayFormat(DataFormatString = "Expression")]
        [DefaultValue("@")]
        public string DestinationFolderPath { get; set; }
    }

    public class ExtractAllZipFilesInFolderOptions
    { 
        /// <summary>
        /// Set timeout in milliseconds. 
        /// If set below 1ms, defaults to 5sec (5000).
        /// </summary>
        private int _setTimeoutForUnpackOperation;
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue(5000)]
        public int SetTimeoutForUnpackOperation
        {
            get
            {
                return _setTimeoutForUnpackOperation <= 0 ? 5000 : _setTimeoutForUnpackOperation;
            }
            set
            {
                _setTimeoutForUnpackOperation = value;
            }
        }

        /// <summary>
        /// Choose if error should be thrown if Task failes.
        /// Otherwise, on error, returns false instead.
        /// </summary>
        [DefaultValue(true)]
        public bool ThrowErrorOnFailure { get; set; } = true;

        /// <summary>
        /// Choose if error should be thrown if file format is unsupported.
        /// Otherwise, on unsupported, simply skips the file and logs it to the return value "SkippedFiles".
        /// </summary>
        [DefaultValue(true)]
        public bool SkipFileOnUnsupported { get; set; } = true;
    }

    public class ExtractAllZipFilesInFolderResult
    {
        /// <summary>
        /// A boolean flag indicating whether the extraction process was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }
    }

    public class ExtractFilesBySearchStringInput
    {
        /// <summary>
        /// A string representing the path of the folder to search for zip or 7zip archives.
        /// </summary>
        public string FolderPath { get; set; }

        /// <summary>
        /// A string representing the search pattern to match against the filenames in the archives.
        /// </summary>
        public string SearchPattern { get; set; }

        /// <summary>
        /// A string representing the path of the target directory where the matching files will be extracted.
        /// </summary>
        public string TargetDirectory { get; set; }
    }

    public class ExtractFilesBySearchStringOptions
    {
        /// <summary>
        /// Set timeout in milliseconds. 
        /// If set below 1ms, defaults to 5sec (5000).
        /// </summary>
        private int _setTimeoutForUnpackOperation;
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue(5000)]
        public int SetTimeoutForUnpackOperation
        {
            get
            {
                return _setTimeoutForUnpackOperation <= 0 ? 5000 : _setTimeoutForUnpackOperation;
            }
            set
            {
                _setTimeoutForUnpackOperation = value;
            }
        }

        /// <summary>
        /// Return only the first match found.
        /// </summary>
        [DefaultValue(false)]
        public bool FirstMatchOnly { get; set; } = false;

        /// <summary>
        /// If the specified target path does not exist, then create it.
        /// </summary>
        [DefaultValue(true)]
        public bool CreateTargetFolderIfMissing { get; set; }

        /// <summary>
        /// Use this option to skip the extraction step. Task will just return a list of found files.
        /// </summary>
        [DefaultValue(false)]
        public bool DoNotExtractToTargetDirectory { get; set; } = false;

        /// <summary>
        /// Use this option to overwrite files with same name. If false, will throw exception on if file allready exists.
        /// </summary>
        [DefaultValue(true)]
        public bool OverwriteEnabled { get; set; } = true;
    }

    public class ExtractFilesBySearchStringResult
    {
        /// <summary>
        /// A boolean flag indicating whether the extraction process was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// A list of strings representing the filenames of the matching files found during the extraction process.
        /// </summary>
        public List<string> MatchingFiles { get; set; }
    }

    public class FindAllZipArchivesInFolderInput
    {
        /// <summary>
        /// Where to look for zip/7z-archives.
        /// </summary>
        [DisplayFormat(DataFormatString = "Expression")]
        [DefaultValue("@")]
        public string FolderPath { get; set; }
    }

    public class FindAllZipArchivesInFolderOptions
    {
        /// <summary>
        /// Use this option to remove path and only return the name of the archives.
        /// </summary>
        [DefaultValue(false)]
        public bool FileNamesOnly { get; set; }
    }

    public class FindAllZipArchivesInFolderResult 
    {
        /// <summary>
        /// List of the names of found archives.
        /// </summary>
        public List<string> Archives { get; set; }
    }
}
