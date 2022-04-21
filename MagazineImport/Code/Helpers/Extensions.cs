using System;
using System.Data;
using SmartXLS;

namespace MagazineImport.Code.Helpers
{
    public static class Extensions
    {
        public static string RemoveHtmlText(this object obj)
        {
            string str = Convert.ToString(obj);
            str = System.Text.RegularExpressions.Regex.Replace(str, @"<(.|\n)*?>", string.Empty);
            return str;
        }

        
        /// <summary>
        /// WorkBook.ExportDataTable has a bug 
        /// where a column get empty values on all rows in DataTable
        /// if only the first row in WorkBook is empty.
        /// </summary>
        public static DataTable ExportDataTableFullFixed(this WorkBook wb, bool bitFirstRowAsHeader)
        {
            var dt = new DataTable(wb.getSheetName(wb.Sheet));
            var intRow = 0;
            //Headers from first row
            if(bitFirstRowAsHeader)
            {
                for(var col = 0; col < wb.LastCol+1;col++)
                {
                    dt.Columns.Add(wb.getText(intRow, col), typeof (string));
                }
                intRow++;
            }
            //Default headers
            else
            {
                for (var col = 0; col < wb.LastCol + 1; col++)
                {
                    dt.Columns.Add("Column " + (col+1), typeof(string));
                }
            }

            //Data
            while(intRow < wb.LastRow+1)
            {
                var row = dt.NewRow();
                for (var col = 0; col < wb.LastCol + 1; col++)
                {
                    row[col] = wb.getText(intRow, col);
                }
                dt.Rows.Add(row);
                intRow++;
            }

            return dt;
        }
    }
}
