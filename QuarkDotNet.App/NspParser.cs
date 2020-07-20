namespace QuarkDotNet.App
{
    public class NspParser
    {
        private readonly FileSystem fileSystem;

        public NspParser(FileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public NspModel Parse(string file)
        {
            var fileName = fileSystem.GetFileName(file);
            var fileSize = fileSystem.GetFileSize(file);

            var nspModel = new NspModel() { 
                Name = fileName,
                Size = fileSize,
            };

            return nspModel;
        }
    }
}