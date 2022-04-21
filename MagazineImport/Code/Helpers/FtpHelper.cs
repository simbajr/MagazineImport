using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace MagazineImport.Code.Helpers
{
    class FtpHelper
    {
        private string _strRemoteHost, _strRemoteUser, _strRemotePassword;
        public FtpHelper(string strRemoteHost, string strRemoteUser, string strRemotePassword) {
            _strRemoteHost = strRemoteHost;
            _strRemoteUser = strRemoteUser;
            _strRemotePassword = strRemotePassword;
        }

        public string GetStringFromXmlFile(string strPath, string strFileName) {
            string strXml = String.Empty;

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(String.Format("{0}{1}{2}", _strRemoteHost, strPath, strFileName));
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(_strRemoteUser, _strRemotePassword);
            
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
                        
            using (StreamReader reader = new StreamReader(responseStream, Encoding.Unicode)) {
                strXml = reader.ReadToEnd();
            }
            return strXml;
        }

    }
}
