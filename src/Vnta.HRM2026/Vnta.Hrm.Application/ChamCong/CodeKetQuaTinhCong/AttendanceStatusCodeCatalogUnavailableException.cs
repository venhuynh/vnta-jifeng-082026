namespace Vnta.Hrm.Application.ChamCong.CodeKetQuaTinhCong;

public sealed class AttendanceStatusCodeCatalogUnavailableException : Exception
{
    public AttendanceStatusCodeCatalogUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
