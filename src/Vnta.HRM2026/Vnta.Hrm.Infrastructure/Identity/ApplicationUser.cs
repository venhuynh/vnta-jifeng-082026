using Microsoft.AspNetCore.Identity;

namespace Vnta.Hrm.Infrastructure.Identity {
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser {
        public Guid? EmployeeId { get; set; }

        public EmployeeAccountApprovalStatus ApprovalStatus { get; set; } = EmployeeAccountApprovalStatus.Draft;

        public string? AccessLevel { get; set; }

        public string? ApprovedByUserId { get; set; }

        public DateTime? ApprovedAtUtc { get; set; }

        public DateTime? RejectedAtUtc { get; set; }

        public string? RejectionReason { get; set; }

        public bool IsActive { get; set; }
    }
}

