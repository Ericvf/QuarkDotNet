using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using LibUsbDotNet;
using QuarkDotNet.Core;
using CommandIds = QuarkDotNet.CommandBlock.CommandIds;

namespace QuarkDotNet
{
    public class GoldleafClient
    {
        private readonly UsbDeviceService usbDeviceService;
        private readonly ILogger logger;
        private readonly FileSystem fileSystem;
        public static int BlockSize = 0x1000;
        private const int VendorId = 0x057E;
        private const int ProductId = 0x3000;

        private volatile bool isRunning = false;
        private Thread deviceThread;

        private delegate IEnumerable<byte[]> CommandHandler(CommandBlock commandBlock);
        private readonly Dictionary<CommandIds, CommandHandler> commandHandlers;

        FileStream readfile = null;
        FileStream writefile = null;

        public GoldleafClient(UsbDeviceService usbDeviceService, ILogger logger, FileSystem fileSystem)
        {
            this.usbDeviceService = usbDeviceService;
            this.logger = logger;
            this.fileSystem = fileSystem;

            commandHandlers = new Dictionary<CommandIds, CommandHandler>()
            {
                { CommandIds.GetDriveCount, GetDriveCount },
                { CommandIds.GetDriveInfo, GetDriveInfo },
                { CommandIds.StatPath, GetStatPath },
                { CommandIds.GetFileCount, GetFileCount },
                { CommandIds.GetFile, GetFile },
                { CommandIds.GetDirectoryCount, GetDirectoryCount },
                { CommandIds.GetDirectory, GetDirectory },
                { CommandIds.StartFile, StartFile },
                { CommandIds.ReadFile, ReadFile },
                { CommandIds.EndFile, EndFile },
                { CommandIds.GetSpecialPathCount, GetSpecialPathCount },
                { CommandIds.GetSpecialPath, GetSpecialPath },
                { CommandIds.SelectFile, SelectFile },
            };
        }

        public void Start()
        {
            if (!isRunning)
            {
                deviceThread = new Thread(DeviceThread);
                deviceThread.Start();
                isRunning = true;
            }
        }

        public void Stop()
        {
            if (isRunning)
            {
                isRunning = false;

                if (deviceThread?.IsAlive == true)
                {
                    var device = usbDeviceService.GetDevice(VendorId, ProductId);
                    device?.ResetDevice();

                    deviceThread.Join();
                }
            }
        }

        private void DeviceThread(object obj)
        {
            while (isRunning)
            {
                var device = usbDeviceService.GetDevice(VendorId, ProductId);
                if (device != null)
                {
                    var deviceName = device.ToString();

                    logger.Print($"Connected to device: {deviceName}");

                    RaiseStateChange(true);

                    while (device.IsOpen)
                    {
                        var inputBuffer = usbDeviceService.readBytes(device, BlockSize, out var lastResult);

                        if (lastResult == Error.Pipe || lastResult == Error.Io)
                            break;

                        if (inputBuffer != null)
                        {
                            var outputBuffer = new byte[BlockSize];
                            var commandBlock = new CommandBlock(inputBuffer, outputBuffer);
                            if (commandBlock.IsValid())
                            {
                                var commandId = commandBlock.CommandId();
                                if (commandHandlers.TryGetValue(commandId, out var commandHandler))
                                {
                                    try
                                    {
                                        var outputBuffers = commandHandler(commandBlock);
                                        foreach (var buffer in outputBuffers)
                                        {
                                            usbDeviceService.writeBytes(device, buffer);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        usbDeviceService.writeBytes(device, commandBlock.respondFailure(0xDEAD));
                                        logger.Error($"Exception: {ex}");
                                    }
                                }
                                else
                                {
                                    logger.Error($"Command not found: {commandId}");
                                }
                            }
                        }
                    }

                    device.ReleaseInterface(0);
                    device.Close();

                    RaiseStateChange(false);

                    logger.Print($"Disconnected from device: {deviceName}");
                }

                if (isRunning)
                {
                    Thread.Sleep(3000);
                }
            }
        }

        private IEnumerable<byte[]> SelectFile(CommandBlock commandBlock)
        {
            logger.Error($"SelectFile");
            yield return commandBlock.respondFailure(0xDEAD);
        }

        private IEnumerable<byte[]> GetSpecialPath(CommandBlock commandBlock)
        {
            logger.Print($"GetSpecialPath: {1}");
            commandBlock.responseStart();
            commandBlock.writeString("Switch Roms");
            commandBlock.writeString("e:\\switch roms");
            yield return commandBlock.responseEnd();
        }

        private IEnumerable<byte[]> GetSpecialPathCount(CommandBlock commandBlock)
        {
            logger.Print($"GetSpecialPathCount: {2}");

            commandBlock.responseStart();
            commandBlock.write32(2);
            yield return commandBlock.responseEnd();
        }

        private IEnumerable<byte[]> EndFile(CommandBlock commandBlock)
        {
            logger.Print($"EndFile");

            var mode = commandBlock.read32();
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

            yield return commandBlock.respondEmpty();
        }

        private IEnumerable<byte[]> ReadFile(CommandBlock commandBlock)
        {
            var path = commandBlock.readString();
            var offset = commandBlock.read64();
            var size = commandBlock.read64();

            logger.Print($"ReadFile: {path} ({offset}:{size})");

            if (readfile != null)
            {
                byte[] block = new byte[(int)size];
                readfile.Seek(offset, SeekOrigin.Begin);
                int read = readfile.Read(block, 0, (int)size);
                commandBlock.responseStart();
                commandBlock.write64((long)read);
                yield return commandBlock.responseEnd();
                yield return block;
            }
            else
            {
                logger.Debug($"FileHandle OpenRead: {path}");
                var raf = readfile = File.OpenRead(path);
                byte[] block = new byte[(int)size];
                raf.Seek(offset, SeekOrigin.Begin);
                int read = raf.Read(block, 0, (int)size);
                raf.Close();
                commandBlock.responseStart();
                commandBlock.write64((long)read);
                yield return commandBlock.responseEnd();
                yield return block;
            }
        }

        private IEnumerable<byte[]> StartFile(CommandBlock commandBlock)
        {
            var path = commandBlock.readString();
            int mode = commandBlock.read32();

            logger.Print($"StartFile: {path} ({mode})");

            if (mode == 1)
            {
                if (readfile != null) readfile.Close();

                logger.Debug($"FileHandle OpenRead: {path}");
                readfile = File.OpenRead(path);
            }
            else
            {
                if (writefile != null) writefile.Close();

                logger.Debug($"FileHandle OpenWrite: {path}");
                writefile = File.OpenWrite(path);

                if (mode == 3)
                    writefile.Seek(writefile.Length, SeekOrigin.Begin);
            }
            yield return commandBlock.respondEmpty();
        }

        private IEnumerable<byte[]> GetDirectory(CommandBlock commandBlock)
        {
            var path = commandBlock.readString();
            var idx = commandBlock.read32();

            var directories = fileSystem.GetDirectories(path);
            if (idx < directories.Count())
            {
                var directoryPath = directories.ElementAt(idx);
                var directoryInfo = new DirectoryInfo(directoryPath);
                var directoryName = directoryInfo.Name;

                logger.Print($"GetDirectory: {directoryName}");

                commandBlock.responseStart();
                commandBlock.writeString(directoryName);
                yield return commandBlock.responseEnd();
            }
            else
            {
                yield return commandBlock.respondFailure(0xDEAD);
            }
        }

        private IEnumerable<byte[]> GetDirectoryCount(CommandBlock commandBlock)
        {
            var path = commandBlock.readString();
            var directoryCount = fileSystem.GetDirectoryCount(path);

            logger.Print($"GetDirectoryCount: {path} ({directoryCount})");

            commandBlock.responseStart();
            commandBlock.write32(directoryCount);
            yield return commandBlock.responseEnd();
        }

        private IEnumerable<byte[]> GetFile(CommandBlock commandBlock)
        {
            var path = commandBlock.readString();
            var idx = commandBlock.read32();
            var files = fileSystem.GetFiles(path);

            if (idx < files.Count())
            {
                var file = files.ElementAt(idx);
                var fileName = fileSystem.GetFileName(file);

                logger.Print($"GetFile: {fileName}");

                commandBlock.responseStart();
                commandBlock.writeString(fileName);
                yield return commandBlock.responseEnd();
            }
            else
            {
                yield return commandBlock.respondFailure(0xDEAD);
            }
        }

        private IEnumerable<byte[]> GetFileCount(CommandBlock commandBlock)
        {
            var path = commandBlock.readString();
            int fileCount = fileSystem.GetFileCount(path);

            logger.Print($"GetFileCount: {path} ({fileCount})");

            commandBlock.responseStart();
            commandBlock.write32(fileCount);
            yield return commandBlock.responseEnd();
        }

        private IEnumerable<byte[]> GetStatPath(CommandBlock commandBlock)
        {
            var path = commandBlock.readString();

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
                yield return commandBlock.respondFailure(0xDEAD);
            }
            else
            {
                commandBlock.responseStart();
                commandBlock.write32(type);
                commandBlock.write64(filesz);

                logger.Print($"GetStatPath: {path} ({type}:{filesz})");

                yield return commandBlock.responseEnd();
            }
        }

        private IEnumerable<byte[]> GetDriveInfo(CommandBlock commandBlock)
        {
            var idx = commandBlock.read32();
            var getDrives = fileSystem.GetDriveNames();
            if (idx < getDrives.Count())
            {
                var drive = getDrives.ElementAt(idx);
                var driveName = drive.VolumeLabel;
                var drivePath = drive.Name;

                logger.Print($"GetDriveInfo: {drivePath}");

                commandBlock.responseStart();
                commandBlock.writeString(driveName);
                commandBlock.writeString(drivePath);
                commandBlock.write32(0);
                commandBlock.write32(0);
                yield return commandBlock.responseEnd();
            }
            else
            {
                yield return commandBlock.respondFailure(0xDEAD);
            }
        }

        private IEnumerable<byte[]> GetDriveCount(CommandBlock commandBlock)
        {
            var driveCount = fileSystem.GetDriveCount();
            logger.Print($"GetDriveCount: {driveCount}");

            commandBlock.responseStart();
            commandBlock.write32(driveCount);
            yield return commandBlock.responseEnd();
        }

        #region Events 
        private void RaiseStateChange(bool isConnected)
            => StateChange?.Invoke(this, new State(isConnected));

        public class State
        {
            public State(bool isConnected) => IsConnected = isConnected;

            public bool IsConnected { get; set; }
        }

        public event EventHandler<State> StateChange;

        #endregion
    }
}
