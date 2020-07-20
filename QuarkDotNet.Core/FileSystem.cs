using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuarkDotNet
{
    public class FileSystem
    {
        private readonly DriveInfo[] drives;

        public FileSystem()
        {
            drives = DriveInfo.GetDrives();
        }

        public IEnumerable<DriveInfo> GetDriveNames() => drives;

        public int GetDriveCount() => drives.Count();

        public long GetFileSize(string file)
        {
            var fileInfo = new FileInfo(file);
            return fileInfo.Exists
                ? fileInfo.Length
                : 0;
        }

        public IEnumerable<string> GetFiles(string path) => Directory.GetFiles(path);

        public IEnumerable<string> GetAllFiles(string path, string pattern) => Directory.GetFiles(path, pattern, SearchOption.AllDirectories);

        public int GetFileCount(string path) => GetFiles(path).Count();

        public string GetFileName(string path) => Path.GetFileName(path);

        public IEnumerable<string> GetDirectories(string path) => Directory.GetDirectories(path);

        public int GetDirectoryCount(string path) => GetDirectories(path).Count();

        public string GetDirectoryName(string directoryPath) => new DirectoryInfo(directoryPath).Name;
    }
}
