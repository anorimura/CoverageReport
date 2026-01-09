using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using CoverageReport.Domain.Models;

namespace CoverageReport.Infrastructure.Parsers
{
    public class CoberturaXmlParser
    {
        public async Task<CoverageReportAggregate> ParseAsync(string xmlContent, string exclusionPattern = ".Controller", IProgress<double>? progress = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            progress?.Report(0.05); // Initial doc parse
            var doc = await Task.Run(() => XDocument.Parse(xmlContent));
            var root = doc.Root;
            if (root == null) throw new ArgumentException("Invalid Cobertura XML");

            var report = new CoverageReportAggregate(DateTime.Now, System.Text.Encoding.UTF8.GetByteCount(xmlContent), exclusionPattern);

            var packagesNodes = root.Element("packages")?.Elements("package").ToList() ?? new List<XElement>();
            int totalClasses = packagesNodes.Sum(p => p.Element("classes")?.Elements("class").Count() ?? 0);
            int processedClasses = 0;

            foreach (var packageNode in packagesNodes)
            {
                var packageName = packageNode.Attribute("name")?.Value ?? "Unknown";
                if (packageName.Contains("Test", StringComparison.OrdinalIgnoreCase))
                {
                    processedClasses += packageNode.Element("classes")?.Elements("class").Count() ?? 0;
                    continue;
                }

                var package = new PackageSummary(packageName);
                var classesNodes = packageNode.Element("classes")?.Elements("class") ?? Enumerable.Empty<XElement>();

                foreach (var classNode in classesNodes)
                {
                    var className = classNode.Attribute("name")?.Value ?? "Unknown";
                    
                    var linesNode = classNode.Element("lines")?.Elements("line") ?? Enumerable.Empty<XElement>();
                    long lineTotal = long.TryParse(classNode.Attribute("lines-valid")?.Value, out var lv) ? lv : linesNode.Count();
                    long lineCovered = long.TryParse(classNode.Attribute("lines-covered")?.Value, out var lc) ? lc : 
                        linesNode.Count(ln => (ln.Attribute("hits")?.Value ?? "0") != "0");

                    long branchTotal = long.TryParse(classNode.Attribute("branches-valid")?.Value, out var bv) ? bv : 0;
                    long branchCovered = long.TryParse(classNode.Attribute("branches-covered")?.Value, out var bc) ? bc : 0;
                    
                    double complexity = double.TryParse(classNode.Attribute("complexity")?.Value, out var cplx) ? cplx : 0;

                    var classDetail = new ClassDetail(
                        className.Split('.').Last(), 
                        className,
                        new CoverageMetrics(lineCovered, lineTotal),
                        new CoverageMetrics(branchCovered, branchTotal),
                        complexity
                    );

                    package.AddClass(classDetail);
                    
                    processedClasses++;
                    if (totalClasses > 0)
                    {
                        // Map 5% to 95% range for class processing
                        progress?.Report(0.05 + (0.90 * processedClasses / totalClasses));
                    }
                }

                if (package.Classes.Any())
                {
                    report.AddPackage(package);
                }
            }

            sw.Stop();
            report.ParseDurationMs = sw.ElapsedMilliseconds;
            progress?.Report(1.0);

            return report;
        }
    }
}
