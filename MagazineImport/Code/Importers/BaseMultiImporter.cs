using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using MagazineImport.Code.Enum;
using MagazineImport.Code.Helpers;
using MagazineImport.Code.Mapping;
using Serilog;

namespace MagazineImport.Code.Importers
{
    /// <summary>
    /// Importer for more than 1 offer per product
    /// </summary>
    public abstract class BaseMultiImporter
    {
        protected DateTime datStartTime;
        protected DateTime datEndTime;

        private int intRowCount = 0;
        private int intNew = 0;
        private int intUpdate = 0;
        private int intDelete = 0;
        
        public bool Import()
        {
            datStartTime = DateTime.Now;
            Log.Logger?.Information("Running job '{0}' - {1}", GetType().Name, datStartTime);

            var b = DoImport();
            datEndTime = DateTime.Now;
            Log.Logger?.Information("Job '{0}' Completed, result: {3} - {1} ({2} seconds) ", GetType().Name, datEndTime, Convert.ToInt32((datEndTime - datStartTime).TotalSeconds), b);

            return b;
        }

        protected abstract bool DoImport();

        protected bool ImportToDatabase(List<IMagazineMapper> magazines)
        {
            intRowCount = magazines.Count();
            foreach (var magazine in magazines)
            {
                ImportMagazine(magazine);
            }
            RemoveDeletedMagazines(magazines);

            Log.Logger?.Information("Magazines in file {MagazinesInFileRowCount}, {MagazinesInFileNew} new magazines, {MagazinesInFileUpdated} updated magazines, {MagazinesInFileDeleted} deleted magazines.",
                intRowCount, intNew, intUpdate, intDelete);

            return true;
        }

        //Logging
        protected bool ArchiveAndLog(string strFullFileName, string strArchivePath)
        {
            var bitReturn = false;
            var strFileNameNew = FileHelper.GetValidFileName(strArchivePath, Path.GetFileName(strFullFileName));
            if (FileHelper.MoveFile(strFullFileName, Path.Combine(strArchivePath, strFileNameNew)))
                bitReturn = true;

            LogToDatabase(Path.GetFileName(strFullFileName), strFileNameNew);

            return bitReturn;
        }
        private void LogToDatabase(string strFileName, string strArchiveFileName)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("strFileName", strFileName);
            parameters.Add("strArchiveFileName", strArchiveFileName);
            parameters.Add("intUpdated", intUpdate);
            parameters.Add("intNew", intNew);
            parameters.Add("intDeleted", intDelete);
            parameters.Add("intMagazines", intRowCount);
            DbHelper.ExecuteNonQueryProcedure("sp_app_import_magazine_log", parameters);
        }

        //Import Methods
        private void ImportMagazine(IMagazineMapper magazine)
        {
            //Update import magazine
            if (UpdateImportMagazine(magazine))
            {//If imported to product
                //update product
                UpdateImportedProduct(magazine);
            }

            //Update magazine row.
            var updateType = UpdateImportMagazineRow(magazine);
            if (updateType == ImportType.Update && !magazine.ImportJobId.ToLower().Contains("hkc"))
                UpdateOffer(magazine);
            else if(updateType == ImportType.Import)
                AddOffer(magazine);
        }

        private bool UpdateImportMagazine(IMagazineMapper magazine)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("strImportJobId", magazine.ImportJobId);
            parameters.Add("strExternalId", magazine.ExternalId);
            parameters.Add("strISSN", magazine.Issn);
            parameters.Add("strProductName", magazine.ProductName);
            parameters.Add("intFreqPerYear", magazine.FreqPerYear);
            parameters.Add("strCountryName", magazine.CountryName);
            parameters.Add("strLanguage", magazine.Language);
            if (magazine.ImportJobId.ToLower().Contains("hkc"))
            {
                parameters.Add("bitReactivate", magazine.Stock > 50);
            }
            var intId = Convert.ToInt32(DbHelper.ExecuteScalar<object>("sp_app_import2_magazine_save", parameters));
            return intId > 0;
        }

        private void UpdateImportedProduct(IMagazineMapper magazine)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("strImportJobId", magazine.ImportJobId);
            parameters.Add("strExternalId", magazine.ExternalId);
            parameters.Add("strISSN", magazine.Issn);
            parameters.Add("intFreqPerYear", magazine.FreqPerYear);
            if(magazine.ImportJobId.ToLower().Contains("hkc"))
            {
                parameters.Add("intStock", magazine.Stock);
            }

            var intId = Convert.ToInt32(DbHelper.ExecuteScalar<object>("sp_app_import2_product_update", parameters));
        }

        private ImportType UpdateImportMagazineRow(IMagazineMapper magazine)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("strImportJobId", magazine.ImportJobId);
            parameters.Add("strExternalId", magazine.ExternalId);
            parameters.Add("strExternalOfferId", magazine.ExternalOfferId);

            parameters.Add("strDescription", magazine.Description);
            parameters.Add("strIssueImageUrl", magazine.IssueImageUrl);
            parameters.Add("datIssueDate", magazine.IssueDate);
            parameters.Add("strFirstIssue", magazine.FirstIssue);
            parameters.Add("strOfferId", magazine.OfferId);
            parameters.Add("strCampaignId", magazine.CampaignId);
            parameters.Add("strGiveId", magazine.GiveId);
            parameters.Add("strSpecialId", magazine.SpecialId);
            parameters.Add("strXmasOfferId", magazine.XmasOfferId);
            parameters.Add("strXmasCampaignId", magazine.XmasCampaignId);
            parameters.Add("strOfferIdPrepaid", magazine.OfferIdPrepaid);
            parameters.Add("strCampaignIdPrepaid", magazine.CampaignIdPrepaid);
            parameters.Add("strGiveIdPrepaid", magazine.GiveIdPrepaid);
            parameters.Add("strSpecialIdPrepaid", magazine.SpecialIdPrepaid);
            parameters.Add("strXmasOfferIdPrepaid", magazine.XmasOfferIdPrepaid);
            parameters.Add("strXmasCampaignIdPrepaid", magazine.XmasCampaignIdPrepaid);
            parameters.Add("intSubscriptionLength", magazine.SubscriptionLength);
            parameters.Add("monInPrice", magazine.InPrice);
            parameters.Add("monPrice", magazine.Price);
            parameters.Add("intCurrencyId", magazine.CurrencyId);
            parameters.Add("intCountryId", magazine.CountryId);
            parameters.Add("strExtraInfo", magazine.ExtraInfo);
            if (magazine.ImportJobId.ToLower().Contains("hkc")) {
                parameters.Add("intStock", magazine.Stock);
                parameters.Add("intVat", magazine.Vat);
            }

            var dr = DbHelper.GetRow("sp_app_import2_magazine_row_save", parameters);
            var intType = (ImportType)Convert.ToInt32(dr["intUpdateType"]);
            magazine.Id = Convert.ToInt32(dr["intId"]);
            return intType;
        }

        private void UpdateOffer(IMagazineMapper magazine)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("strImportJobId", magazine.ImportJobId);
            parameters.Add("strExternalId", magazine.ExternalId);
            parameters.Add("strExternalOfferId", magazine.ExternalOfferId);

            var intId = Convert.ToInt32(DbHelper.ExecuteScalar<object>("sp_app_import2_magazine_offer_update", parameters));
        }

        private void AddOffer(IMagazineMapper magazine)
        {
            var dr = DbHelper.GetRow("sp_app_import_magazine_row_get", new Dictionary<string, object> {{"intId", magazine.Id}});

            var parameters = new Dictionary<string, object>();
            parameters.Add("intId", magazine.Id);
            parameters.Add("intImportMagazineId",Convert.ToInt32(dr["intImportMagazineId"]));
            parameters.Add("intUserId", 0);

            dr = DbHelper.GetRow("sp_admin_import2_magazine_row_import", parameters);
        }

        private void RemoveDeletedMagazines(List<IMagazineMapper> magazines)
        {
            if(magazines.Count == 0)
                return;

            var strImportJobId = magazines.First().ImportJobId;
            var intCountryId = magazines.First().CountryId;

            var dtOffers = DbHelper.GetTable("sp_app_import2_magazine_rows_get", new Dictionary<string, object> { { "strImportJobId", strImportJobId }, { "intCountryId", intCountryId } });
            var deleted = dtOffers.AsEnumerable().
                    Where(r => !magazines.
                    Any(m => Convert.ToString(r["strExternalId"]).ToLower() == m.ExternalId.ToLower() && Convert.ToString(r["strExternalOfferId"]) == m.ExternalOfferId));
            foreach (var dr in deleted)
            {
                DbHelper.ExecuteNonQueryProcedure("sp_app_import2_magazine_row_delete", new Dictionary<string, object> { { "intId", Convert.ToInt32(dr["intId"]) }, { "strImportJobId", strImportJobId } });
                intDelete++;
            }
        }
    }
}
