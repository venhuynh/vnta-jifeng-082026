using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Models {
    public class AttendanceDailySummaryRecord {
        public Guid Id { get; set; }
        public Guid? EmployeeId { get; set; }

        [StringLength(50)]
        public string? EmployeeCode { get; set; }

        [StringLength(200)]
        public string? EmployeeName { get; set; }

        [StringLength(200)]
        public string? DepartmentName { get; set; }

        [StringLength(200)]
        public string? PositionName { get; set; }

        public DateOnly WorkDate { get; set; }
        public int PunchCount { get; set; }

        [Required]
        public string PunchMomentsText { get; set; } = string.Empty;

        public DateTime? FirstPunchTime { get; set; }
        public DateTime? LastPunchTime { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public bool IsMatchedEmployee => EmployeeId.HasValue;

        public string EmployeeDisplay
        {
            get
            {
                var parts = new[] { EmployeeCode, EmployeeName }
                    .Where(static part => !string.IsNullOrWhiteSpace(part))
                    .Select(static part => part!.Trim())
                    .ToArray();

                return parts.Length == 0 ? "--" : string.Join(" - ", parts);
            }
        }

        public IReadOnlyList<string> PunchMoments => string.IsNullOrWhiteSpace(PunchMomentsText)
            ? Array.Empty<string>()
            : PunchMomentsText
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();

        public AttendanceDailySummaryRecord Clone() =>
            new() {
                Id = Id,
                EmployeeId = EmployeeId,
                EmployeeCode = EmployeeCode,
                EmployeeName = EmployeeName,
                DepartmentName = DepartmentName,
                PositionName = PositionName,
                WorkDate = WorkDate,
                PunchCount = PunchCount,
                PunchMomentsText = PunchMomentsText,
                FirstPunchTime = FirstPunchTime,
                LastPunchTime = LastPunchTime,
                CreatedAtUtc = CreatedAtUtc,
                UpdatedAtUtc = UpdatedAtUtc
            };
    }
}
