using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;

namespace Vnta.Hrm.Infrastructure.NhanSu.ChiTietNhanVien;

public sealed class EmployeeContactProfileRowConfiguration : IEntityTypeConfiguration<EmployeeContactProfileRow>
{
    public void Configure(EntityTypeBuilder<EmployeeContactProfileRow> builder)
    {
        builder.ToTable("employee_contact_profiles");
        builder.HasKey(x => x.EmployeeId);
        builder.Property(x => x.PersonalEmail).HasMaxLength(256);
        builder.Property(x => x.PersonalPhoneNumber).HasMaxLength(30);
        builder.Property(x => x.PermanentAddress).HasColumnType("text");
        builder.Property(x => x.CurrentAddress).HasColumnType("text");
        builder.Property(x => x.EmergencyContactName).HasMaxLength(150);
        builder.Property(x => x.EmergencyContactRelationship).HasMaxLength(100);
        builder.Property(x => x.EmergencyContactPhoneNumber).HasMaxLength(30);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp without time zone");
        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithOne()
            .HasForeignKey<EmployeeContactProfileRow>(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CitizenIdentityRowConfiguration : IEntityTypeConfiguration<CitizenIdentityRow>
{
    public void Configure(EntityTypeBuilder<CitizenIdentityRow> builder)
    {
        builder.ToTable("employee_citizen_identities");
        builder.HasKey(x => x.EmployeeId);
        builder.Property(x => x.CitizenIdentityNumberCiphertext).HasColumnType("text").IsRequired();
        builder.Property(x => x.CitizenIdentityNumberHash).HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.IssuedDate).HasColumnType("date");
        builder.Property(x => x.IssuedPlace).HasMaxLength(250);
        builder.Property(x => x.ExpiryDate).HasColumnType("date");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp without time zone");
        builder.HasIndex(x => x.CitizenIdentityNumberHash)
            .IsUnique()
            .HasDatabaseName("UX_employee_citizen_identities_NumberHash");
        builder.HasOne<AttendanceGatewayEmployeeRow>()
            .WithOne()
            .HasForeignKey<CitizenIdentityRow>(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
