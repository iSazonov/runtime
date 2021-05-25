// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.IO
{
    public sealed class TemporaryDirectory : IDisposable
    {
        private static string? s_tempDirPrefix;

        private const string DefaultTempFileExtension = ".tmp";
        private readonly List<string>? _fileNameList;

        public TemporaryDirectory() : this(Path.GetTempPath(), storeFileNames: false)
        {
        }

        public TemporaryDirectory(string tempDir) : this(tempDir, storeFileNames: false)
        {
        }

        public TemporaryDirectory(bool storeFileNames) : this(Path.GetTempPath(), storeFileNames)
        {
        }

        public TemporaryDirectory(string tempDir, bool storeFileNames)
        {
            if (tempDir is null)
                throw new ArgumentNullException(nameof(tempDir));

            StoreFileNames = storeFileNames;
            BaseTemporaryDirectory = Path.Combine(tempDir, TemporaryDirectoryPrefix + Environment.TickCount64.ToString());
            Directory.CreateDirectory(BaseTemporaryDirectory);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            SafeDelete();
        }

        ~TemporaryDirectory()
        {
            Dispose(false);
        }

        /// <summary>
        /// Temporary directory prefix common for the application.
        /// Default value is a name of the application entry assembly.
        /// </summary>
        public static string TemporaryDirectoryPrefix
        {
            get
            {
                if (s_tempDirPrefix is null)
                {
                    s_tempDirPrefix = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name + "_";
                }

                return s_tempDirPrefix;
            }

            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                s_tempDirPrefix = value;
            }
        }

        /// <summary>
        /// Creates the file stream for the temporary file.
        /// The temporary file is created in the user profile temporary directory.
        /// </summary>
        /// <param name="keepFile">Don't remove the temporary file if true</param>
        /// <returns>The <see cref="FileStream"/> for the temporary file.</returns>
        /// <example>
        /// <code>
        ///   using var tempFileStream = TemporaryDirectory.CreateTempFileStream()
        ///   {
        ///       // Working with the temp file.
        ///       tempFileStream.Write(...);
        ///       ...
        ///       tempFileStream.Read(...);
        ///       ...
        ///   } // Here the stream is closed and the temp file is deleted.
        /// </code>
        /// </example>
        /// <example>
        /// <code>
        ///   string tempFileName;
        ///   using var tempFileStream = TemporaryDirectory.CreateTempFileStream(keepFile: true)
        ///   {
        ///       // Working with the temp file.
        ///       tempFileStream.Write(...);
        ///       ...
        ///       tempFileName = tempFileStream.Name;
        ///       ...
        ///   } // Here the stream is closed but the file is not deleted
        ///     // and the tempFileName presents in FileNameList.
        ///   ...
        ///   File.Move(tempFileName, targetFile);
        /// </code>
        /// </example>
        public static FileStream CreateTempFileStream(bool keepFile = false)
        {
            var fileName = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            return new FileStream(
                fileName,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 4096,
                keepFile ? FileOptions.None : FileOptions.DeleteOnClose);
        }

        /// <summary>
        /// Creates the file stream for the temporary file.
        /// The temporary file is created in the user profile temporary directory.
        /// </summary>
        /// <param name="fileExtension">The extension for the temporary file name.</param>
        /// <param name="keepFile">Don't remove the temporary file if true</param>
        /// <returns>The <see cref="FileStream"/> for the temporary file.</returns>
        public static FileStream CreateTempFileStream(string fileExtension, bool keepFile = false)
        {
            var fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + fileExtension);
            return new FileStream(
                fileName,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 4096,
                keepFile ? FileOptions.None : FileOptions.DeleteOnClose);
        }

        /// <summary>
        /// Creates the file stream with custom options for the temporary file.
        /// The temporary file is created in the user profile temporary directory.
        /// </summary>
        /// <param name="func">Custom function for creating <see cref="FileStream"/></param>
        /// <param name="keepFile">
        /// <c>true</c> if the file should be kept after use;
        /// <c>false</c> if the file should be deleted.
        /// </param>
        /// <returns>The <see cref="FileStream"/> for the temporary file.</returns>
        public static FileStream CreateTempFileStream(CreateTempFileStreamFunc func, bool keepFile = false)
        {
            var fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            return func(fileName, keepFile);
        }

        /// <summary>
        /// Creates the file stream with custom options for the temporary file.
        /// The temporary file is created in the user profile temporary directory.
        /// </summary>
        /// <param name="func">Custom function for creating <see cref="FileStream"/></param>
        /// <param name="fileExtension">The extension for the temporary file name.</param>
        /// <param name="keepFile">
        /// <c>true</c> if the file should be kept after use;
        /// <c>false</c> if the file should be deleted.
        /// </param>
        /// <returns>The <see cref="FileStream"/> for the temporary file.</returns>
        public static FileStream CreateTempFileStream(CreateTempFileStreamFunc func, string fileExtension, bool keepFile = false)
        {
            var fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + fileExtension);
            return func(fileName, keepFile);
        }

        /// <summary>
        /// Delegate for creating a temporary file stream with custom options.
        /// </summary>
        public delegate FileStream CreateTempFileStreamFunc(string fileName, bool keepFile);

        /// <summary>
        /// Gets the file stream for a temporary file in <see cref="BaseTemporaryDirectory"/>
        /// and store the file name in <see cref="FileNameList"/> if <see cref="StoreFileNames"/> is <c>true</c>.
        /// </summary>
        public FileStream GetTempFileNameStream() => GetTempFileNameStream(StoreFileNames);

        /// <summary>
        /// Gets the file stream for a temporary file in <see cref="BaseTemporaryDirectory"/>.
        /// </summary>
        /// <param name="keepFile">
        /// <c>true</c> if the file should be kept after use; also store the file name in <see cref="FileNameList"/>;
        /// <c>false</c> if the file should be deleted.
        /// </param>
        /// <returns>The <see cref="FileStream"/> for the temporary file.</returns>
        public FileStream GetTempFileNameStream(bool keepFile)
        {
            var fileName = GetTempFileName(DefaultTempFileExtension, keepFile);
            return new FileStream(
                fileName,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 4096,
                keepFile ? FileOptions.None : FileOptions.DeleteOnClose);
        }

        /// <summary>
        /// Gets the file stream for a temporary file in <see cref="BaseTemporaryDirectory"/>
        /// and store the file name in <see cref="FileNameList"/> if <see cref="StoreFileNames"/> is <c>true</c>.
        /// </summary>
        /// <param name="fileExtension">The extension for the temporary file name.</param>
        /// <returns>The <see cref="FileStream"/> for the temporary file.</returns>
        public FileStream GetTempFileNameStream(string fileExtension) => GetTempFileNameStream(fileExtension, StoreFileNames);

        /// <summary>
        /// Gets the file stream for a temporary file in <see cref="BaseTemporaryDirectory"/>.
        /// </summary>
        /// <param name="fileExtension">The extension for the temporary file name.</param>
        /// <param name="keepFile">
        /// <c>true</c> if the file should be kept after use; also store the file name in <see cref="FileNameList"/>;
        /// <c>false</c> if the file should be deleted.
        /// </param>
        /// <returns>The <see cref="FileStream"/> for the temporary file.</returns>
        public FileStream GetTempFileNameStream(string fileExtension, bool keepFile)
        {
            var fileName = GetTempFileName(fileExtension, keepFile);
            return new FileStream(
                fileName,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 4096,
                keepFile ? FileOptions.None : FileOptions.DeleteOnClose);
        }

        /// <summary>
        /// Gets the file stream for a temporary file in <see cref="BaseTemporaryDirectory"/>
        /// and store the file name in <see cref="FileNameList"/> if <see cref="StoreFileNames"/> is <c>true</c>.
        /// </summary>
        /// <param name="func">Custom function for creating <see cref="FileStream"/></param>
        /// <returns>The <see cref="FileStream"/> for the temporary file.</returns>
        public FileStream GetTempFileNameStream(CreateTempFileStreamFunc func)
            => GetTempFileNameStream(func, DefaultTempFileExtension, StoreFileNames);


        /// <summary>
        /// Gets the file stream for a temporary file in <see cref="BaseTemporaryDirectory"/>.
        /// </summary>
        /// <param name="func">Custom function for creating <see cref="FileStream"/></param>
        /// <param name="keepFile">
        /// <c>true</c> if the file should be kept after use; also store the file name in <see cref="FileNameList"/>;
        /// <c>false</c> if the file should be deleted.
        /// </param>
        /// <returns>The <see cref="FileStream"/> for the temporary file.</returns>
        public FileStream GetTempFileNameStream(CreateTempFileStreamFunc func, bool keepFile)
        {
            var fileName = GetTempFileName(keepFile);
            return func(fileName, keepFile);
        }

        /// <summary>
        /// Gets the file stream for a temporary file in <see cref="BaseTemporaryDirectory"/>
        /// and store the file name in <see cref="FileNameList"/> if <see cref="StoreFileNames"/> is <c>true</c>.
        /// </summary>
        /// <param name="func">Custom function for creating <see cref="FileStream"/></param>
        /// <param name="fileExtension">The extension for the temporary file name.</param>
        /// <returns>The <see cref="FileStream"/> for the temporary file.</returns>
        public FileStream GetTempFileNameStream(CreateTempFileStreamFunc func, string fileExtension)
            => GetTempFileNameStream(func, fileExtension, StoreFileNames);

        /// <summary>
        /// Gets the file stream for a temporary file in <see cref="BaseTemporaryDirectory"/>.
        /// </summary>
        /// <param name="func">Custom function for creating <see cref="FileStream"/></param>
        /// <param name="fileExtension">The extension for the temporary file name.</param>
        /// <param name="keepFile">
        /// <c>true</c> if the file should be kept after use; also store the file name in <see cref="FileNameList"/>;
        /// <c>false</c> if the file should be deleted.
        /// </param>
        /// <returns>The <see cref="FileStream"/> for the temporary file.</returns>
        public FileStream GetTempFileNameStream(CreateTempFileStreamFunc func, string fileExtension, bool keepFile)
        {
            var fileName = GetTempFileName(fileExtension, keepFile);
            return func(fileName, keepFile);
        }

        /// <summary>
        /// Gets the full path to the temporary file with default <c>.tmp</c> file extention
        /// and store the file name in <see cref="FileNameList"/> if <see cref="StoreFileNames"/> is <c>true</c>.
        /// </summary>
        /// <returns>The full path to the temporary file.</returns>
        public string GetTempFileName() => GetTempFileName(StoreFileNames);

        /// <summary>
        /// Gets the full path to the temporary file with default <c>.tmp</c> file extention.
        /// </summary>
        /// <param name="storeFileName">
        /// <c>true</c> store the file name in <see cref="FileNameList"/>;
        /// <c>false</c> don't store the file name in <see cref="FileNameList"/>.
        /// </param>
        /// <returns>The full path to the temporary file.</returns>
        public string GetTempFileName(bool storeFileName)
            => GetTempFileName(DefaultTempFileExtension, storeFileName);

        /// <summary>
        /// Gets the full path to the temporary file with specified file extention
        /// and store the file name in <see cref="FileNameList"/> if <see cref="StoreFileNames"/> is <c>true</c>.
        /// </summary>
        /// <param name="fileExtension">The extension for the temporary file name.</param>
        /// <returns>The full path to the temporary file.</returns>
        public string GetTempFileName(string fileExtension) => GetTempFileName(fileExtension, StoreFileNames);

        /// <summary>
        /// Gets the full path to the temporary file with specified file extention.
        /// </summary>
        /// <param name="fileExtension">The extension for the temporary file name.</param>
        /// <param name="storeFileName">
        /// <c>true</c> store the file name in <see cref="FileNameList"/>;
        /// <c>false</c> don't store the file name in <see cref="FileNameList"/>.
        /// </param>
        /// <returns>The full path to the temporary file.</returns>
        public string GetTempFileName(string fileExtension, bool storeFileName)
        {
            if (string.IsNullOrEmpty(fileExtension))
            {
                throw new ArgumentException(SR.Format(SR.ArgumentNullOrEmpty_Extension, nameof(fileExtension)), nameof(fileExtension));
            }

            string fileName = Path.Combine(BaseTemporaryDirectory, Guid.NewGuid().ToString() + fileExtension);
            if (storeFileName) _tempFileList.Add(fileName);
            return fileName;
        }

        /// <value>Gets the path to the temporary directory for the class instance.</value>
        /// <remarks>Default value is <c>user profile temp directory \ application name + _ + random number per the class instance</c>.</remarks>
        public string BaseTemporaryDirectory { get; init; }

        /// <value>
        /// Gets a default value for specifying
        /// whether file names should be stored in <see cref="FileNameList"/>.
        /// <c>true</c> store the file name in <see cref="FileNameList"/>;
        /// <c>false</c> don't store the file name in <see cref="FileNameList"/>.
        /// </value>
        public bool StoreFileNames { get; init; }

        /// <summary>
        /// Delete all temporary files from a disk and remove its names from the collection.
        /// </summary>
        public void Clear()
        {
            _fileNameList?.Clear();
            SafeDelete();
            Directory.CreateDirectory(BaseTemporaryDirectory);
        }

        private void SafeDelete()
        {
            try
            {
                Directory.Delete(BaseTemporaryDirectory, recursive: true);
            }
            catch
            {
                // Ignore all exceptions
            }
        }

        public IReadOnlyList<string> FileNameList
        {
            get => _tempFileList;
        }

        private List<string> _tempFileList
        {
            get => _fileNameList ?? new List<string>();
        }
    }
}
