namespace PMS.API.Models;

public class ExcelImportRequest
{
    public string FilePath { get; set; } = string.Empty;
    public List<string> FilePaths { get; set; } = [];
}
