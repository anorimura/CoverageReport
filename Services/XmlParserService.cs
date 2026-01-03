using System;
using System.Linq;
using System.Xml.Linq;
using CoverageReport.Models;

namespace CoverageReport.Services
{
    public class XmlParserService
    {
        public async Task<ReportHistory> ParseAsync(string xmlContent, IProgress<double>? progress = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // XDocument.Parse can be slow for very large files
            var doc = await Task.Run(() => XDocument.Parse(xmlContent));
            var coverage = doc.Root;
            if (coverage == null) throw new ArgumentException("Invalid Cobertura XML");

            var history = new ReportHistory
            {
                UploadDate = DateTime.Now,
                FileSizeBytes = System.Text.Encoding.UTF8.GetByteCount(xmlContent),
                ExclusionPattern = ".Controller" // Default
            };

            var packages = coverage.Element("packages")?.Elements("package").ToList() ?? new List<XElement>();
            int totalPackages = packages.Count;
            int processedPackages = 0;

            long totalLines = 0;
            long coveredLines = 0;
            long totalBranches = 0;
            long coveredBranches = 0;

            foreach (var package in packages)
            {
                var packageName = package.Attribute("name")?.Value ?? "Unknown";
                
                // Exclude any package that looks like a test project
                if (packageName.Contains("Test", StringComparison.OrdinalIgnoreCase)) continue;

                var classes = package.Element("classes")?.Elements("class") ?? Enumerable.Empty<XElement>();
                
                foreach (var cls in classes)
                {
                    var clsName = cls.Attribute("name")?.Value ?? "Unknown";
                    
                    // Robust line counting from <line> nodes if attributes are missing
                    var linesNode = cls.Element("lines")?.Elements("line") ?? Enumerable.Empty<XElement>();
                    long clsLines = long.TryParse(cls.Attribute("lines-valid")?.Value, out var clv) ? clv : linesNode.Count();
                    long clsCoveredLines = long.TryParse(cls.Attribute("lines-covered")?.Value, out var clc) ? clc : 
                        linesNode.Count(ln => (ln.Attribute("hits")?.Value ?? "0") != "0");

                    long clsBranches = long.TryParse(cls.Attribute("branches-valid")?.Value, out var cbv) ? cbv : 0;
                    long clsCoveredBranches = long.TryParse(cls.Attribute("branches-covered")?.Value, out var cbc) ? cbc : 0;
                    double clsLineRate = double.TryParse(cls.Attribute("line-rate")?.Value, out var cllr) ? cllr : 
                        (clsLines > 0 ? (double)clsCoveredLines / clsLines : 0);

                    totalLines += clsLines;
                    coveredLines += clsCoveredLines;
                    totalBranches += clsBranches;
                    coveredBranches += clsCoveredBranches;

                    var detail = new ClassDetail
                    {
                        PackageName = packageName,
                        ClassName = clsName,
                        LineRate = clsLineRate,
                        BranchRate = double.TryParse(cls.Attribute("branch-rate")?.Value, out var clbr) ? clbr : 0,
                        Complexity = double.TryParse(cls.Attribute("complexity")?.Value, out var clc2) ? clc2 : 0,
                        LinesTotal = clsLines,
                        LinesCovered = clsCoveredLines
                    };

                    // In the initial parse, we add everything to ClassDetails.
                    // The UI will categorize them on the fly based on the pattern.
                    history.ClassDetails.Add(detail);
                }

                processedPackages++;
                if (progress != null)
                {
                    progress.Report((double)processedPackages / totalPackages);
                    // Yield to keep UI responsive
                    await Task.Yield();
                }
            }

            history.LinesTotal = totalLines;
            history.LinesCovered = coveredLines;
            history.LineRate = totalLines > 0 ? (double)coveredLines / totalLines : 0;
            history.BranchRate = totalBranches > 0 ? (double)coveredBranches / totalBranches : 0;

            // Note: Core/Controller rates will be calculated by UI dynamically.
            // But we initialize them here with the default pattern logic to show initial data.
            RecalculateMetrics(history);

            sw.Stop();
            history.ParseDurationMs = sw.ElapsedMilliseconds;

            return history;
        }

        public void RecalculateMetrics(ReportHistory history)
        {
            var pattern = history.ExclusionPattern ?? ".Controller";
            
            var controllers = history.ClassDetails.Where(d => 
                d.PackageName.Contains(pattern, StringComparison.OrdinalIgnoreCase) || 
                d.ClassName.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
            
            var core = history.ClassDetails.Except(controllers).ToList();

            history.ControllerLinesTotal = controllers.Sum(c => c.LinesTotal);
            history.ControllerLinesCovered = controllers.Sum(c => c.LinesCovered);
            history.ControllerLineRate = history.ControllerLinesTotal > 0 ? (double)history.ControllerLinesCovered / history.ControllerLinesTotal : 0;

            history.CoreLinesTotal = core.Sum(c => c.LinesTotal);
            history.CoreLinesCovered = core.Sum(c => c.LinesCovered);
            history.CoreLineRate = history.CoreLinesTotal > 0 ? (double)history.CoreLinesCovered / history.CoreLinesTotal : 0;
            
            // For simplicity, we can also update the ControllerDetails list if the UI still expects it
            history.ControllerDetails = controllers;
        }
    }
}
