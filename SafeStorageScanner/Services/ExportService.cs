using System.Text;
using System.Text.Json;
using System.IO;
using SafeStorageScanner.Models;

namespace SafeStorageScanner.Services;

public sealed class ExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public void ExportFilesCsv(string path, IEnumerable<FileSystemScanItem> files)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Kind,Path,SizeBytes,Size,FileType,Modified,Created,LastAccessed,Category,Explanation");
        foreach (var item in files)
        {
            sb.AppendLine(string.Join(",", Csv(item.ItemKind), Csv(item.Path), item.SizeBytes, Csv(item.SizeText), Csv(item.FileType),
                Csv(item.Modified?.ToString("O") ?? ""), Csv(item.Created?.ToString("O") ?? ""), Csv(item.LastAccessed?.ToString("O") ?? ""),
                Csv(item.CategoryText), Csv(item.Explanation)));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    public void ExportFilesJson(string path, IEnumerable<FileSystemScanItem> files)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(files, JsonOptions), Encoding.UTF8);
    }

    public void ExportDuplicatesJson(string path, IEnumerable<DuplicateGroup> duplicates)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(duplicates, JsonOptions), Encoding.UTF8);
    }

    public void ExportHtml(string path, IEnumerable<RecommendationItem> recommendations)
    {
        var rows = string.Join(Environment.NewLine, recommendations.Select(r =>
            $"<tr><td>{Html(r.Kind)}</td><td>{Html(r.Path)}</td><td>{Html(r.EstimatedRecoverableText)}</td><td>{r.ConfidenceScore}</td><td>{Html(r.RecommendedAction)}</td><td>{Html(r.Evidence)}</td></tr>"));
        var html = $$"""
        <!doctype html>
        <html>
        <head><meta charset="utf-8"><title>Safe Storage Scanner Report</title>
        <style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px}table{border-collapse:collapse;width:100%}td,th{border:1px solid #ccc;padding:6px;text-align:left}th{background:#f1f5f9}</style></head>
        <body><h1>Safe Storage Scanner Report</h1><p>Dry-run recommendations only. Review every item before action.</p>
        <table><thead><tr><th>Kind</th><th>Path</th><th>Recoverable</th><th>Confidence</th><th>Action</th><th>Evidence</th></tr></thead><tbody>{{rows}}</tbody></table>
        </body></html>
        """;
        File.WriteAllText(path, html, Encoding.UTF8);
    }

    public void ExportPdf(string path, IEnumerable<RecommendationItem> recommendations)
    {
        var lines = new List<string>
        {
            "Safe Storage Scanner Report",
            $"Generated {DateTime.Now:g}",
            "Dry-run recommendations only. Review every item before action.",
            ""
        };
        lines.AddRange(recommendations.Take(40).Select(r =>
            $"{r.EstimatedRecoverableText} | {r.ConfidenceScore}/100 | {r.RecommendedAction} | {r.Path}"));

        var content = new StringBuilder();
        content.AppendLine("BT");
        content.AppendLine("/F1 10 Tf");
        content.AppendLine("36 760 Td");
        foreach (var line in lines)
        {
            content.AppendLine($"({Pdf(line)}) Tj");
            content.AppendLine("0 -16 Td");
        }
        content.AppendLine("ET");

        var stream = content.ToString();
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream"
        };

        using var file = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(file, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
        writer.WriteLine("%PDF-1.4");
        writer.Flush();
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(file.Position);
            writer.WriteLine($"{i + 1} 0 obj");
            writer.WriteLine(objects[i]);
            writer.WriteLine("endobj");
            writer.Flush();
        }

        var xref = file.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
        {
            writer.WriteLine($"{offset:0000000000} 00000 n ");
        }
        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xref);
        writer.WriteLine("%%EOF");
    }

    private static string Html(string value) => System.Net.WebUtility.HtmlEncode(value);
    private static string Pdf(string value) => value
        .Replace("\\", "\\\\")
        .Replace("(", "\\(")
        .Replace(")", "\\)")
        .Replace("\r", " ")
        .Replace("\n", " ");

    private static string Csv(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
