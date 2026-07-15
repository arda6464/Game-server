using System.Linq;

[HttpController]
public class ReportsController : BaseController
{
    [HttpRoute("GET", "/api/reports")]
    public object GetReports()
    {
        var reports = ReportManager.GetReports();
        return reports.Select(r => new
        {
            id = r.Id,
            type = r.Type ?? "Report",
            targetName = r.TargetName ?? "Bilinmiyor",
            targetId = r.TargetId ?? 0,
            reporterName = r.ReporterName ?? "Bilinmiyor",
            time = r.Timestamp.ToString("dd.MM.yyyy HH:mm"),
            status = r.Status,
            reason = r.Reason ?? "",
            context = r.Context.Select(m => new
            {
                senderName = m.SenderName ?? "",
                senderId = m.SenderId,
                content = m.Content ?? "",
                time = m.Time ?? ""
            }).ToList()
        }).ToList();
    }

    [HttpRoute("POST", "/api/reports/resolve")]
    public object ResolveReport()
    {
        var data = ReadFormData();
        if (data == null || !data.ContainsKey("id"))
            return Fail("Rapor ID gerekli.");

        string reportId = data["id"];
        if (string.IsNullOrEmpty(reportId))
            return Fail("Geçersiz rapor ID.");

        bool resolved = ReportManager.ResolveReport(reportId);
        if (!resolved)
            return Fail("Rapor bulunamadı.");

        Audit("Rapor Çözüldü", $"Rapor #{reportId}", "Admin tarafından çözüldü.");
        return new { success = true, message = "Rapor çözüldü." };
    }

    [HttpRoute("POST", "/api/reports/delete")]
    public object DeleteReport()
    {
        var data = ReadFormData();
        if (data == null || !data.ContainsKey("id"))
            return Fail("Rapor ID gerekli.");

        string reportId = data["id"];
        if (string.IsNullOrEmpty(reportId))
            return Fail("Geçersiz rapor ID.");

        bool deleted = ReportManager.DeleteReport(reportId);
        if (!deleted)
            return Fail("Rapor bulunamadı.");

        Audit("Rapor Silindi", $"Rapor #{reportId}", "Rapor kaydı sistemden kaldırıldı.");
        return new { success = true, message = "Rapor silindi." };
    }
}
