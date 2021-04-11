using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace UPnP.Utils
{
    static class Request
    {
        internal static async Task<Uri> RequestUriAsync(Uri url)
        {
            WebRequest request;
            HttpWebResponse response;
            try
            {
                Uri result = url;
                request = WebRequest.Create(result);
                response = (HttpWebResponse)await request.GetResponseAsync();
                if (response.ResponseUri.AbsoluteUri != request.RequestUri.AbsoluteUri)
                    result = response.ResponseUri;
                HttpStatusCode statusCode = response.StatusCode;
                response.Dispose();
                if (statusCode == HttpStatusCode.OK)
                    return result;
                else
                    return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                request = null;
                response = null;
            }
        }

        internal static string RequestStringAsync(Uri url, Encoding encoding)
        {
            WebRequest request;
            HttpWebResponse response;
            try
            {
                request = WebRequest.Create(url);
                response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream inStream = response.GetResponseStream();
                    StreamReader rdr = new StreamReader(inStream, encoding, true);
                    string result = rdr.ReadToEnd();
                    response.Dispose();
                    return result;
                }
                else
                    return null;
            }
            catch
            {
                return null;
            }
            finally
            {
                request = null;
                response = null;
            }
        }

        internal static async Task<byte[]> RequestByteArrayAsync(Uri url)
        {
            HttpClient client = new HttpClient();
            try
            {
                byte[] byteArr = await client.GetByteArrayAsync(url.AbsoluteUri);
                client.Dispose();
                return byteArr;
            }
            catch
            {
                return null;
            }
            finally
            {
                client = null;
            }
        }
    }
}
