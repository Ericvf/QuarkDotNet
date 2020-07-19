using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using LibUsbDotNet;

namespace QuarkDotNet
{
    public class GoldleafClient
    {
        private const int VendorId = 0x057E;
        private const int ProductId = 0x3000;

        public class State
        {
            public State(bool isConnected) => IsConnected = isConnected;

            public bool IsConnected { get; set; }
        }

        public event EventHandler<State> StateChange;

        private volatile bool isRunning;

        private readonly UsbDeviceService deviceService;
        private Thread deviceThread;

        public GoldleafClient()
        {
            deviceService = new UsbDeviceService();
            isRunning = false;
        }

        public void Start()
        {
            if (isRunning)
                throw new ApplicationException();

            deviceThread = new Thread(DeviceThread);
            deviceThread.Start();
            isRunning = true;
        }

        public void Stop()
        {
            if (isRunning)
            {
                isRunning = false;

                if (deviceThread?.IsAlive == true)
                {
                    var device = deviceService.GetDevice(VendorId, ProductId);
                    device?.ResetDevice();

                    deviceThread.Join();
                }
            }
        }

        private void DeviceThread(object obj)
        {
            while (isRunning)
            {
                var device = deviceService.GetDevice(VendorId, ProductId);
                if (device != null)
                {
                    var deviceName = device.ToString();

                    Console.WriteLine($"Connected to device: {deviceName}");
                    Console.WriteLine();

                    //vm.IsConnected = true;
                    //vm.Device = device;
                    RaiseStateChange(true);

                    var drives = FileSystem.GetDrives();

                    FileStream readfile = null;
                    FileStream writefile = null;

                    while (device.IsOpen)
                    {
                        var xxx = deviceService.readBytes(device, Command.BlockSize, out var lastResult);

                        if (lastResult == Error.Pipe || lastResult == Error.Io)
                            break;

                        if (xxx != null)
                        {
                            var c = new Command(xxx, new byte[Command.BlockSize]);
                            int magic = c.read32();
                            if (magic == Command.GLCI)
                            {
                                int cmdid = c.read32();
                                int idx = 0;
                                string path;

                                CommandId id = (CommandId)cmdid;
                                Console.WriteLine($"Command: {id}");

                                switch (id)
                                {
                                    case CommandId.Invalid:
                                        break;
                                    case CommandId.GetDriveCount:
                                        {
                                            c.responseStart();
                                            c.write32(drives.Count());
                                            var end = c.responseEnd();
                                            deviceService.writeBytes(device, end);
                                            break;
                                        }
                                    case CommandId.GetDriveInfo:
                                        {
                                            idx = c.read32();

                                            if (idx < drives.Count())
                                            {
                                                var drive = drives.ElementAt(idx);
                                                c.responseStart();
                                                c.writeString(drive);
                                                c.writeString(drive);
                                                c.write32(0);
                                                c.write32(0);
                                                var end = c.responseEnd();
                                                deviceService.writeBytes(device, end);
                                            }
                                            else
                                            {
                                                var end = c.respondFailure(0xDEAD);
                                                deviceService.writeBytes(device, end);
                                            }
                                            break;
                                        }
                                    case CommandId.StatPath:
                                        {
                                            path = c.readString();
                                            try
                                            {
                                                var fileInfo = new FileInfo(path);
                                                var isDir = Directory.Exists(path);

                                                int type = 0;
                                                long filesz = 0;
                                                if (!isDir && fileInfo.Exists)
                                                {
                                                    type = 1;
                                                    filesz = fileInfo.Length;
                                                }
                                                if (isDir) type = 2;
                                                if (type == 0)
                                                {
                                                    var end = c.respondFailure(0xDEAD);
                                                    deviceService.writeBytes(device, end);
                                                }
                                                else
                                                {
                                                    c.responseStart();
                                                    c.write32(type);
                                                    c.write64(filesz);
                                                    var end = c.responseEnd();
                                                    deviceService.writeBytes(device, end);
                                                }
                                            }
                                            catch
                                            {
                                                var end = c.respondFailure(0xDEAD);
                                                deviceService.writeBytes(device, end);
                                            }
                                            break;
                                        }
                                    case CommandId.GetFileCount:
                                        {
                                            path = c.readString();
                                            int count = Directory.GetFiles(path).Length;
                                            c.responseStart();
                                            c.write32(count);
                                            var end = c.responseEnd();
                                            deviceService.writeBytes(device, end);
                                            break;
                                        }
                                    case CommandId.GetFile:
                                        {
                                            path = c.readString();
                                            idx = c.read32();
                                            var files = Directory.GetFiles(path);
                                            if (idx < files.Length)
                                            {
                                                c.responseStart();
                                                var di = files.ElementAt(idx);
                                                var info = new FileInfo(di);
                                                c.writeString(info.Name);
                                                var end = c.responseEnd();
                                                deviceService.writeBytes(device, end);
                                            }
                                            else
                                            {
                                                var end = c.respondFailure(0xDEAD);
                                                deviceService.writeBytes(device, end);
                                            }
                                            break;
                                        }
                                    case CommandId.GetDirectoryCount:
                                        {
                                            path = c.readString();
                                            int count = Directory.GetDirectories(path).Length;
                                            c.responseStart();
                                            c.write32(count);
                                            var end = c.responseEnd();
                                            deviceService.writeBytes(device, end);
                                            break;
                                        }
                                    case CommandId.GetDirectory:
                                        {
                                            path = c.readString();
                                            idx = c.read32();

                                            var dirs = Directory.GetDirectories(path);
                                            if (idx < dirs.Length)
                                            {
                                                c.responseStart();
                                                var di = dirs.ElementAt(idx);
                                                var info = new DirectoryInfo(di);
                                                c.writeString(info.Name);
                                                var end = c.responseEnd();
                                                deviceService.writeBytes(device, end);
                                            }
                                            else
                                            {
                                                var end = c.respondFailure(0xDEAD);
                                                deviceService.writeBytes(device, end);
                                            }
                                            break;
                                        }
                                    case CommandId.StartFile:
                                        {
                                            path = c.readString();
                                            int mode = c.read32();
                                            if (mode == 1)
                                            {
                                                if (readfile != null) readfile.Close();
                                                readfile = File.OpenRead(path);
                                            }
                                            else
                                            {
                                                if (writefile != null) writefile.Close();
                                                writefile = File.OpenWrite(path);

                                                if (mode == 3) writefile.Seek(writefile.Length, SeekOrigin.Begin);
                                            }
                                            var end = c.respondEmpty();
                                            deviceService.writeBytes(device, end);
                                        }
                                        break;
                                    case CommandId.ReadFile:
                                        {
                                            path = c.readString();
                                            long offset = c.read64();
                                            long size = c.read64();
                                            try
                                            {
                                                if (readfile != null)
                                                {
                                                    byte[] block = new byte[(int)size];
                                                    readfile.Seek(offset, SeekOrigin.Begin);
                                                    int read = readfile.Read(block, 0, (int)size);
                                                    c.responseStart();
                                                    c.write64((long)read);
                                                    var end = c.responseEnd();
                                                    deviceService.writeBytes(device, end);
                                                    deviceService.writeBytes(device, block);
                                                }
                                                else
                                                {
                                                    var raf = readfile = File.OpenRead(path);
                                                    byte[] block = new byte[(int)size];
                                                    raf.Seek(offset, SeekOrigin.Begin);
                                                    int read = raf.Read(block, 0, (int)size);
                                                    raf.Close();
                                                    c.responseStart();
                                                    c.write64((long)read);
                                                    var end = c.responseEnd();
                                                    deviceService.writeBytes(device, end);
                                                    deviceService.writeBytes(device, block);
                                                }
                                            }
                                            catch
                                            {
                                                var end = c.respondFailure(0xDEAD);
                                                deviceService.writeBytes(device, end);
                                            }
                                            break;
                                        }
                                    case CommandId.WriteFile:
                                        break;
                                    case CommandId.EndFile:
                                        {
                                            var mode = c.read32();
                                            if (mode == 1)
                                            {
                                                if (readfile != null)
                                                {
                                                    readfile.Close();
                                                    readfile = null;
                                                }
                                            }
                                            else
                                            {
                                                if (writefile != null)
                                                {
                                                    writefile.Close();
                                                    writefile = null;
                                                }
                                            }
                                            var end = c.respondEmpty();
                                            deviceService.writeBytes(device, end);
                                        }
                                        break;
                                    case CommandId.Create:
                                        break;
                                    case CommandId.Delete:
                                        break;
                                    case CommandId.Rename:
                                        break;
                                    case CommandId.GetSpecialPathCount:
                                        {
                                            c.responseStart();
                                            c.write32(1);
                                            var end = c.responseEnd();
                                            deviceService.writeBytes(device, end);
                                            break;
                                        }
                                    case CommandId.GetSpecialPath:
                                        {
                                            c.responseStart();
                                            c.writeString("Switch Roms");
                                            c.writeString("e:\\switch roms");
                                            var end = c.responseEnd();
                                            deviceService.writeBytes(device, end);
                                            break;
                                        }
                                    case CommandId.SelectFile:
                                        {
                                            var end = c.respondFailure(0xDEAD);
                                            deviceService.writeBytes(device, end);
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }

                    device.ReleaseInterface(0);
                    device.Close();

                    //vm.IsConnected = false;
                    RaiseStateChange(false);

                    Console.WriteLine($"Disconnected from device: {deviceName}");
                }

                if (isRunning)
                {
                    Thread.Sleep(3000);
                }
            }
        }

        private void RaiseStateChange(bool isConnected) 
            => StateChange?.Invoke(this, new State(isConnected));
    }

    public enum CommandId : int
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

    public class Buffer
    {
        private BinaryReader reader;
        private BinaryWriter writer;

        public Buffer(byte[] data)
        {
            var buf = new MemoryStream(data);
            reader = new BinaryReader(buf);
            writer = new BinaryWriter(buf);
        }

        public int read32()
        {
            return reader.ReadInt32();
        }

        public long read64()
        {
            return reader.ReadInt64();
        }

        public byte[] readBytes(int length)
        {
            return reader.ReadBytes(length);
        }

        public void write32(int val)
        {
            writer.Write(val);
        }

        public void write64(long val)
        {
            writer.Write(val);
        }

        public void writeBytes(byte[] data)
        {
            writer.Write(data);
        }
    }

    public class Command
    {
        public static int BlockSize = 0x1000;
        public static int GLCI = 0x49434C47;
        public static int GLCO = 0x4F434C47;

        public static byte WriteEndpoint = (byte)0x1;
        public static byte ReadEndpoint = (byte)0x81;

        private Buffer inner_buf;
        private Buffer resp_buf;
        private byte[] resp_block;

        public Command(byte[] b, byte[] e)
        {
            inner_buf = new Buffer(b);
            resp_buf = new Buffer(e);
            resp_block = e;
        }

        public int read32()
        {
            return inner_buf.read32();
        }

        public long read64()
        {
            return inner_buf.read64();
        }

        public bool isValid()
        {
            return false;
        }

        public string readString()
        {
            var x = read32();
            var bytes = inner_buf.readBytes(x * 2);
            return Encoding.Unicode.GetString(bytes);
        }

        public void responseStart()
        {
            resp_buf.write32(GLCO);
            resp_buf.write32(0);
        }

        public byte[] respondFailure(int result)
        {
            resp_buf.write32(GLCO);
            resp_buf.write32(result);
            return responseEnd();
        }

        public byte[] responseEnd()
        {
            return resp_block;
        }

        public byte[] respondEmpty()
        {
            responseStart();
            return responseEnd();
        }

        public void write32(int val)
        {
            resp_buf.write32(val);
        }

        public void write64(long val)
        {
            resp_buf.write64(val);
        }

        public void writeString(string val)
        {
            byte[] raw = Encoding.Unicode.GetBytes(val);
            write32(val.Length);
            resp_buf.writeBytes(raw);
        }
    }
}
