using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using MagazineImport.Code.Helpers;
using MagazineImport.Code.Mapping;
using Serilog;
using SmartXLS;

namespace MagazineImport.Code.Importers
{
    public class PrenaxImporter : BaseMultiImporter
    {
        private const string strPathUpload = "C:\\ftpImport\\MagazineImport\\upload";
        private const string strPathArchive = "C:\\ftpImport\\MagazineImport\\archive";
        public const string strFilePrefix = "prenax_";

        protected override bool DoImport()
        {
            //Make sure paths exists
            if (!Directory.Exists(strPathUpload))
                Directory.CreateDirectory(strPathUpload);
            if (!Directory.Exists(strPathArchive))
                Directory.CreateDirectory(strPathArchive);

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
                using(var wb = new WorkBook())
                {
                    if (strFullFileName.EndsWith(".xlsx"))
                        wb.readXLSX(strFullFileName);
                    else
                        wb.read(strFullFileName);

                    var dt = wb.ExportDataTableFullFixed(true);
                    offers = dt.AsEnumerable().Select(dr => (IMagazineMapper)new PrenaxMapper(dr, Path.GetFileName(strFullFileName))).ToList();
                }

                ////TEST
                //foreach (var offer in offers)
                //{
                //     Log.Logger?.Information("{0}, in: {1}, out: {2}, curr: {3}", offer.ProductName, offer.InPrice, offer.Price, offer.CurrencyId);
                //}

                //Import
                bitReturn &= base.ImportToDatabase(offers);

                ArchiveAndLog(strFullFileName, strPathArchive);
            }
            
            return bitReturn;
        }
    }


    public class PrenaxMapper : MagazineMapperDefaults
    {
        private readonly string FileName;
        private readonly DataRow DataSource;
        private object Field(string column)
        {
            return DataSource != null && DataSource.Table.Columns.Contains(column) ? DataSource[column] : string.Empty;
        }

        public PrenaxMapper(DataRow dr, string strFileName)
        {
            FileName = strFileName;
            DataSource = dr;
        }

        private readonly string _importJobSuffix;
        private int _id = 0;
        public override int Id { get { return _id; } set { _id = value; } }
        public override string ImportJobId { get { return "Prenax" + _importJobSuffix; } }
        public override string ExternalId { get { return Convert.ToString(Field("ProductId")); } }
        public override string Issn { get { return Convert.ToString(Field("ISSN")); } }
        public override string ProductName { get { return Convert.ToString(Field("Title")); } }
        public override string Description { get { return Convert.ToString(Field("Description")); } }
        //public override string FirstIssue { get { return Convert.ToString(Field("DeliveryTime")).Replace("veckor", " ").Trim(); } }
        public override string FirstIssue { get { return "4-14"; } }
        public override int FreqPerYear { get { return Convert.ToInt32(Convert.ToString(Field("Frequency")) != "NULL" && !string.IsNullOrEmpty(Convert.ToString(Field("Frequency"))) ? Field("Frequency") : 0); } }

        public override string ExternalOfferId { get { return Convert.ToString(Field("DeliveryOptionId")); } }
        public override string CountryName { get { return Convert.ToString(Field("PublisherCountry")); } }
        public override string OfferIdPrepaid { get { return Convert.ToString(Field("DeliveryOptionId")); } }
        public override string CampaignIdPrepaid { get { return Convert.ToString(Field("DeliveryOption")); } }
        public override int SubscriptionLength 
        { 
            get
            {
                int length;
                if (int.TryParse(Convert.ToString(Field("OfferLength")), out length))
                    return length;
            
                return 0;
            } 
        }
        public override decimal InPrice { get { return Convert.ToDecimal(Field("Price").ToString().Replace(",", "."), CultureInfo.InvariantCulture); } }
        public override decimal Price { get { return Convert.ToDecimal(Field("Price").ToString().Replace(",", "."), CultureInfo.InvariantCulture); } }
        public override string ExtraInfo { get { return string.Empty; } }
        public override int CurrencyId
        {
            get
            {
                switch (Convert.ToString(Field("Valuta")))
                {
                    case "SEK" :
                        return 1;
                    case "NOK":
                        return 2;
                    case "EUR" :
                        return 3;
                    default:
                        return 1;
                }
            }
        }
        public override int CountryId
        {
            get
            {
                if (FileName.StartsWith(PrenaxImporter.strFilePrefix + "se_"))
                    return 1;
                if (FileName.StartsWith(PrenaxImporter.strFilePrefix + "no_"))
                    return 2;
                if (FileName.StartsWith(PrenaxImporter.strFilePrefix + "fi_"))
                    return 13;
                if (FileName.StartsWith(PrenaxImporter.strFilePrefix + "al_"))
                    return 35;

                return 1;
            }
        }
    }
}
