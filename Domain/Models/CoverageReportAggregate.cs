using System;
using System.Collections.Generic;
using System.Linq;

namespace CoverageReport.Domain.Models
{
    public class CoverageReportAggregate
    {
        public int Id { get; set; }
        public DateTime UploadDate { get; set; }
        public List<PackageSummary> Packages { get; private set; } = new();
        public string ExclusionPattern { get; private set; }
        public long FileSizeBytes { get; init; }
        public long ParseDurationMs { get; set; }

        public CoverageReportAggregate(DateTime uploadDate, long fileSizeBytes, string exclusionPattern = ".Controller")
        {
            UploadDate = uploadDate;
            FileSizeBytes = fileSizeBytes;
            ExclusionPattern = exclusionPattern;
        }

        public void AddPackage(PackageSummary package) => Packages.Add(package);

        public void UpdateExclusionPattern(string pattern)
        {
            ExclusionPattern = pattern;
        }

        public IEnumerable<ClassDetail> AllClasses => Packages.SelectMany(p => p.Classes);

        public CoverageMetrics? ForcedTotalMetrics { get; set; }
        public CoverageMetrics? ForcedCoreMetrics { get; set; }
        public CoverageMetrics? ForcedControllerMetrics { get; set; }

        public CoverageMetrics GetTotalMetrics(Func<ClassDetail, bool>? filter = null)
        {
            if (filter == null && ForcedTotalMetrics != null && !Packages.Any()) return ForcedTotalMetrics;
            var classes = filter == null ? AllClasses : AllClasses.Where(filter);
            return classes.Aggregate(CoverageMetrics.Empty, (acc, c) => acc + c.LineCoverage);
        }

        public CoverageMetrics CoreMetrics => (!Packages.Any() && ForcedCoreMetrics != null) ? ForcedCoreMetrics : GetTotalMetrics(c => !IsExcluded(c));
        public CoverageMetrics ControllerMetrics => (!Packages.Any() && ForcedControllerMetrics != null) ? ForcedControllerMetrics : GetTotalMetrics(c => IsExcluded(c));

        private bool IsExcluded(ClassDetail cls)
        {
            if (string.IsNullOrEmpty(ExclusionPattern)) return false;
            return cls.FullName.Contains(ExclusionPattern, StringComparison.OrdinalIgnoreCase);
        }
    }
}
