namespace Vnta.Hrm.Web.Client {
    // Add properties to this class and update the server and client AuthenticationStateProviders
    // to expose more information about the authenticated user to the client.
    public class UserInfo {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Role { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = [];
        public IReadOnlyList<string> Permissions { get; set; } = [];
        public string? EmployeeId { get; set; }
        public string? AccessLevel { get; set; }
        public string? ApprovalStatus { get; set; }
        public bool IsActive { get; set; }
    }
}

