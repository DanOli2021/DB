using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Data;
using DocumentFormat.OpenXml.Wordprocessing;
using AngelDBTools;
using Python.Runtime;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace AngelDB
{
    public static class OpenDocuments
    {

        public static string ReadExcelAsJson(string fname, AngelDB.DB db, string as_table, bool firstRowIsHeader = true, string sheet_name = "") 
        {
            try
            {

                System.Data.DataTable t = ReadExcelSheet(fname, firstRowIsHeader, sheet_name);

                if (t is null)
                {
                    return "[]";
                }

                string columns = "";

                if (as_table != "null")
                {

                    if (t.Columns.Count == 1 && t.Columns[0].ColumnName.Trim().ToLower() == "id")
                    {
                        return "Error: At least one other column is needed in addition to the Id column";
                    }

                    foreach (DataColumn c in t.Columns)
                    {
                        c.ColumnName = StringFunctions.ConvertStringToDbColumn(c.ColumnName);
                    }

                    foreach (DataColumn c in t.Columns)
                    {
                        if (c.ColumnName.Trim().ToLower() == "id") 
                        {
                            continue;
                        }
                        columns += c.ColumnName + ",";
                    }

                    columns = columns.TrimEnd(',');
                    
                    string result = db.Prompt($"CREATE TABLE {as_table} FIELD LIST {columns}");

                    if (result.StartsWith("Error:"))
                    {                        
                        return result;
                    }

                    result = db.Prompt($"UPSERT INTO {as_table} VALUES {JsonConvert.SerializeObject(t, Newtonsoft.Json.Formatting.Indented)}" );
                    return result;

                }

                return JsonConvert.SerializeObject(t, Newtonsoft.Json.Formatting.Indented);

            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }


        public static string ReadExcelAsJsonOnMemory(string fname, AngelDB.DB db, string as_table, bool firstRowIsHeader = true, string sheet_name = "")
        {
            try
            {

                System.Data.DataTable t = ReadExcelSheet(fname, firstRowIsHeader, sheet_name);

                if (t is null)
                {
                    return "[]";
                }

                string columns = "";

                if (as_table != "null")
                {

                    foreach (DataColumn c in t.Columns)
                    {
                        c.ColumnName = StringFunctions.ConvertStringToDbColumn(c.ColumnName);
                    }

                    foreach (DataColumn c in t.Columns)
                    {
                        columns += c.ColumnName + ",";
                    }

                    columns = columns.TrimEnd(',');

                    string result = db.Prompt($"GRID CREATE TABLE {as_table} ( {columns} )");

                    if (result.StartsWith("Error:"))
                    {
                        return result;
                    }

                    foreach (DataRow r in t.Rows)
                    {
                        db.Grid.Reset();
                        db.Grid.CreateInsert(as_table);

                        foreach (DataColumn c in t.Columns)
                        {                            
                            db.Grid.AddField(c.ColumnName, r[c.ColumnName].ToString().Trim());
                        }

                        result = db.Grid.Exec();

                        if (result.StartsWith("Error:"))
                        {
                            return "Error: ReadExcelAsJsonOnMemory: Insert Data " + result;
                        }
                    }

                    return "Ok.";

                }

                return JsonConvert.SerializeObject(t, Newtonsoft.Json.Formatting.Indented);

            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }




        public static string CreateExcelFromJson(string fname, AngelDB.DB db, string json) 
        {
            try
            {
                System.Data.DataTable t = JsonConvert.DeserializeObject<System.Data.DataTable>(json);
                CreateExcelFile(t, fname);                    
                return "Ok.";
            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }

        public static System.Data.DataTable ReadExcelSheet(string fname, bool firstRowIsHeader = true, string sheet_name = "")
        {
            List<string> Headers = new List<string>();
            System.Data.DataTable dt = new System.Data.DataTable();
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(fname, false))
            {
                //Read the first Sheets 
                Sheet sheet = doc.WorkbookPart.Workbook.Sheets.GetFirstChild<Sheet>();

                if (sheet_name == "null")
                {
                    sheet_name = "";
                }

                if (sheet_name != "")
                {
                    sheet = doc.WorkbookPart.Workbook.Sheets.Elements<Sheet>().Where(s => s.Name.ToString().Trim().ToLower() == sheet_name.Trim().ToLower()).FirstOrDefault();
                }

                if (sheet is null) 
                {
                    return null;
                }
                
                Worksheet worksheet = (doc.WorkbookPart.GetPartById(sheet.Id.Value) as WorksheetPart).Worksheet;                
                IEnumerable<Row> rows = worksheet.GetFirstChild<SheetData>().Descendants<Row>();
                
                int counter = 0;
                
                foreach (Row row in rows)
                {
                    counter = counter + 1;
                    //Read the first row as header
                    if (counter == 1)
                    {
                        var j = 1;
                        foreach (Cell cell in row.Descendants<Cell>())
                        {

                            string column_name = GetCellValue(doc, cell);


                            if( column_name is null)
                            {
                                column_name = "Field" + j++;
                            }

                            if (column_name.Trim().ToLower() == "id") 
                            {
                                column_name = "id";
                            }
                            
                            var colunmName = firstRowIsHeader ? GetCellValue(doc, cell) : "Field" + j++;
                            Headers.Add(colunmName);
                            dt.Columns.Add(colunmName);
                        }
                    }
                    else
                    {
                        dt.Rows.Add();
                        int i = 0;
                        foreach (Cell cell in row.Descendants<Cell>())
                        {
                            try
                            {
                                dt.Rows[dt.Rows.Count - 1][i] = GetCellValue(doc, cell);
                                i++;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error: ReadExcelSheet: " + e.ToString());
                                return null;
                                
                            }
                        }
                    }
                }

            }
            return dt;
        }

        public static string CreateExcelFile(System.Data.DataTable table, string destination)
        {
            try
            {
                var ds = new DataSet();
                ds.Tables.Add(table);
                ExportDSToExcel(ds, destination);
                return "Ok.";

            }
            catch (Exception e)
            {
                return $"Error: {e}";
            }
        }

        private static object GetCellSheetValue(SpreadsheetDocument doc, Cell cell)
        {

            if (cell == null)
            {
                return null;
            }

            if (cell.CellValue == null)
            {
                return null;
            }

            return cell.CellValue;

        }

        private static string GetCellValue(SpreadsheetDocument doc, Cell cell)
        {

            if (cell == null)
            {
                return null;
            }

            if( cell.CellValue == null)
            {
                return null;
            }

            string value = cell.CellValue.InnerText;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return doc.WorkbookPart.SharedStringTablePart.SharedStringTable.ChildElements.GetItem(int.Parse(value)).InnerText;
            }

            return value;

        }

        public static void ExportDSToExcel(DataSet ds, string destination)
        {
            // https://stackoverflow.com/questions/11811143/export-datatable-to-excel-with-open-xml-sdk-in-c-sharp
            using (var workbook = SpreadsheetDocument.Create(destination, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = workbook.AddWorkbookPart();
                workbook.WorkbookPart.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook();
                workbook.WorkbookPart.Workbook.Sheets = new DocumentFormat.OpenXml.Spreadsheet.Sheets();

                uint sheetId = 1;

                foreach (System.Data.DataTable table in ds.Tables)
                {
                    var sheetPart = workbook.WorkbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new DocumentFormat.OpenXml.Spreadsheet.SheetData();
                    sheetPart.Worksheet = new DocumentFormat.OpenXml.Spreadsheet.Worksheet(sheetData);

                    DocumentFormat.OpenXml.Spreadsheet.Sheets sheets = workbook.WorkbookPart.Workbook.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.Sheets>();
                    string relationshipId = workbook.WorkbookPart.GetIdOfPart(sheetPart);

                    if (sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>().Count() > 0)
                    {
                        sheetId =
                            sheets.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>().Select(s => s.SheetId.Value).Max() + 1;
                    }

                    DocumentFormat.OpenXml.Spreadsheet.Sheet sheet = new DocumentFormat.OpenXml.Spreadsheet.Sheet() { Id = relationshipId, SheetId = sheetId, Name = table.TableName };
                    sheets.Append(sheet);

                    DocumentFormat.OpenXml.Spreadsheet.Row headerRow = new DocumentFormat.OpenXml.Spreadsheet.Row();

                    List<String> columns = new List<string>();
                    foreach (DataColumn column in table.Columns)
                    {
                        columns.Add(column.ColumnName);

                        DocumentFormat.OpenXml.Spreadsheet.Cell cell = new DocumentFormat.OpenXml.Spreadsheet.Cell();
                        cell.DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String;
                        cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(column.ColumnName);
                        headerRow.AppendChild(cell);
                    }

                    sheetData.AppendChild(headerRow);

                    foreach (DataRow dsrow in table.Rows)
                    {
                        DocumentFormat.OpenXml.Spreadsheet.Row newRow = new DocumentFormat.OpenXml.Spreadsheet.Row();
                        foreach (String col in columns)
                        {
                            DocumentFormat.OpenXml.Spreadsheet.Cell cell = new DocumentFormat.OpenXml.Spreadsheet.Cell();
                            cell.DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.String;
                            cell.CellValue = new DocumentFormat.OpenXml.Spreadsheet.CellValue(dsrow[col].ToString()); //
                            newRow.AppendChild(cell);
                        }

                        sheetData.AppendChild(newRow);
                    }
                }
            }
        }
    }
}

