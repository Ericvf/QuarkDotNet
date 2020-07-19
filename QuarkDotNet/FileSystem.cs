using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuarkDotNet
{
    public class FileSystem
    {
        public static IEnumerable<string> GetDrives()
        {
            return DriveInfo.GetDrives()
                .Select(f => f.Name)
                .ToArray();
        }
    }
}
