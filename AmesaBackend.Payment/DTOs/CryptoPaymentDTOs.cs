using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Payment.DTOs;

public class CreateCryptoChargeRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [MaxLength(255)]
    public string? IdempotencyKey { get; set; }
}

public class CoinbaseChargeResponse
{
    public string ChargeId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string HostedUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public List<CoinbasePayment> Payments { get; set; } = new();
    public CoinbasePricing Pricing { get; set; } = new();
}

public class CoinbasePayment
{
    public string Network { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public CoinbaseValue Value { get; set; } = new();
    public CoinbaseValue Block { get; set; } = new();
}

public class CoinbasePricing
{
    public CoinbaseValue Local { get; set; } = new();
    public CoinbaseValue Bitcoin { get; set; } = new();
    public CoinbaseValue Ethereum { get; set; } = new();
    public CoinbaseValue? Usdc { get; set; }
    public CoinbaseValue? Usdt { get; set; }
    public CoinbaseValue? Dai { get; set; }
}

public class CoinbaseValue
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class SupportedCrypto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsStablecoin { get; set; }
}

