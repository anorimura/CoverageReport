namespace CoverageReport.Domain.Models
{
    public class ClassDetail
    {
        public string Name { get; init; }
        public string FullName { get; init; }
        public CoverageMetrics LineCoverage { get; private set; }
        public CoverageMetrics BranchCoverage { get; private set; }
        public double Complexity { get; init; }

        public ClassDetail(string name, string fullName, CoverageMetrics lineCoverage, CoverageMetrics branchCoverage, double complexity)
        {
            Name = name;
            FullName = fullName;
            LineCoverage = lineCoverage;
            BranchCoverage = branchCoverage;
            Complexity = complexity;
        }

        public bool IsTarget { get; set; }

        public string Namespace => FullName.Contains(".") 
            ? FullName.Substring(0, FullName.LastIndexOf('.')) 
            : string.Empty;
    }
}
