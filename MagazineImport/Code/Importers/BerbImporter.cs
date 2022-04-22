using MagazineImport.Code.Helpers;
using MagazineImport.Code.Mapping;
using Serilog;
using SmartXLS;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagazineImport.Code.Importers
{
    public class BerbImporter : BaseMultiImporter
    {
        private const string strPathUpload = "C:\\ftpImport\\MagazineImport\\upload";
        private const string strPathArchive = "C:\\ftpImport\\MagazineImport\\archive";
        public const string strFilePrefix = "berb_";
       
        protected override bool DoImport()
        {
            
            BlobHelper.UploadBlob(strPathUpload);

            //Get all excel files in path
            var filePaths = Directory.GetFiles(strPathUpload)
                .Where(s => (s.EndsWith(".xls") || s.EndsWith(".xlsx")) && Path.GetFileName(s).StartsWith(strFilePrefix))
                .ToList();

            if (filePaths.Count == 0)
            {
                Log.Logger?.Information("No Magazine files with prefix '{MagazineImportFilePrefix}' to import!", strFilePrefix);
                return true;
            }

            List<IMagazineMapper> offers;
            var bitReturn = true;

            //Import all files
            foreach (var strFullFileName in filePaths)
            {
                Log.Logger?.Information("File: {PrenaxImportFileName}", strFullFileName);

                //Init work book
                using (var workBook = new WorkBook())
                {
                    if (strFullFileName.EndsWith(".xlsx"))
                        workBook.readXLSX(strFullFileName);
                    else
                        workBook.read(strFullFileName);

                    var dataRow = workBook.ExportDataTableFullFixed(true);
                    offers = dataRow.AsEnumerable().Select(dr => (IMagazineMapper) new PrenaxMapper(dr, Path.GetFileName(strFullFileName))).ToList();
                }

                bitReturn &= base.ImportToDatabase(offers);

                ArchiveAndLog(strFullFileName, strPathArchive);
            }

            return bitReturn;
        }
    }
}
