using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using MagazineImport.Code.Helpers;
using MagazineImport.Code.Mapping;
using Serilog;

namespace MagazineImport.Code.Importers
{
    
    public abstract class BaseImporter
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
            var parameters = new Dictionary<string, object>();
            parameters.Add("strImportJobId", magazine.ImportJobId);
            parameters.Add("strExternalId", magazine.ExternalId);
            var dr = DbHelper.GetRow("sp_app_import_magazine_imported", parameters);
            if (dr == null)
            {
                //Adds new row or updates unimported magazine
                if (SaveForImport(magazine))
                    intNew++;
            }
            else
            {
                if (dr["datImported"] == DBNull.Value || Convert.ToInt32(dr["intImportedToProductId"]) == 0)
                    return;
                
                if (UpdateMagazine(magazine, dr))
                    intUpdate++;
            }
        }
        private bool SaveForImport(IMagazineMapper magazine)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("strImportJobId", magazine.ImportJobId);
            parameters.Add("strExternalId", magazine.ExternalId);
            parameters.Add("strISSN", magazine.Issn);
            parameters.Add("strProductName", magazine.ProductName);
            parameters.Add("intFreqPerYear", magazine.FreqPerYear);
            parameters.Add("strCountryName", magazine.CountryName);
            parameters.Add("strLanguage", magazine.Language);
            
            var intId = Convert.ToInt32(DbHelper.ExecuteScalar<object>("sp_app_import_magazine_unimported_save", parameters));

            if (intId > 0)
            {
                parameters = new Dictionary<string, object>();
                parameters.Add("intImportMagazineId", intId);
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

                var importRowId = Convert.ToInt32(DbHelper.ExecuteScalar<object>("sp_app_import_magazine_row_unimported_save", parameters));
                
                if(importRowId > 0)
                    return true;
            }
            return false;
        }
        private bool UpdateMagazine(IMagazineMapper magazineNew, DataRow drExisting)
        {
            var bitUpdated = false;

            //Update Product
            if (Convert.ToString(drExisting["strISSN"]) != magazineNew.Issn ||
                Convert.ToString(drExisting["strProductName"]) != magazineNew.ProductName ||
                Convert.ToInt32(drExisting["intFreqPerYear"]) != magazineNew.FreqPerYear ||
                Convert.ToString(drExisting["strCountryName"]) != magazineNew.CountryName ||
                Convert.ToString(drExisting["strLanguage"]) != magazineNew.Language)
            {

                var parameters = new Dictionary<string, object>();
                parameters.Add("strImportJobId", magazineNew.ImportJobId);
                parameters.Add("strExternalId", magazineNew.ExternalId);
                parameters.Add("strISSN", magazineNew.Issn);
                parameters.Add("strProductName", magazineNew.ProductName);
                parameters.Add("intFreqPerYear", magazineNew.FreqPerYear);
                parameters.Add("strCountryName", magazineNew.CountryName);
                parameters.Add("strLanguage", magazineNew.Language);

                var productId = Convert.ToInt32(DbHelper.ExecuteScalar<object>("sp_app_import_magazine_update", parameters));
                bitUpdated = productId > 0;
            }

            //Set active if deleted and force offer update
            if (Convert.ToBoolean(drExisting["bitDeleted"]))
            {
                DbHelper.ExecuteNonQueryProcedure("sp_app_import_reactivate_magazine", new Dictionary<string, object> { { "strImportJobId", magazineNew.ImportJobId }, { "strExternalId", magazineNew.ExternalId } });
            }

            bitUpdated |= UpdateOffer(magazineNew, Convert.ToInt32(drExisting["intId"]));

            return bitUpdated;
        }
        private void RemoveDeletedMagazines(List<IMagazineMapper> magazines)
        {
            if(magazines.Count == 0)
                return;

            var strImportJobId = magazines.First().ImportJobId;
            var intCountryId = magazines.First().CountryId;

            var dtOffers = DbHelper.GetTable("sp_app_import_magazine_rows_get", new Dictionary<string, object> { { "strImportJobId", strImportJobId }, { "intCountryId", intCountryId } });
            var deleted = dtOffers.AsEnumerable()
                            .Where(r => !magazines.Any(m => Convert.ToString(r["strExternalId"]).ToLower() == m.ExternalId.ToLower() && Convert.ToInt32(r["intCurrencyId"]) == m.CurrencyId));
            foreach (var dr in deleted)
            {
                DbHelper.ExecuteNonQueryProcedure("sp_app_import_magazine_row_delete", new Dictionary<string, object> { { "intId", Convert.ToInt32(dr["intId"]) }, { "strImportJobId", strImportJobId } });
                intDelete++;
            }
            
        }

        private static bool UpdateOffer(IMagazineMapper magazineNew, int intImportMagazineId)
        {
            bool bitUpdated = false;
            var bitForceOfferUpdate = false;

            var parameters = new Dictionary<string, object>();
            parameters.Add("intImportMagazineId", intImportMagazineId);
            parameters.Add("intCurrencyId", magazineNew.CurrencyId);
            parameters.Add("intCountryId", magazineNew.CountryId);
            var drExisting = DbHelper.GetRow("sp_app_import_magazine_row_imported", parameters);

            if (drExisting == null)
            {
                parameters = new Dictionary<string, object>();

                parameters.Add("intImportMagazineId", intImportMagazineId);
                parameters.Add("strDescription", magazineNew.Description);
                parameters.Add("strIssueImageUrl", magazineNew.IssueImageUrl);
                parameters.Add("datIssueDate", magazineNew.IssueDate);
                parameters.Add("strFirstIssue", magazineNew.FirstIssue);
                parameters.Add("strOfferId", magazineNew.OfferId);
                parameters.Add("strCampaignId", magazineNew.CampaignId);
                parameters.Add("strGiveId", magazineNew.GiveId);
                parameters.Add("strSpecialId", magazineNew.SpecialId);
                parameters.Add("strXmasOfferId", magazineNew.XmasOfferId);
                parameters.Add("strXmasCampaignId", magazineNew.XmasCampaignId);
                parameters.Add("strOfferIdPrepaid", magazineNew.OfferIdPrepaid);
                parameters.Add("strCampaignIdPrepaid", magazineNew.CampaignIdPrepaid);
                parameters.Add("strGiveIdPrepaid", magazineNew.GiveIdPrepaid);
                parameters.Add("strSpecialIdPrepaid", magazineNew.SpecialIdPrepaid);
                parameters.Add("strXmasOfferIdPrepaid", magazineNew.XmasOfferIdPrepaid);
                parameters.Add("strXmasCampaignIdPrepaid", magazineNew.XmasCampaignIdPrepaid);
                parameters.Add("intSubscriptionLength", magazineNew.SubscriptionLength);
                parameters.Add("monInPrice", magazineNew.InPrice);
                parameters.Add("monPrice", magazineNew.Price);
                parameters.Add("intCurrencyId", magazineNew.CurrencyId);
                parameters.Add("intCountryId", magazineNew.CountryId);
                parameters.Add("strExtraInfo", magazineNew.ExtraInfo);

                DbHelper.ExecuteNonQueryProcedure("sp_app_import_magazine_row_unimported_save", parameters);
                bitUpdated = true;
            }
            else if (Convert.ToBoolean(drExisting["bitDeleted"]))
            {
                parameters = new Dictionary<string, object>();
                parameters.Add("strImportJobId", magazineNew.ImportJobId);
                parameters.Add("strExternalId", magazineNew.ExternalId);
                parameters.Add("intCurrencyId", magazineNew.CurrencyId);
                parameters.Add("intCountryId", magazineNew.CountryId);
                DbHelper.ExecuteNonQueryProcedure("sp_app_import_reactivate_magazine_row", parameters);
                bitForceOfferUpdate = drExisting["datImported"] != DBNull.Value;
            }

            if (bitForceOfferUpdate || (drExisting != null && (Convert.ToDecimal(drExisting["monInPrice"]) != Convert.ToDecimal(magazineNew.InPrice) || magazineNew.SubscriptionLength != Convert.ToInt32(drExisting["intSubscriptionLength"]))))
            {
                //update
                parameters = new Dictionary<string, object>();

                parameters.Add("intImportMagazineId", intImportMagazineId);
                parameters.Add("strDescription", magazineNew.Description);
                parameters.Add("strIssueImageUrl", magazineNew.IssueImageUrl);
                parameters.Add("datIssueDate", magazineNew.IssueDate);
                parameters.Add("strFirstIssue", magazineNew.FirstIssue);
                parameters.Add("strOfferId", magazineNew.OfferId);
                parameters.Add("strCampaignId", magazineNew.CampaignId);
                parameters.Add("strGiveId", magazineNew.GiveId);
                parameters.Add("strSpecialId", magazineNew.SpecialId);
                parameters.Add("strXmasOfferId", magazineNew.XmasOfferId);
                parameters.Add("strXmasCampaignId", magazineNew.XmasCampaignId);
                parameters.Add("strOfferIdPrepaid", magazineNew.OfferIdPrepaid);
                parameters.Add("strCampaignIdPrepaid", magazineNew.CampaignIdPrepaid);
                parameters.Add("strGiveIdPrepaid", magazineNew.GiveIdPrepaid);
                parameters.Add("strSpecialIdPrepaid", magazineNew.SpecialIdPrepaid);
                parameters.Add("strXmasOfferIdPrepaid", magazineNew.XmasOfferIdPrepaid);
                parameters.Add("strXmasCampaignIdPrepaid", magazineNew.XmasCampaignIdPrepaid);
                parameters.Add("intSubscriptionLength", magazineNew.SubscriptionLength);
                parameters.Add("monInPrice", magazineNew.InPrice);
                parameters.Add("monPrice", magazineNew.Price);
                parameters.Add("intCurrencyId", magazineNew.CurrencyId);
                parameters.Add("intCountryId", magazineNew.CountryId);
                parameters.Add("strExtraInfo", magazineNew.ExtraInfo);

                DbHelper.ExecuteNonQueryProcedure("sp_app_import_magazine_row_update", parameters);
                bitUpdated = true;
            }
            return bitUpdated;
        }
    }
}
