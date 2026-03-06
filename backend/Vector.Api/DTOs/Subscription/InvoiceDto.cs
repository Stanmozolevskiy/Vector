namespace Vector.Api.DTOs.Subscription;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public string PlanType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime PaidAt { get; set; }
    public string Status { get; set; } = string.Empty; // paid, pending, failed
    public string? InvoiceUrl { get; set; }
}

