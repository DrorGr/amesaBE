using System.ComponentModel.DataAnnotations;

namespace AmesaBackend.Payment.DTOs;

public class CreatePaymentIntentRequest
{
    [Required]
    [Range(0.01, 10000)]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public Guid? PaymentMethodId { get; set; }

    public Guid? ProductId { get; set; }

    public int? Quantity { get; set; }

    [MaxLength(255)]
    public string? IdempotencyKey { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}

public class PaymentIntentResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public bool RequiresAction { get; set; }
    public string? NextAction { get; set; }
}

public class ConfirmPaymentIntentRequest
{
    [Required]
    public string PaymentIntentId { get; set; } = string.Empty;

    public Guid? PaymentMethodId { get; set; }

    [MaxLength(255)]
    public string? ReturnUrl { get; set; }
}

public class SetupIntentResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string SetupIntentId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class StripePaymentMethodDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public StripeCardDto? Card { get; set; }
    public bool IsDefault { get; set; }
}

public class StripeCardDto
{
    public string? Brand { get; set; }
    public string? Last4 { get; set; }
    public int? ExpMonth { get; set; }
    public int? ExpYear { get; set; }
}

public class WebhookEventResult
{
    public bool Processed { get; set; }
    public string? Message { get; set; }
}

