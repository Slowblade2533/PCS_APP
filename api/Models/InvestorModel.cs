namespace PCS_API.Models;

public class InvestorModel
{
    public Guid InvestorId { get; set; }
    public string Title { get; set; } = string.Empty; // e.g., Mr., Ms.
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty; // เลขผู้เสียภาษี
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
