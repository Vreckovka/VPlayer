using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace UPnP.Common
{
    [XmlRoot("scpd", Namespace = "urn:schemas-upnp-org:service-1-0")]
    public class Services
    {
        public Services()
        {
        }

        [XmlElement("specVersion")]
        public SpecVersionType SpecVersion { get; set; }
        [XmlArray("actionList")]
        [XmlArrayItem("action")]
        public ServiceAction[] ActionList { get; set; }
        [XmlArray("serviceStateTable")]
        [XmlArrayItem("stateVariable")]
        public StateVariable[] ServiceStateTable { get; set; }
    }

    public partial class ServiceAction
    {
        public ServiceAction()
        {
        }

        [XmlElement("name")]
        public string Name { get; set; }
        [XmlArray("argumentList")]
        [XmlArrayItem("argument")]
        public Argument[] ArgumentList { get; set; }

        #region Public functions

        public void ClearArgumentsValue()
        {
            foreach (Argument arg in ArgumentList)
                arg.Value = null;
        }

        public void SetArgumentValue(string key, string value)
        {
            bool FoundArgument = false;
            foreach (Argument arg in ArgumentList)
                if (arg.Name.ToUpper() == key.ToUpper())
                {
                    arg.Value = value;
                    FoundArgument = true;
                }

            if (!FoundArgument)
                throw new Exception("Invalid argument!");
        }

        public async Task InvokeAsync(string service_type, string control_url)
        {
            try
            {
                StringBuilder xml = new StringBuilder();
                xml.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
                xml.Append("<s:Envelope s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\">\n");
                xml.Append("<s:Body>\n");
                xml.Append("<u:" + this.Name + " xmlns:u=\"" + service_type + "\">\n");
                foreach (Argument arg in this.ArgumentList)
                {
                    if (arg.Direction.ToUpper() == "IN")
                        xml.Append("<" + arg.Name + ">" + arg.Value + "</" + arg.Name + ">");
                }
                xml.Append("</u:" + this.Name + ">\n");
                xml.Append("</s:Body>\n");
                xml.Append("</s:Envelope>\n");

                HttpClient client = new HttpClient();
                //client.DefaultRequestHeaders.Authorization = CreateBasicHeader("username", "password");  //if needed...

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, control_url);
                request.Headers.Add("SOAPAction", "\"" + service_type + "#" + this.Name + "\"");
                request.Method = HttpMethod.Post;
                StringContent requestContent = new StringContent(xml.ToString(), Encoding.UTF8, "text/xml");
                request.Content = requestContent;

                HttpResponseMessage response = await client.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream stream = await response.Content.ReadAsStreamAsync();
                    XmlReader rdr = XmlReader.Create(stream);
                    while (rdr.Read())
                    {
                        if (rdr.IsStartElement())
                        {
                            foreach (Argument arg in this.ArgumentList)
                            {
                                if (arg.Direction.ToUpper() == "OUT" && arg.Name.ToUpper() == rdr.Name.ToUpper())
                                {
                                    arg.Value = (string)rdr.ReadElementContentAs(typeof(string), null);
                                }
                            }
                        }

                    }
                }
            }
            catch
            {
            }
        }

        public string GetArgumentValue(string key)
        {
            foreach (Argument arg in ArgumentList)
                if (arg.Name.ToUpper() == key.ToUpper())
                    return arg.Value;

            throw new Exception("Invalid argument!");
        }

        private AuthenticationHeaderValue CreateBasicHeader(string username, string password)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(username + ":" + password);
            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        #endregion
    }

    public partial class Argument
    {
        public Argument()
        {
        }

        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("direction")]
        public string Direction { get; set; }
        [XmlElement("relatedStateVariable")]
        public string RelatedStateVariable { get; set; }

        [XmlIgnore]
        public string Value { get; set; }
    }

    public partial class StateVariable
    {
        public StateVariable()
        {
        }

        [XmlAttribute("sendEvents")]
        public string SendEvents { get; set; }
        [XmlElement("name")]
        public string Name { get; set; }
        [XmlElement("dataType")]
        public string DataType { get; set; }
        [XmlArray("allowedValueList")]
        [XmlArrayItem("allowedValue")]
        public string[] AllowedValueList { get; set; }
    }
}