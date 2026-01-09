using System.Collections.Generic;
using System.Linq;

namespace CoverageReport.Domain.Models
{
    public class PackageSummary
    {
        public string Name { get; init; }
        public List<ClassDetail> Classes { get; private set; } = new();

        public PackageSummary(string name)
        {
            Name = name;
        }

        public void AddClass(ClassDetail classDetail) => Classes.Add(classDetail);

        public CoverageMetrics TotalLineCoverage => 
            Classes.Aggregate(CoverageMetrics.Empty, (acc, c) => acc + c.LineCoverage);

        public CoverageMetrics TotalBranchCoverage => 
            Classes.Aggregate(CoverageMetrics.Empty, (acc, c) => acc + c.BranchCoverage);
    }
}
