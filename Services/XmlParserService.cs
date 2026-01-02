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
                FileSizeBytes = System.Text.Encoding.UTF8.GetByteCount(xmlContent)
            };

            var packages = coverage.Element("packages")?.Elements("package").ToList() ?? new List<XElement>();
            int totalPackages = packages.Count;
            int processedPackages = 0;

            long totalLines = 0;
            long coveredLines = 0;
            long totalBranches = 0;
            long coveredBranches = 0;

            long coreTotalLines = 0;
            long coreCoveredLines = 0;
            long coreTotalBranches = 0;
            long coreCoveredBranches = 0;

            long controllerTotalLines = 0;
            long controllerCoveredLines = 0;
            long controllerTotalBranches = 0;
            long controllerCoveredBranches = 0;

            foreach (var package in packages)
            {
                var packageName = package.Attribute("name")?.Value ?? "Unknown";
                
                // Exclude any package that looks like a test project
                if (packageName.Contains("Test", StringComparison.OrdinalIgnoreCase)) continue;

                var classes = package.Element("classes")?.Elements("class") ?? Enumerable.Empty<XElement>();
                
                // Track sub-totals per package to handle mixed packages
                long pkgLogicLines = 0;
                long pkgLogicCovered = 0;
                long pkgControllerLines = 0;
                long pkgControllerCovered = 0;

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

                    // Check if this class is a controller
                    bool isController = packageName.Contains("WebAPI.Controller", StringComparison.OrdinalIgnoreCase) || 
                                       clsName.Contains(".Controller", StringComparison.OrdinalIgnoreCase) ||
                                       clsName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);

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

                    if (isController)
                    {
                        pkgControllerLines += clsLines;
                        pkgControllerCovered += clsCoveredLines;
                        controllerTotalLines += clsLines;
                        controllerCoveredLines += clsCoveredLines;
                        controllerTotalBranches += clsBranches;
                        controllerCoveredBranches += clsCoveredBranches;
                        history.ControllerDetails.Add(detail);
                    }
                    else
                    {
                        pkgLogicLines += clsLines;
                        pkgLogicCovered += clsCoveredLines;
                        coreTotalLines += clsLines;
                        coreCoveredLines += clsCoveredLines;
                        coreTotalBranches += clsBranches;
                        coreCoveredBranches += clsCoveredBranches;
                        
                        // Include all WebAPI logic for dynamic filtering in UI
                        if (packageName.Contains("WebAPI", StringComparison.OrdinalIgnoreCase))
                        {
                            history.ClassDetails.Add(detail);
                        }
                    }
                }

                // Add to records (summaries table side)
                if (pkgLogicLines > 0)
                {
                    history.Records.Add(new PackageSummary {
                        PackageName = packageName + " (Logic)",
                        ClassCount = classes.Count(c => !((c.Attribute("name")?.Value ?? "").Contains(".Controller") || (c.Attribute("name")?.Value ?? "").EndsWith("Controller"))),
                        LinesTotal = pkgLogicLines,
                        LinesCovered = pkgLogicCovered,
                        LineRate = (double)pkgLogicCovered / pkgLogicLines
                    });
                }
                if (pkgControllerLines > 0)
                {
                    history.Records.Add(new PackageSummary {
                        PackageName = packageName + " (Controllers)",
                        ClassCount = classes.Count(c => (c.Attribute("name")?.Value ?? "").Contains(".Controller") || (c.Attribute("name")?.Value ?? "").EndsWith("Controller")),
                        LinesTotal = pkgControllerLines,
                        LinesCovered = pkgControllerCovered,
                        LineRate = (double)pkgControllerCovered / pkgControllerLines
                    });
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

            history.CoreLinesTotal = coreTotalLines;
            history.CoreLinesCovered = coreCoveredLines;
            history.CoreLineRate = coreTotalLines > 0 ? (double)coreCoveredLines / coreTotalLines : 0;
            history.CoreBranchRate = coreTotalBranches > 0 ? (double)coreCoveredBranches / coreTotalBranches : 0;

            history.ControllerLinesTotal = controllerTotalLines;
            history.ControllerLinesCovered = controllerCoveredLines;
            history.ControllerLineRate = controllerTotalLines > 0 ? (double)controllerCoveredLines / controllerTotalLines : 0;
            history.ControllerBranchRate = controllerTotalBranches > 0 ? (double)controllerCoveredBranches / controllerTotalBranches : 0;

            sw.Stop();
            history.ParseDurationMs = sw.ElapsedMilliseconds;

            return history;
        }
    }
}
