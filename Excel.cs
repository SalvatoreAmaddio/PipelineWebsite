using OfficeOpenXml;

namespace Pipeline
{
    public class Excel : IDisposable
    {
        private readonly ExcelPackage _xlPackage;
        private ExcelWorksheet _worksheet = null!;

        public Excel()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            _xlPackage = new();
        }

        public void CreateWorkSheet(string sheetName) => _worksheet = _xlPackage.Workbook.Worksheets.Add(sheetName);
        public void SetColumnWidth(int col, double width) => _worksheet.Column(col).Width = width;
        public void SetHeaderColumn(int column, string value) => _worksheet.Cells[1, column].Value = value;
        public void SetHeaders(params string[] columns) 
        {
            for (int i = 1; i <= columns.Length; i++)
                _worksheet.Cells[1, i].Value = columns[i-1];
        }

        public void SetCellValue(int row, int columun, object value) => _worksheet.Cells[row, columun].Value = value;
        public void SetAutoFilters(int toCol) => _worksheet.Cells[1, 1, 1, toCol].AutoFilter = true;        
        public void AdjustColumnWidth() => _worksheet.Cells[_worksheet.Dimension.Address].AutoFitColumns();
        public void Save(string path) => _xlPackage.SaveAs(new FileInfo(path));
        public void Dispose()
        {
            _worksheet.Dispose();
            _xlPackage.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}