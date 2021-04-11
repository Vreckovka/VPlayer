
namespace UPnP.Utils
{
    static class Parser
    {
        internal static string UseSlash(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "";

            if (url.Substring(url.Length - 1, 1) != "/")
                return "/";

            return "";
        }
    }
}
