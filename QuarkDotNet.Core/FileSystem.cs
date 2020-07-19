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

        public IEnumerable<string> GetFiles(string path) => Directory.GetFiles(path);

        public int GetFileCount(string path) => GetFiles(path).Count();

        public string GetFileName(string path) => Path.GetFileName(path);

        public IEnumerable<string> GetDirectories(string path) => Directory.GetDirectories(path);

        public int GetDirectoryCount(string path) => GetDirectories(path).Count();

        public string GetDirectoryName(string directoryPath) => Path.GetDirectoryName(directoryPath);
    }
}
