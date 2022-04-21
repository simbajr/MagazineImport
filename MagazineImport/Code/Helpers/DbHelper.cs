using System;
using System.Collections.Generic;
using System.Data;

namespace MagazineImport.Code.Helpers
{
    public class DbHelper
    {
        internal static DataRow GetRow(string v, Dictionary<string, object> parameters) =>
            throw new NotImplementedException();
        internal static void ExecuteNonQueryProcedure(string v, Dictionary<string, object> parameters) =>
            throw new NotImplementedException();
        internal static T ExecuteScalar<T>(string v, Dictionary<string, T> parameters) =>
            throw new NotImplementedException();
        internal static DataTable GetTable(string v, Dictionary<string, object> dictionary) =>
            throw new NotImplementedException();
    }
}
