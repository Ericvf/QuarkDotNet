using System.IO;
using System.Text;

namespace QuarkDotNet
{
    public class CommandBlock
    {
        public enum CommandIds : int
        {
            Invalid,
            GetDriveCount,
            GetDriveInfo,
            StatPath,
            GetFileCount,
            GetFile,
            GetDirectoryCount,
            GetDirectory,
            StartFile,
            ReadFile,
            WriteFile,
            EndFile,
            Create,
            Delete,
            Rename,
            GetSpecialPathCount,
            GetSpecialPath,
            SelectFile
        };

        public static int GLCI = 0x49434C47;
        public static int GLCO = 0x4F434C47;

        private BinaryReader reader;
        private BinaryWriter writer;
        private byte[] outputBuffer;
        private bool isValid;
        private CommandIds commandId;

        public bool IsValid() => isValid;

        public CommandIds CommandId() => commandId;

        public CommandBlock(byte[] inputBuffer, byte[] outputBuffer)
        {
            reader = new BinaryReader(new MemoryStream(inputBuffer));
            writer = new BinaryWriter(new MemoryStream(outputBuffer));
            this.outputBuffer = outputBuffer;

            int magic = read32();
            if (magic == GLCI)
            {
                isValid = true;
                commandId = (CommandIds)read32();
            }
        }

        public int read32() => reader.ReadInt32();

        public long read64() => reader.ReadInt64();

        public string readString()
        {
            var x = read32();
            var bytes = reader.ReadBytes(x * 2);
            return Encoding.Unicode.GetString(bytes);
        }

        public void write32(int val) => writer.Write(val);

        public void write64(long val) => writer.Write(val);

        public void writeString(string val)
        {
            byte[] raw = Encoding.Unicode.GetBytes(val);
            write32(val.Length);
            writer.Write(raw);
        }

        public void responseStart()
        {
            writer.Write(GLCO);
            writer.Write(0);
        }

        public byte[] respondFailure(int result)
        {
            writer.Write(GLCO);
            writer.Write(result);
            return responseEnd();
        }

        public byte[] responseEnd()
        {
            return outputBuffer;
        }

        public byte[] respondEmpty()
        {
            responseStart();
            return responseEnd();
        }
    }
}
