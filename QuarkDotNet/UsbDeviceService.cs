using System;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;

namespace QuarkDotNet
{
    public class UsbDeviceService : IDisposable
    {
        UsbContext context = new UsbContext();

        public IUsbDevice GetDevice(int vendorId, int productId)
        {
            //context.SetDebugLevel(LogLevel.Debug);

            var deviceList = context.List();

            var device = GetDeviceFromList(deviceList, vendorId, productId);
            if (device != null && device.TryOpen())
            {
                device.ClaimInterface(0);
                return device;
            }

            return null;
        }

        private static IUsbDevice GetDeviceFromList(UsbDeviceCollection deviceList, int vendorId, int productId)
        {
            foreach (var device in deviceList)
            {
                if (device.VendorId == vendorId && device.ProductId == productId)
                {
                    return device;
                }
            }

            return null;
        }

        public byte[] readBytes(IUsbDevice device, int blockSize, out Error lastResult)
        {
            byte[] buffer = new byte[blockSize];
            var reader = device.OpenEndpointReader(ReadEndpointID.Ep01, blockSize, EndpointType.Bulk);
            lastResult = reader.Read(buffer, 0, out int length);

            if (lastResult == Error.Success && length > 0)
                return buffer;

            return null;
        }

        public void writeBytes(IUsbDevice device, byte[] resp_block)
        {
            var writer = device.OpenEndpointWriter(WriteEndpointID.Ep01, EndpointType.Bulk);
            writer.Write(resp_block, 0, out _);
        }

        public void Dispose()
        {
            context?.Dispose();
            context = null;
        }
    }
}
