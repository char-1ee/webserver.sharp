using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Extensions;

namespace WebServer
{
    public class Router
    {
        public string WebsitePath { get; set; }

        private Dictionary<string, ExtensionInfo> extFolderMap;

        public Router()
        {
            extFolderMap = new Dictionary<string, ExtensionInfo>()
            {
                {"ico",  new ExtensionInfo() { Loader = ImageLoader, ContentType = "image/ico"     }},
                {"png",  new ExtensionInfo() { Loader = ImageLoader, ContentType = "image/png"     }},
                {"jpg",  new ExtensionInfo() { Loader = ImageLoader, ContentType = "image/jpg"     }},
                {"gif",  new ExtensionInfo() { Loader = ImageLoader, ContentType = "image/gif"     }},
                {"bmp",  new ExtensionInfo() { Loader = ImageLoader, ContentType = "image/bmp"     }},
                {"html", new ExtensionInfo() { Loader = PageLoader, ContentType = "text/html"      }},
                {"css",  new ExtensionInfo() { Loader = FileLoader, ContentType = "text/css"       }},
                {"js",   new ExtensionInfo() { Loader = FileLoader, ContentType = "text/javascript"}},
                {"",     new ExtensionInfo() { Loader = PageLoader, ContentType = "text/html"      }},
            };
        }

        /// <summary>
        /// Read in an image file, return a ResponsePacket with the raw data.
        /// </summary>
        private ResponsePacket ImageLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            FileStream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            ResponsePacket res = new ResponsePacket()
            {
                Data = binaryReader.ReadBytes((int)fileStream.Length),
                ContentType = extInfo.ContentType
            };

            binaryReader.Close();
            fileStream.Close();

            return res;
        }

        /// <summary>
        /// Read in what is basically a text file and return a ResponsePacket with the ext UTF8 encoded.
        /// </summary>
        private ResponsePacket FileLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            string text = File.ReadAllText(fullPath);
            ResponsePacket res = new ResponsePacket()
            {
                Data = Encoding.UTF8.GetBytes(text),
                ContentType = extInfo.ContentType, 
                Encoding = Encoding.UTF8
            };
            return res;
        }

        /// <summary>
        /// Read an HTML file, including foo.com, foo.com\index, foo.com\index.html
        /// </summary>        
        private ResponsePacket PageLoader(string fullPath, string ext, ExtensionInfo extInfo)
        {
            ResponsePacket res = new ResponsePacket();

            if (fullPath == WebsitePath) // if foo.com, defaultly load index.html
            {
                res = Route(GET, "/index.html", null);
            }
            else // otherwise foo.com\index.html or foo.com\index
            {
                if (String.IsNullOrEmpty(ext))
                {
                    fullPath = fullPath + ".html";
                }

                // inject the "Page" folder into the path
                fullPath = WebsitePath + "\\Pages" + fullPath.RightOf(WebsitePath);
                res = FileLoader(fullPath, ext, extInfo);
            }

            return res;
        }

        public ResponsePacket Route(string httpMethod, string path, Dictionary<string, string> kvParams)
        {
            string ext = path.RightOf('.');
            ExtensionInfo extInfo;
            ResponsePacket res = null;

            if (extFolderMap.TryGetValue(ext, out extInfo))
            {
                // '/' => '\' for windows
                string fullPath = Path.Combine(WebsitePath, path);
                res = extInfo.Loader(fullPath, ext, extInfo);
            }

            return res;

        }
    }

    /// <summary>
    /// Helper class: web packet 
    /// </summary>
    public class ResponsePacket
    {
        public string Redirect { get; set; }
        public byte[] Data { get; set; }
        public string ContentType { get; set; }
        public Encoding Encoding { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public ResponsePacket()
        {
            //Error = Server.ServerError.OK;
            StatusCode = HttpStatusCode.OK;
        }
    }

    /// <summary>
    /// File extension information: .html, .js ....
    /// </summary>
    internal class ExtensionInfo
    {
        // Delegate Func<Tn, Tr>: passing function pointers
        public Func<string, string, ExtensionInfo, ResponsePacket> Loader { get; set; }
        public string ContentType { get; set; }
    }
}
