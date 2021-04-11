
namespace UPnP.Common
{
    public static class SupportedOnlineDevices
    {
        private static string[] _items = new string[]
        {
            "TMSDeviceDescription.xml",
            "DeviceDescription.xml",
            "description.xml"
        };
        public static string[] Items { get { return _items; } }
    }
}
