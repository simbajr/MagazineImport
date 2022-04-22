using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using System.Configuration;

namespace MagazineImport.Code.Helpers
{
    public static class BlobHelper
    {
        public static void UploadBlob(string blob)
        {
            FileStream uploadFileStream = null ;
            var connectionString = ConfigurationManager.AppSettings["BlobConnectionString"];
            string containerName = ConfigurationManager.AppSettings["BlobContainerName"];
            var serviceClient = new BlobServiceClient(connectionString);
            var containerClient = serviceClient.GetBlobContainerClient(containerName);
            var path = @"c:\temp";
            var fileName = blob;
            var localFile = Path.Combine(path, fileName);
            try
            {
                File.WriteAllText(localFile, "This is a test message");
                var blobClient = containerClient.GetBlobClient(fileName);
                Console.WriteLine("Uploading to Blob storage");
                uploadFileStream = File.OpenRead(localFile);
                blobClient.UploadAsync(uploadFileStream, true);
            }
            catch (Exception exception)
            {

                throw exception;
            }
            finally
            {
                uploadFileStream.Close();
            }

        }
    }
}
