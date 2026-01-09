namespace CoverageReport.Domain.Models
{
    public record CoverageMetrics(long Covered, long Total)
    {
        public double Rate => Total > 0 ? (double)Covered / Total : 0;
        
        public static CoverageMetrics Empty => new CoverageMetrics(0, 0);
        
        public static CoverageMetrics operator +(CoverageMetrics a, CoverageMetrics b)
            => new CoverageMetrics(a.Covered + b.Covered, a.Total + b.Total);
    }
}
