using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MagazineImport.Code.Helpers
{
    public static class WebHelper
    {

        public static byte[] DownloadImage(string strUrl)
        {
            byte[] buffer = null;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(strUrl);
                request.AllowWriteStreamBuffering = true;

                // set timeout for 20 seconds (Optional) . 
                request.Timeout = 20000;

                // Request response: . 
                using (var response = request.GetResponse())
                {
                    // Open data stream: 
                    using (var responseStream = response.GetResponseStream())
                    {
                        buffer = ReadFully(responseStream);
                    }
                }
            }
            catch (Exception e)
            {


            }

            return buffer;

        }
        public static byte[] ReadFully(Stream input)
        {
            var buffer = new byte[input.Length];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
