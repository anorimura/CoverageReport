using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoverageReport.Models
{
    public class ReportHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime UploadDate { get; set; }

        // Overall summary metrics
        public double LineRate { get; set; }
        public double BranchRate { get; set; }

        public double CoreLineRate { get; set; }
        public double CoreBranchRate { get; set; }

        public double ControllerLineRate { get; set; }
        public double ControllerBranchRate { get; set; }
        public double TargetCoreLineRate { get; set; } = 0.6; // Default 60%
        public string ExclusionPattern { get; set; } = ".Controller"; // Default pattern

        // Performance metrics for progress estimation
        public long ParseDurationMs { get; set; }
        public long FileSizeBytes { get; set; }

        public long LinesCovered { get; set; }
        public long LinesTotal { get; set; }

        public long CoreLinesCovered { get; set; }
        public long CoreLinesTotal { get; set; }

        public long ControllerLinesCovered { get; set; }
        public long ControllerLinesTotal { get; set; }
        
        // Navigation property
        // Navigation property
        public List<PackageSummary> Records { get; set; } = new List<PackageSummary>();
        public List<ClassDetail> ClassDetails { get; set; } = new List<ClassDetail>();
        public List<ClassDetail> ControllerDetails { get; set; } = new List<ClassDetail>();
    }

    public class PackageSummary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ReportHistoryId { get; set; }
        [ForeignKey("ReportHistoryId")]
        public ReportHistory ReportHistory { get; set; } = null!;

        public string PackageName { get; set; } = string.Empty;
        
        public double LineRate { get; set; }
        public double BranchRate { get; set; }
        public double Complexity { get; set; }
        public int ClassCount { get; set; }

        public long LinesCovered { get; set; }
        public long LinesTotal { get; set; }
    }

    public class ClassDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ReportHistoryId { get; set; }
        [ForeignKey("ReportHistoryId")]
        public ReportHistory ReportHistory { get; set; } = null!;

        public string PackageName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;

        public double LineRate { get; set; }
        public double BranchRate { get; set; }
        public double Complexity { get; set; }

        public long LinesCovered { get; set; }
        public long LinesTotal { get; set; }
        public bool IsTarget { get; set; }
    }
}
