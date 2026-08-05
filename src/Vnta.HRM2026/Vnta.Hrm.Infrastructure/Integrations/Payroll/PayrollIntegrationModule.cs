using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Vnta.Hrm.Application.KhauTru.KhauTruTamUng;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
using Vnta.Hrm.Application.KhauTru.KhauTruKhac;
using Vnta.Hrm.Application.KhauTru.GiamTruGiaCanh;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Application.TinhLuong.BangCongTongHop;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemGanNhanVien;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;
using Vnta.Hrm.Infrastructure.KhauTru.GiamTruGiaCanh;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTamUng;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruThueTNCN.DependencyInjection;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruKhac;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom.DependencyInjection;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.DependencyInjection;
using Vnta.Hrm.Infrastructure.TinhLuong.BangCongTongHop;
using Vnta.Hrm.Infrastructure.TinhLuong.LuongCanBan;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan.DependencyInjection;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac.DependencyInjection;
using Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop.DependencyInjection;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem.DependencyInjection;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop.DependencyInjection;
namespace Vnta.Hrm.Infrastructure.Integrations.Payroll;

public static class PayrollIntegrationModule
{
    /// <summary>
    /// Đăng ký persistence implementation theo scoped lifetime vì mỗi request/circuit cần cùng DbContext scope.
    /// </summary>
    public static IServiceCollection AddPayrollIntegration(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddScoped<IBasicSalaryService, DatabaseBasicSalaryService>();
        services.AddScoped<IBasicSalaryWorkdaySource, DatabaseBasicSalaryWorkdaySource>();
        services.AddScoped<IPayrollMonthlyWorkInputRefreshService, DatabasePayrollMonthlyWorkInputRefreshService>();
        services.AddPhuCapChuyenCan();
        // Một scope dùng chung DbContext cho toàn bộ thao tác đọc/ghi snapshot tổng hợp phụ cấp.
        services.AddPhuCapTongHop();
        services.AddKhauTruTongHop();
        services.AddKhauTruPhiCongDoan();
        services.AddScoped<IPayrollAdvanceDeductionReadService, DatabasePayrollAdvanceDeductionReadService>();
        services.AddKhauTruThueTNCN();
        services.AddScoped<IEmployeeTaxDependentService, DatabaseEmployeeTaxDependentService>();
        services.AddKhauTruBHXHYT();
        services.AddScoped<IPayrollEmployeeOtherDeductionAllowanceService, DatabasePayrollEmployeeOtherDeductionAllowanceService>();
        services.AddPhuCapTrachNhiemKhac();
        services.AddOtherAllowance();
        // Một concrete instance trong scope được expose qua ba contract để read/command dùng cùng DbContext.
        services.AddPhuCapDocHai();
        services.AddPhuCapCom();
        services.AddPhuCapThamNien();
        services.AddPhuCapTrachNhiem();
        services.AddScoped<IPhuCapTrachNhiemGanNhanVienXemService, PhuCapTrachNhiemGanNhanVienXemService>();
        services.AddScoped<IPhuCapTrachNhiemGanNhanVienDongBoService, DatabasePhuCapTrachNhiemGanNhanVienDongBoService>();
        services.AddScoped<IPhuCapTrachNhiemGanNhanVienQueryService, DatabasePhuCapTrachNhiemGanNhanVienQueryService>();
        return services;
    }
}
