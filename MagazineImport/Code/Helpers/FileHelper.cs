using Serilog;
using System;
using System.IO;

namespace MagazineImport.Code.Helpers
{
    public class FileHelper
    {
        public static bool SaveFile(string strSavePath, Stream fileStream)
        {
            return SaveFile(strSavePath, fileStream, null);
        }

        public static bool SaveFile(string strSavePath, Stream fileStream, decimal? decFileSize)
        {
            var file = File.Create(strSavePath);

            Log.Logger?.Information(string.Format("Saving {0}..", strSavePath));
            
            Console.WriteLine(string.Format("Saving {0}..", strSavePath));

            //Save file
            var buffer = new byte[32 * 1024];
            int read;
            long current = 0;

            if(decFileSize.HasValue)
                Console.Write(string.Format("{0}%", current));
            else
                Console.Write(App.Loading[current]);

            while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (decFileSize.HasValue)
                    Console.Write(string.Format("\r{0}%", Convert.ToInt32((current / decFileSize * 100))));
                else
                    Console.Write("\r" + App.Loading[current % App.Loading.Length]);


                
                file.Write(buffer, 0, read);
                current += read;
            }
            if (decFileSize.HasValue)
                Console.WriteLine("\r100%");
            else
                Console.Write("\rdone");
            

            file.Close();

            Log.Logger?.Information(string.Format("File saved. ", strSavePath));

            return true;
        }

        public static byte[] GetFile(Stream fileStream)
        {
            return GetFile(fileStream, null);
        }

        public static byte[] GetFile(Stream fileStream, decimal? decFileSize)
        {
            long current = 0;

            if (decFileSize.HasValue)
                Console.Write(string.Format("{0}%", current));
            else
                Console.Write(App.Loading[current]);

            var buffer = new byte[16 * 1024]; 
            using (var ms = new MemoryStream()) 
            { 
                int read;
                while ((read = fileStream.Read(buffer, 0, buffer.Length)) > 0) 
                {
                    if (decFileSize.HasValue)
                        Console.Write(string.Format("\r{0}%", Convert.ToInt32((current / decFileSize * 100))));
                    else
                        Console.Write("\r" + App.Loading[current % App.Loading.Length]);

                    ms.Write(buffer, 0, read); 
                }

                if (decFileSize.HasValue)
                    Console.WriteLine("\r100%");
                else
                    Console.Write("\rdone");

                return ms.ToArray(); 
            } 
        }

        public static bool MoveFile(string strFilePath, string strNewPath)
        {
            var bitResult = false;
            try
            {
                File.Move(strFilePath, strNewPath);
                bitResult = true;
            }
            catch (Exception ex)
            {
                Log.Logger?.Error(ex, "MoveFile Failed. \nFrom Path: {FilePath}\nTo Path:{NewFilePath}", strFilePath, strNewPath);
            }
            
            return bitResult;
        }

        public static string GetValidFileName(string strNewPath, string strFileName)
        {
            var strFileNameNoExt = Path.GetFileNameWithoutExtension(strFileName);
            var strExtension = Path.GetExtension(strFileName);

            var strCount = string.Empty;
            for (var i = 1; File.Exists(Path.Combine(strNewPath, strFileNameNoExt + strCount + strExtension)); i++)
                strCount = " (" + i + ")";
            return Path.Combine(strNewPath, strFileNameNoExt + strCount + strExtension);
        }
    }
}
