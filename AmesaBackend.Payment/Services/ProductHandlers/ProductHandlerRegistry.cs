using AmesaBackend.Payment.DTOs;

namespace AmesaBackend.Payment.Services.ProductHandlers;

public interface IProductHandlerRegistry
{
    IProductHandler? GetHandler(string productType);
    void RegisterHandler(IProductHandler handler);
}

public class ProductHandlerRegistry : IProductHandlerRegistry
{
    private readonly Dictionary<string, IProductHandler> _handlers = new();

    public IProductHandler? GetHandler(string productType)
    {
        return _handlers.TryGetValue(productType, out var handler) ? handler : null;
    }

    public void RegisterHandler(IProductHandler handler)
    {
        _handlers[handler.HandlesType] = handler;
    }
}

