using Azure;
using Azure.AI.OpenAI;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Application.KhauTru.GiamTruGiaCanh;
using Vnta.Hrm.Application.ChamCong.DashboardBangChamCong;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Web.Client.Services.Adms;
using Vnta.Hrm.Web.Client.Services.Api;
using Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapKhac;
using Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapTrachNhiemGanNhanVien;
using Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.Api.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Client.Services;
using Vnta.Hrm.Web.Client.Services.Ui;
using Vnta.Hrm.Web.Client.Services.DataProviders;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapThamNien;
using Vnta.Hrm.Web.Client.Services.DataProviders.NhanSu.ChiTietNhanVien;
using Vnta.Hrm.Web.Client.Services.DataProviders.NhanSu.NhanVien;
using Vnta.Hrm.Web.Client.Services.Api.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Web.Client.Services.DataProviders.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapDashboard;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapChuyenCan;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemGanNhanVien;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemKhac;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapCom;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapKhac;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Export;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.State;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;
using Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapCom;
using Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapPhepLe;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapPhepLe;
using DevExpress.AIIntegration;
using DevExpress.AIIntegration.Chat;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapChuyenCan;
using Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapTrachNhiemKhac;
using Microsoft.Extensions.AI;
using Vnta.Hrm.Web.Client.Services.Api.TinhLuong;
using Vnta.Hrm.Web.Client.Services.Api.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Web.Client.Services.Api.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Web.Client.Services.DataProviders.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;

namespace Vnta.Hrm.Web.Client.Utils {
    public static class ServiceExtensions {
        public static void AddAppServices(this IServiceCollection services) {
            services.AddSingleton(TimeProvider.System);
            services.AddScoped(sp =>
                new HttpClient {
                    BaseAddress = new Uri("https://js.devexpress.com/Demos/RwaService/api/")
                });
            services.Configure<AdmsGatewayMonitorOptions>(_ => { });
            services.AddDevExpressBlazor();
            services.AddScoped<SearchManager>();
            services.AddScoped<ModuleLoader>();
            services.AddScoped<ThemeManager>();
            services.AddScoped<ClipboardManager>();
            services.AddScoped<SizeModeManager>();
            services.AddScoped<HrmBusyStateService>();
            services.AddScoped<IHrmToastService, HrmToastService>();
            services.AddScoped<IHrmDialogService, HrmDialogService>();
            services.AddScoped<HrmOperationExecutor>();
            services.AddScoped<IEmployeeApiService, HttpEmployeeApiService>();
            services.AddScoped<EmployeeDataProvider>();
            services.AddScoped<NhanVienDataProvider>();
            // Adapter compatibility cho các màn assignment/grade đang rollout riêng.
            services.AddScoped<PayrollResponsibilityAllowanceDataProvider>();
            services.AddScoped<PhuCapTrachNhiemAbcQueryDataProvider>();
            services.AddScoped<PhuCapTrachNhiemAbcCommandDataProvider>();
            services.AddScoped<PhuCapTrachNhiemConfigurationDataProvider>();
            services.AddScoped<IPhuCapTrachNhiemQueryFactory, PhuCapTrachNhiemQueryFactory>();
            services.AddScoped<PhuCapTrachNhiemGanNhanVienDataProvider>();
            // Interactive Server uses application services directly. AddBrowserApiServices
            // overrides this registration with the HTTP gateway for WebAssembly.
            services.AddScoped<IPhuCapTrachNhiemGanNhanVienGateway, PhuCapTrachNhiemGanNhanVienGatewayAdapter>();
            services.AddScoped<PhuCapTrachNhiemGanNhanVienXemDataProvider>();
            services.AddScoped<ResponsibilityPositionAssignmentDataProvider>();
            services.AddScoped<IChiTietNhanVienApiService, HttpChiTietNhanVienApiService>();
            services.AddScoped<ChiTietNhanVienDataProvider>();
            services.AddScoped<EmployeeAccountDataProvider>();
            services.AddScoped<AttendanceDepartmentDataProvider>();
            services.AddScoped<AttendanceDeviceDataProvider>();
            services.AddScoped<AttendancePositionDataProvider>();
            services.AddScoped<AttendanceShiftDataProvider>();
            services.AddScoped<AttendanceStatusCodeDataProvider>();
            services.AddScoped<AttendanceWorkCalendarDataProvider>();
            services.AddScoped<MonthlyWorkSummaryDataProvider>();
            services.AddScoped<IMonthlyWorkSummaryDataProvider>(sp =>
                sp.GetRequiredService<MonthlyWorkSummaryDataProvider>());
            services.AddScoped<AttendanceTimesheetDashboardDataProvider>();
            services.AddScoped<OvertimeRegistrationDataProvider>();
            services.AddAttendanceAllowanceResultDataProvider();
            services.AddScoped<IAttendanceAllowanceFilterFactory, AttendanceAllowanceFilterFactory>();
            services.AddScoped<PayrollPersonalIncomeTaxDeductionDataProvider>();
            services.AddScoped<PhuCapComDataProvider>();
            services.AddScoped<IPhuCapComDataProvider>(sp =>
                sp.GetRequiredService<PhuCapComDataProvider>());
            services.AddScoped<IPhuCapComFilterFactory, PhuCapComFilterFactory>();
            services.AddScoped<LeaveHolidayAllowanceDataProvider>();
            services.AddScoped<ILeaveHolidayAllowanceDataProvider>(sp =>
                sp.GetRequiredService<LeaveHolidayAllowanceDataProvider>());
            services.AddScoped<IPhuCapPhepLeFilterFactory, PhuCapPhepLeFilterFactory>();
            services.AddScoped<OtherAllowanceDataProvider>();
            services.AddScoped<IOtherAllowanceReadDataProvider>(sp => sp.GetRequiredService<OtherAllowanceDataProvider>());
            services.AddScoped<IOtherAllowanceCreateDataProvider>(sp => sp.GetRequiredService<OtherAllowanceDataProvider>());
            services.AddScoped<IOtherAllowancePreviousMonthSyncDataProvider>(sp => sp.GetRequiredService<OtherAllowanceDataProvider>());
            services.AddScoped<IOtherAllowanceUpdateDataProvider>(sp => sp.GetRequiredService<OtherAllowanceDataProvider>());
            services.AddScoped<IOtherAllowanceLockDataProvider>(sp => sp.GetRequiredService<OtherAllowanceDataProvider>());
            services.AddScoped<IOtherAllowanceMonthlyWorkDataProvider>(sp => sp.GetRequiredService<OtherAllowanceDataProvider>());
            services.AddTransient<IOtherAllowanceScreenController, OtherAllowanceCoordinator>();
            services.AddScoped<OtherResponsibilityAllowanceDataProvider>();
            services.AddScoped<IOtherResponsibilityAllowanceDataProvider>(sp =>
                sp.GetRequiredService<OtherResponsibilityAllowanceDataProvider>());
            services.AddTransient<OtherResponsibilityAllowanceGridExporter>();
            services.AddTransient<OtherResponsibilityAllowanceCoordinator>();
            // Provider giữ model giao diện; implementation HTTP của contract được đăng ký ở AddBrowserApiServices.
            services.AddScoped<PayrollAllowanceSummaryDataProvider>();
            services.AddScoped<IPayrollAllowanceSummaryDataProvider>(sp =>
                sp.GetRequiredService<PayrollAllowanceSummaryDataProvider>());
            services.AddScoped<IPhuCapTongHopFilterFactory, PhuCapTongHopFilterFactory>();
            services.AddScoped<PayrollAllowanceDashboardDataProvider>();
            services.AddScoped<PayrollDeductionSummaryDataProvider>();
            services.AddScoped<PayrollDeductionDashboardDataProvider>();
            services.AddScoped<PayrollUnionFeeDeductionDataProvider>();
            services.AddScoped<PayrollInsuranceDeductionDataProvider>();
            services.AddScoped<EmployeeTaxDependentDataProvider>();
            services.AddScoped<PayrollEmployeeOtherDeductionAllowanceDataProvider>();
            services.AddScoped<PayrollEmployeeSeniorityAllowanceDataProvider>();
            services.AddScoped<IPayrollEmployeeSeniorityAllowanceDataProvider>(sp =>
                sp.GetRequiredService<PayrollEmployeeSeniorityAllowanceDataProvider>());
            services.AddHazardAllowanceDataProvider();
            services.AddScoped<ShiftSchedulingSettingDataProvider>();
            services.AddScoped<BasicSalaryDataProvider>();
            services.AddScoped<ContactDataProvider>();
            services.AddScoped<AnalyticDataProvider>();
            services.AddScoped<TasksDataProvider>();
            services.AddScoped<IAttendanceBiometricDeviceCommandService, AttendanceBiometricDeviceCommandService>();
            services.AddCascadingValue("NotificationCount", sp => 4);
            services.AddScoped(sp => new CascadingValueSource<SizeMode>("ParentSizeMode", SizeMode.Medium, false));
            services.AddCascadingValue(sp => sp.GetRequiredService<CascadingValueSource<SizeMode>>());
            services.AddDevExpressAI();
        }

        public static void AddChatClient(this IServiceCollection services, string aiEndpoint, string aiKey, string deployment) {
            services.AddKeyedScoped<IChatResponseProvider>(ChatResponseProviderServiceKeys.Dashboard, (sp, _) => CreateChatResponseProvider(sp, aiEndpoint, aiKey, deployment));
            services.AddKeyedScoped<IChatResponseProvider>(ChatResponseProviderServiceKeys.Scheduler, (sp, _) => CreateChatResponseProvider(sp, aiEndpoint, aiKey, deployment));
        }

        static IChatResponseProvider CreateChatResponseProvider(IServiceProvider sp, string aiEndpoint, string aiKey, string deployment) {
            var azureClient = new AzureOpenAIClient(new Uri(aiEndpoint), new AzureKeyCredential(aiKey));
            return azureClient
                   .GetChatClient(deployment)
                   .AsIChatClient()
                   .AsBuilder()
                   .UseDXTools()
                   .UseFunctionInvocation()
                   .Build(sp)
                   .AsIChatResponseProvider();
        }

        public static void AddBrowserApiServices(this IServiceCollection services) {
            services.AddScoped<IAttendanceDeviceService, HttpAttendanceDeviceService>();
            services.AddScoped<IAttendanceStatusCodeService, HttpAttendanceStatusCodeService>();
            services.AddScoped<IAttendanceLogReadService, HttpAttendanceLogReadService>();
            services.AddScoped<IAttendanceBiometricDataReadService, HttpAttendanceBiometricDataReadService>();
            services.AddScoped<IAttendanceBiometricDataRefreshService, HttpAttendanceBiometricDataRefreshService>();
            services.AddScoped<IAttendanceBiometricDeviceQueueService, HttpAttendanceBiometricDeviceQueueService>();
            services.AddScoped<IAttendanceDailySummaryReadService, HttpAttendanceDailySummaryReadService>();
            services.AddScoped<IAttendanceDailySummaryService, HttpAttendanceDailySummaryService>();
            services.AddScoped<IAttendanceWorkdaySummaryReadService, HttpAttendanceWorkdaySummaryReadService>();
            services.AddScoped<IAttendanceWorkdaySummaryService, HttpAttendanceWorkdaySummaryService>();
            services.AddScoped<IAttendanceTimesheetDashboardService, HttpAttendanceTimesheetDashboardService>();
            services.AddScoped<IAttendanceWorkCalendarService, HttpAttendanceWorkCalendarService>();
            services.AddScoped<IOvertimeRegistrationService, HttpOvertimeRegistrationService>();
            services.AddAttendanceAllowanceResultApi();
            services.AddScoped<HttpPayrollPersonalIncomeTaxDeductionService>();
            services.AddScoped<IPayrollPersonalIncomeTaxDeductionReadService>(sp =>
                sp.GetRequiredService<HttpPayrollPersonalIncomeTaxDeductionService>());
            services.AddScoped<IPayrollPersonalIncomeTaxDeductionRefreshService>(sp =>
                sp.GetRequiredService<HttpPayrollPersonalIncomeTaxDeductionService>());
            services.AddScoped<IPayrollPersonalIncomeTaxDeductionManualAdjustmentService>(sp =>
                sp.GetRequiredService<HttpPayrollPersonalIncomeTaxDeductionService>());
            services.AddScoped<IPayrollPersonalIncomeTaxDeductionLockService>(sp =>
                sp.GetRequiredService<HttpPayrollPersonalIncomeTaxDeductionService>());
            // Cùng contract application với backend để client không phụ thuộc trực tiếp vào URL endpoint.
            services.AddScoped<HttpPayrollAllowanceSummaryService>();
            services.AddScoped<IPayrollAllowanceSummaryReadService>(sp =>
                sp.GetRequiredService<HttpPayrollAllowanceSummaryService>());
            services.AddScoped<IPayrollAllowanceSummaryExportService>(sp =>
                sp.GetRequiredService<HttpPayrollAllowanceSummaryService>());
            services.AddScoped<IPayrollAllowanceSummaryPreviousMonthSyncService>(sp =>
                sp.GetRequiredService<HttpPayrollAllowanceSummaryService>());
            services.AddScoped<IPayrollAllowanceSummaryRefreshService>(sp =>
                sp.GetRequiredService<HttpPayrollAllowanceSummaryService>());
            services.AddScoped<IPayrollAllowanceSummaryManualAdjustmentService>(sp =>
                sp.GetRequiredService<HttpPayrollAllowanceSummaryService>());
            services.AddScoped<IPayrollAllowanceSummaryLockService>(sp =>
                sp.GetRequiredService<HttpPayrollAllowanceSummaryService>());
            services.AddScoped<IPayrollAllowanceDashboardReadService>(sp =>
                sp.GetRequiredService<HttpPayrollAllowanceSummaryService>());
            services.AddScoped<IPayrollAllowanceDashboardBreakdownQueryService>(sp => sp.GetRequiredService<HttpPayrollAllowanceSummaryService>());
            services.AddScoped<IPayrollAllowanceDashboardTrendQueryService>(sp => sp.GetRequiredService<HttpPayrollAllowanceSummaryService>());
            services.AddScoped<IPayrollAllowanceDashboardMonthlyComparisonQueryService>(sp => sp.GetRequiredService<HttpPayrollAllowanceSummaryService>());
            services.AddScoped<IPayrollAllowanceDashboardDepartmentComparisonQueryService>(sp => sp.GetRequiredService<HttpPayrollAllowanceSummaryService>());
            services.AddScoped<HttpMealAllowanceService>();
            services.AddScoped<IMealAllowanceReadService>(sp => sp.GetRequiredService<HttpMealAllowanceService>());
            services.AddScoped<IMealAllowanceExportService>(sp => sp.GetRequiredService<HttpMealAllowanceService>());
            services.AddScoped<IMealAllowanceRefreshService>(sp => sp.GetRequiredService<HttpMealAllowanceService>());
            services.AddScoped<IMealAllowanceLockService>(sp => sp.GetRequiredService<HttpMealAllowanceService>());
            services.AddScoped<IMealAllowanceManualAdjustmentService>(sp => sp.GetRequiredService<HttpMealAllowanceService>());
            services.AddScoped<HttpLeaveHolidayAllowanceService>();
            services.AddScoped<ILeaveHolidayAllowanceReadService>(sp =>
                sp.GetRequiredService<HttpLeaveHolidayAllowanceService>());
            services.AddScoped<ILeaveHolidayAllowancePeriodPreparationService>(sp => sp.GetRequiredService<HttpLeaveHolidayAllowanceService>());
            services.AddScoped<ILeaveHolidayAllowanceRecalculationService>(sp => sp.GetRequiredService<HttpLeaveHolidayAllowanceService>());
            services.AddScoped<ILeaveHolidayAllowanceManualAdjustmentService>(sp => sp.GetRequiredService<HttpLeaveHolidayAllowanceService>());
            services.AddScoped<ILeaveHolidayAllowanceLockService>(sp => sp.GetRequiredService<HttpLeaveHolidayAllowanceService>());
            services.AddScoped<HttpOtherAllowanceService>();
            services.AddScoped<IOtherAllowanceReadService>(sp => sp.GetRequiredService<HttpOtherAllowanceService>());
            services.AddScoped<IOtherAllowanceCreateService>(sp => sp.GetRequiredService<HttpOtherAllowanceService>());
            services.AddScoped<IOtherAllowanceUpdateService>(sp => sp.GetRequiredService<HttpOtherAllowanceService>());
            services.AddScoped<IOtherAllowanceLockService>(sp => sp.GetRequiredService<HttpOtherAllowanceService>());
            services.AddScoped<HttpOtherResponsibilityAllowanceService>();
            services.AddScoped<IOtherResponsibilityAllowanceReadService>(sp =>
                sp.GetRequiredService<HttpOtherResponsibilityAllowanceService>());
            services.AddScoped<IOtherResponsibilityAllowancePeriodPreparationService>(sp =>
                sp.GetRequiredService<HttpOtherResponsibilityAllowanceService>());
            services.AddScoped<IOtherResponsibilityAllowanceRecalculationService>(sp =>
                sp.GetRequiredService<HttpOtherResponsibilityAllowanceService>());
            services.AddScoped<IOtherResponsibilityAllowanceLockService>(sp =>
                sp.GetRequiredService<HttpOtherResponsibilityAllowanceService>());
            services.AddScoped<HttpPayrollDeductionSummaryService>();
            services.AddScoped<IPayrollDeductionSummaryReadService>(sp =>
                sp.GetRequiredService<HttpPayrollDeductionSummaryService>());
            services.AddScoped<IPayrollDeductionSummaryExportService>(sp =>
                sp.GetRequiredService<HttpPayrollDeductionSummaryService>());
            services.AddScoped<IPayrollDeductionSummarySyncService>(sp =>
                sp.GetRequiredService<HttpPayrollDeductionSummaryService>());
            services.AddScoped<IPayrollDeductionSummaryRefreshService>(sp =>
                sp.GetRequiredService<HttpPayrollDeductionSummaryService>());
            services.AddScoped<IPayrollDeductionSummaryManualAdjustmentService>(sp =>
                sp.GetRequiredService<HttpPayrollDeductionSummaryService>());
            services.AddScoped<IPayrollDeductionSummaryLockService>(sp =>
                sp.GetRequiredService<HttpPayrollDeductionSummaryService>());
            services.AddScoped<IPayrollDeductionDashboardService>(sp =>
                sp.GetRequiredService<HttpPayrollDeductionSummaryService>());
            services.AddScoped<HttpPayrollUnionFeeDeductionReadService>();
            services.AddScoped<IPayrollUnionFeeDeductionReadService>(sp =>
                sp.GetRequiredService<HttpPayrollUnionFeeDeductionReadService>());
            services.AddScoped<HttpPayrollUnionFeeDeductionCommandService>();
            services.AddScoped<IPayrollUnionFeeDeductionPeriodPreparationService>(sp =>
                sp.GetRequiredService<HttpPayrollUnionFeeDeductionCommandService>());
            services.AddScoped<IPayrollUnionFeeDeductionRefreshService>(sp =>
                sp.GetRequiredService<HttpPayrollUnionFeeDeductionCommandService>());
            services.AddScoped<IPayrollUnionFeeDeductionManualAdjustmentService>(sp =>
                sp.GetRequiredService<HttpPayrollUnionFeeDeductionCommandService>());
            services.AddScoped<IPayrollUnionFeeDeductionLockService>(sp =>
                sp.GetRequiredService<HttpPayrollUnionFeeDeductionCommandService>());
            services.AddScoped<HttpPayrollInsuranceDeductionService>();
            services.AddScoped<IPayrollInsuranceDeductionReadService>(sp =>
                sp.GetRequiredService<HttpPayrollInsuranceDeductionService>());
            services.AddScoped<IPayrollInsuranceDeductionRefreshService>(sp =>
                sp.GetRequiredService<HttpPayrollInsuranceDeductionService>());
            services.AddScoped<IPayrollInsuranceDeductionPreviousMonthSyncService>(sp =>
                sp.GetRequiredService<HttpPayrollInsuranceDeductionService>());
            services.AddScoped<IPayrollInsuranceDeductionManualAdjustmentService>(sp =>
                sp.GetRequiredService<HttpPayrollInsuranceDeductionService>());
            services.AddScoped<IPayrollInsuranceDeductionLockService>(sp =>
                sp.GetRequiredService<HttpPayrollInsuranceDeductionService>());
            services.AddScoped<IPayrollInsuranceDeductionLegacyWriteService>(sp =>
                sp.GetRequiredService<HttpPayrollInsuranceDeductionService>());
            services.AddScoped<IEmployeeTaxDependentService, HttpEmployeeTaxDependentService>();
            services.AddScoped<IPayrollEmployeeOtherDeductionAllowanceService, HttpPayrollEmployeeOtherDeductionAllowanceService>();
            services.AddScoped<HttpPayrollEmployeeSeniorityAllowanceService>();
            services.AddScoped<IPayrollEmployeeSeniorityAllowanceReadService>(sp =>
                sp.GetRequiredService<HttpPayrollEmployeeSeniorityAllowanceService>());
            services.AddScoped<IPayrollEmployeeSeniorityAllowanceRangeSummaryService>(sp =>
                sp.GetRequiredService<HttpPayrollEmployeeSeniorityAllowanceService>());
            services.AddScoped<IPayrollEmployeeSeniorityAllowancePeriodPreparationService>(sp =>
                sp.GetRequiredService<HttpPayrollEmployeeSeniorityAllowanceService>());
            services.AddScoped<IPayrollEmployeeSeniorityAllowanceRefreshService>(sp =>
                sp.GetRequiredService<HttpPayrollEmployeeSeniorityAllowanceService>());
            services.AddScoped<IPayrollEmployeeSeniorityAllowanceManualAdjustmentService>(sp =>
                sp.GetRequiredService<HttpPayrollEmployeeSeniorityAllowanceService>());
            services.AddScoped<IPayrollEmployeeSeniorityAllowanceLockService>(sp =>
                sp.GetRequiredService<HttpPayrollEmployeeSeniorityAllowanceService>());
            services.AddHazardAllowanceApi();
            services.AddScoped<IAdmsDeviceCommandService, HttpAdmsDeviceCommandService>();
            services.AddScoped<IBasicSalaryService, HttpBasicSalaryService>();
            services.AddScoped<HttpPayrollResponsibilityAllowanceWorkflowService>();
            services.AddScoped<HttpPhuCapTrachNhiemGanNhanVienService>();
            services.AddScoped<IPhuCapTrachNhiemGanNhanVienGateway>(sp =>
                sp.GetRequiredService<HttpPhuCapTrachNhiemGanNhanVienService>());
            services.AddScoped<HttpPhuCapTrachNhiemGanNhanVienXemService>();
            services.AddScoped<IPhuCapTrachNhiemGanNhanVienXemService>(sp =>
                sp.GetRequiredService<HttpPhuCapTrachNhiemGanNhanVienXemService>());
            services.AddScoped<IPayrollResponsibilityAllowanceGradeConfigurationReadService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceGradeConfigurationWriteService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceEmployeeAssignmentExportService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcQueryService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcExportService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcCommandService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcRefreshService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcCopyService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcLockService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IPayrollResponsibilityAllowanceRecalculationService>(sp =>
                sp.GetRequiredService<HttpPayrollResponsibilityAllowanceWorkflowService>());
            services.AddScoped<IEmployeeAccountService, HttpEmployeeAccountService>();
        }
    }
}
