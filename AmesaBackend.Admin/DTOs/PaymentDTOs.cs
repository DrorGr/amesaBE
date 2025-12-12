namespace AmesaBackend.Admin.DTOs
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EUR";
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ReferenceId { get; set; }
        public Guid? PaymentMethodId { get; set; }
        public string? ProviderTransactionId { get; set; }
        public Guid? ProductId { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

