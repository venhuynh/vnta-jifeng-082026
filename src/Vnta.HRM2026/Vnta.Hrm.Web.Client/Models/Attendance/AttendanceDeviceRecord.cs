using System.ComponentModel.DataAnnotations;

namespace Vnta.Hrm.Web.Client.Models {
    public class AttendanceDeviceRecord {
        const string NonWhitespacePattern = @".*\S.*";
        const string OptionalNonWhitespacePattern = @"^$|.*\S.*";
        const string SerialNumberPattern = @"^\s*[A-Za-z0-9]+\s*$";

        public Guid Id { get; set; }

        [StringLength(100, ErrorMessage = "Mã máy không được vượt quá 100 ký tự.")]
        public string? Code { get; set; }

        [Required(ErrorMessage = "Tên máy không được để trống.")]
        [StringLength(250, ErrorMessage = "Tên máy không được vượt quá 250 ký tự.")]
        [RegularExpression(NonWhitespacePattern, ErrorMessage = "Tên máy không được chỉ gồm khoảng trắng.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Số serial không được để trống.")]
        [StringLength(50, ErrorMessage = "Số serial không được vượt quá 50 ký tự.")]
        [RegularExpression(SerialNumberPattern, ErrorMessage = "Số serial chỉ được gồm chữ, số và các ký tự ., _, -.")]
        public string? SerialNumber { get; set; }

        [StringLength(50, ErrorMessage = "IP không được vượt quá 50 ký tự.")]
        public string? IpAddress { get; set; }

        [StringLength(50, ErrorMessage = "MAC Address không được vượt quá 50 ký tự.")]
        public string? MacAddress { get; set; }

        public int? Port { get; set; }

        [StringLength(500, ErrorMessage = "Vị trí không được vượt quá 500 ký tự.")]
        [RegularExpression(OptionalNonWhitespacePattern, ErrorMessage = "Vị trí không được chỉ gồm khoảng trắng.")]
        public string? Location { get; set; }

        [Required(ErrorMessage = "Mã kích hoạt không được để trống.")]
        [StringLength(200, ErrorMessage = "Mã kích hoạt không được vượt quá 200 ký tự.")]
        [RegularExpression(AttendanceGatewayActivationCode.OptionalActivationCodePattern, ErrorMessage = "Mã kích hoạt phải đúng dạng VN1-XXXX-XXXX-XXXX-XXXX.")]
        public string? ActivationCode { get; set; }

        [StringLength(100, ErrorMessage = "Tên hãng không được vượt quá 100 ký tự.")]
        public string? VendorName { get; set; }

        [StringLength(200, ErrorMessage = "Model không được vượt quá 200 ký tự.")]
        public string? DeviceModel { get; set; }

        [StringLength(100, ErrorMessage = "Firmware không được vượt quá 100 ký tự.")]
        public string? FirmwareVersion { get; set; }

        [StringLength(100, ErrorMessage = "Phiên bản vân tay không được vượt quá 100 ký tự.")]
        public string? FingerprintVersion { get; set; }

        [StringLength(50, ErrorMessage = "Múi giờ không được vượt quá 50 ký tự.")]
        public string? TimeZone { get; set; }

        public int Status { get; set; }
        public bool IsInUse { get; set; }
        public int? UserCount { get; set; }
        public int? AttendanceLogCount { get; set; }
        public int? FingerprintCount { get; set; }

        [StringLength(100, ErrorMessage = "AttendanceLogStamp không được vượt quá 100 ký tự.")]
        public string? AttendanceLogStamp { get; set; }

        [StringLength(100, ErrorMessage = "AttendancePhotoStamp không được vượt quá 100 ký tự.")]
        public string? AttendancePhotoStamp { get; set; }

        [StringLength(100, ErrorMessage = "OperationLogStamp không được vượt quá 100 ký tự.")]
        public string? OperationLogStamp { get; set; }

        [StringLength(100, ErrorMessage = "ErrorLogStamp không được vượt quá 100 ký tự.")]
        public string? ErrorLogStamp { get; set; }

        [StringLength(1000, ErrorMessage = "TransferFlag không được vượt quá 1000 ký tự.")]
        public string? TransferFlag { get; set; }

        [StringLength(100, ErrorMessage = "Delay không được vượt quá 100 ký tự.")]
        public string? Delay { get; set; }

        [StringLength(20, ErrorMessage = "Realtime không được vượt quá 20 ký tự.")]
        public string? Realtime { get; set; }

        [StringLength(100, ErrorMessage = "TransInterval không được vượt quá 100 ký tự.")]
        public string? TransInterval { get; set; }

        [StringLength(100, ErrorMessage = "TransTimes không được vượt quá 100 ký tự.")]
        public string? TransTimes { get; set; }

        [StringLength(20, ErrorMessage = "Encrypt không được vượt quá 20 ký tự.")]
        public string? Encrypt { get; set; }

        [StringLength(100, ErrorMessage = "ErrorDelay không được vượt quá 100 ký tự.")]
        public string? ErrorDelay { get; set; }

        public int? Timeout { get; set; }
        public int SyncTime { get; set; }
        public DateTime? LastRequestTime { get; set; }

        [StringLength(20, ErrorMessage = "IrTempDetectionFunOn không được vượt quá 20 ký tự.")]
        public string? IrTempDetectionFunOn { get; set; }

        [StringLength(20, ErrorMessage = "MaskDetectionFunOn không được vượt quá 20 ký tự.")]
        public string? MaskDetectionFunOn { get; set; }

        [StringLength(200, ErrorMessage = "MultiBioDataSupport không được vượt quá 200 ký tự.")]
        public string? MultiBioDataSupport { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public string OperationalStatus =>
            string.IsNullOrWhiteSpace(SerialNumber) ? "Chưa đăng ký serial" :
            !AttendanceGatewayActivationCode.Validate(SerialNumber, ActivationCode ?? string.Empty) ? "Chưa kích hoạt" :
            LastRequestTime.HasValue ? "Đang hoạt động" :
            "Chưa ghi nhận kết nối";

        public AttendanceDeviceRecord Clone() =>
            new() {
                Id = Id,
                Code = Code,
                Name = Name,
                SerialNumber = SerialNumber,
                IpAddress = IpAddress,
                MacAddress = MacAddress,
                Port = Port,
                Location = Location,
                ActivationCode = ActivationCode,
                VendorName = VendorName,
                DeviceModel = DeviceModel,
                FirmwareVersion = FirmwareVersion,
                FingerprintVersion = FingerprintVersion,
                TimeZone = TimeZone,
                Status = Status,
                IsInUse = IsInUse,
                UserCount = UserCount,
                AttendanceLogCount = AttendanceLogCount,
                FingerprintCount = FingerprintCount,
                AttendanceLogStamp = AttendanceLogStamp,
                AttendancePhotoStamp = AttendancePhotoStamp,
                OperationLogStamp = OperationLogStamp,
                ErrorLogStamp = ErrorLogStamp,
                TransferFlag = TransferFlag,
                Delay = Delay,
                Realtime = Realtime,
                TransInterval = TransInterval,
                TransTimes = TransTimes,
                Encrypt = Encrypt,
                ErrorDelay = ErrorDelay,
                Timeout = Timeout,
                SyncTime = SyncTime,
                LastRequestTime = LastRequestTime,
                IrTempDetectionFunOn = IrTempDetectionFunOn,
                MaskDetectionFunOn = MaskDetectionFunOn,
                MultiBioDataSupport = MultiBioDataSupport,
                CreatedAtUtc = CreatedAtUtc,
                UpdatedAtUtc = UpdatedAtUtc
            };
    }
}
