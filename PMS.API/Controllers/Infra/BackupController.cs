using Microsoft.AspNetCore.Mvc;
using PMS.API.Models;
using PMS.Infrastructure.Services;

namespace PMS.API.Controllers;

[ApiController]
[Route("api/system/backup")]
public class BackupController : ControllerBase
{
    [HttpGet("download")]
    public IActionResult Download()
    {
        var dbPath = SqliteJsonStore.GetDbPath();
        if (!System.IO.File.Exists(dbPath))
        {
            return NotFound(new { code = 404, message = "数据库文件不存在" });
        }

        var backupPath = Path.Combine(Path.GetTempPath(), $"pms-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        try
        {
            // Use SQLite VACUUM INTO for a consistent snapshot
            using var conn = SqliteJsonStore.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"VACUUM INTO '{backupPath.Replace("'", "''")}'";
            cmd.ExecuteNonQuery();

            var bytes = System.IO.File.ReadAllBytes(backupPath);
            return File(bytes, "application/octet-stream", $"pms-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        }
        finally
        {
            if (System.IO.File.Exists(backupPath))
            {
                try { System.IO.File.Delete(backupPath); } catch { /* ignore cleanup failures */ }
            }
        }
    }

    [HttpPost("restore")]
    public async Task<IActionResult> Restore(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { code = 400, message = "请上传备份文件" });
        }

        if (!file.FileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { code = 400, message = "仅支持 .db 格式的备份文件" });
        }

        // Validate uploaded file is a valid SQLite database
        var tempUpload = Path.Combine(Path.GetTempPath(), $"pms-restore-{Guid.NewGuid():N}.db");
        try
        {
            await using (var fs = new FileStream(tempUpload, FileMode.Create))
            {
                await file.CopyToAsync(fs, cancellationToken);
            }

            // Quick validation: try opening and reading
            using (var testConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={tempUpload};Mode=ReadOnly"))
            {
                testConn.Open();
                using var cmd = testConn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM AppState";
                cmd.ExecuteScalar(); // Will throw if not a valid PMS db
            }

            // Replace current database
            var dbPath = SqliteJsonStore.GetDbPath();
            var backupOriginal = dbPath + $".pre-restore-{DateTime.Now:yyyyMMdd-HHmmss}";
            if (System.IO.File.Exists(dbPath))
            {
                System.IO.File.Copy(dbPath, backupOriginal, overwrite: true);
            }

            System.IO.File.Copy(tempUpload, dbPath, overwrite: true);

            return Ok(ApiResponse<object>.Success(new
            {
                message = "数据恢复成功，请重启服务以加载新数据",
                backupOf = Path.GetFileName(backupOriginal)
            }));
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            return BadRequest(new { code = 400, message = "上传的文件不是有效的PMS数据库备份" });
        }
        finally
        {
            if (System.IO.File.Exists(tempUpload))
            {
                try { System.IO.File.Delete(tempUpload); } catch { /* ignore */ }
            }
        }
    }
}
