namespace Vnta.Hrm.Web.Client.Models;

public static class AttendanceDeviceInfoLabelMapper
{
    private static readonly IReadOnlyDictionary<string, string> Labels =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IPADDRESS"] = "Địa chỉ IP",
            ["IP"] = "Địa chỉ IP",
            ["DEVICEIP"] = "Địa chỉ IP",
            ["MACADDRESS"] = "Địa chỉ MAC",
            ["MAC"] = "Địa chỉ MAC",
            ["OEMVENDOR"] = "Nhà sản xuất",
            ["VENDORNAME"] = "Nhà sản xuất",
            ["VENDOR"] = "Nhà sản xuất",
            ["MANUFACTURER"] = "Nhà sản xuất",
            ["NAME"] = "Model thiết bị",
            ["DEVICENAME"] = "Model thiết bị",
            ["DEVFIRMWAREVERSION"] = "Phiên bản firmware",
            ["FIRMWAREVERSION"] = "Phiên bản firmware",
            ["FWVERSION"] = "Phiên bản firmware",
            ["FIRMVERSION"] = "Phiên bản firmware",
            ["DEVFPVERSION"] = "Phiên bản vân tay",
            ["FINGERPRINTVERSION"] = "Phiên bản vân tay",
            ["FPVERSION"] = "Phiên bản vân tay",
            ["TIMEZONE"] = "Múi giờ",
            ["ATTLOGSTAMP"] = "Mốc log chấm công",
            ["ATTPHOTOSTAMP"] = "Mốc ảnh chấm công",
            ["PHOTOSTAMP"] = "Mốc ảnh chấm công",
            ["OPLOGSTAMP"] = "Mốc log vận hành",
            ["OPERLOGSTAMP"] = "Mốc log vận hành",
            ["OPERATIONLOGSTAMP"] = "Mốc log vận hành",
            ["TRANSFLAG"] = "Cờ truyền dữ liệu",
            ["TRANSFERFLAG"] = "Cờ truyền dữ liệu",
            ["DELAY"] = "Độ trễ đồng bộ",
            ["REALTIME"] = "Đồng bộ thời gian thực",
            ["TRANSINTERVAL"] = "Chu kỳ truyền",
            ["TRANSTIMES"] = "Thời điểm truyền",
            ["ENCRYPT"] = "Mã hóa",
            ["ERRORDELAY"] = "Độ trễ khi lỗi",
            ["IRTEMPDETECTIONFUNON"] = "Đo nhiệt độ hồng ngoại",
            ["MASKDETECTIONFUNON"] = "Nhận diện khẩu trang",
            ["MULTIBIODATASUPPORT"] = "Hỗ trợ đa sinh trắc",
            ["USERCOUNT"] = "Số người dùng",
            ["USERCNT"] = "Số người dùng",
            ["TRANSACTIONCOUNT"] = "Số log chấm công",
            ["ATTLOGCOUNT"] = "Số log chấm công",
            ["LOGCOUNT"] = "Số log chấm công",
            ["FPCOUNT"] = "Số mẫu vân tay",
            ["FINGERPRINTCOUNT"] = "Số mẫu vân tay",
            ["TIMEOUT"] = "Thời gian chờ",
            ["SYNCTIME"] = "Đồng bộ giờ",
            ["PORT"] = "Cổng kết nối"
        };

    public static string GetLabel(string normalizedKey, string fallbackKey)
    {
        return Labels.TryGetValue(normalizedKey, out var label)
            ? label
            : fallbackKey.Trim();
    }
}
