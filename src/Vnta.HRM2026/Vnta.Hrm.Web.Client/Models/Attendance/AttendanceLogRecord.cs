using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Models {
    public class AttendanceLogRecord {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public Guid? EmployeeId { get; set; }

        [StringLength(50)]
        public string? DeviceCode { get; set; }

        [StringLength(50)]
        public string? EmployeeCode { get; set; }

        [StringLength(200)]
        public string? EmployeeName { get; set; }

        public DateTime? AttTime { get; set; }

        [StringLength(10)]
        public string? Status { get; set; }

        [StringLength(10)]
        public string? Verify { get; set; }

        [StringLength(50)]
        public string? WorkCode { get; set; }

        [StringLength(50)]
        public string? Reserved1 { get; set; }

        [StringLength(50)]
        public string? Reserved2 { get; set; }

        public int? MaskFlag { get; set; }

        [StringLength(50)]
        public string? Temperature { get; set; }

        [Required]
        [StringLength(200)]
        public string DedupKey { get; set; } = string.Empty;

        public DateTime UpdateTime { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public bool IsMatchedEmployee => EmployeeId.HasValue;

        public string AttendanceStateText => Status switch {
            "0" => "Chấm vào",
            "1" => "Chấm ra",
            "4" => "Tăng ca vào",
            "5" => "Tăng ca ra",
            _ => string.IsNullOrWhiteSpace(Status) ? "Không xác định" : $"Mã {Status}"
        };

        public string VerifyModeText => Verify switch {
            "1" => "Vân tay",
            "3" => "Thẻ",
            "15" => "Khuôn mặt",
            "0" => "Mật khẩu",
            _ => string.IsNullOrWhiteSpace(Verify) ? "Không xác định" : $"Mã {Verify}"
        };

        public AttendanceLogRecord Clone() =>
            new() {
                Id = Id,
                DeviceId = DeviceId,
                EmployeeId = EmployeeId,
                DeviceCode = DeviceCode,
                EmployeeCode = EmployeeCode,
                EmployeeName = EmployeeName,
                AttTime = AttTime,
                Status = Status,
                Verify = Verify,
                WorkCode = WorkCode,
                Reserved1 = Reserved1,
                Reserved2 = Reserved2,
                MaskFlag = MaskFlag,
                Temperature = Temperature,
                DedupKey = DedupKey,
                UpdateTime = UpdateTime,
                CreatedAtUtc = CreatedAtUtc,
                UpdatedAtUtc = UpdatedAtUtc
            };
    }
}
