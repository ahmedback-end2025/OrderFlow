using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Diagnostics;
using System.Text.Json;

namespace OrderFlow.Application.Common.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailsDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GetOrderByIdQueryHandler> _logger;

    public GetOrderByIdQueryHandler(
        IApplicationDbContext context,
        IDistributedCache cache,
        ILogger<GetOrderByIdQueryHandler> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<OrderDetailsDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        using var activity = OrderFlowDiagnostics.ActivitySource.StartActivity("GetOrderById");
        activity?.SetTag("order.id", request.Id.ToString());

        var cacheKey = $"orders:{request.Id}";

        // 1. فحص كاش Redis
        var cachedOrder = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedOrder))
        {
            _logger.LogInformation("Cache HIT for Order ID: {OrderId}", request.Id);
            activity?.SetTag("cache.hit", true);
            return JsonSerializer.Deserialize<OrderDetailsDto>(cachedOrder);
        }

        // 2. في حالة عدم وجوده في الكاش
        _logger.LogInformation("Cache MISS for Order ID: {OrderId}. Fetching from Database...", request.Id);
        activity?.SetTag("cache.hit", false);

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order with ID: {OrderId} was not found.", request.Id);
            return null;
        }

        var orderDto = new OrderDetailsDto(
            order.Id,
            order.CustomerName,
            order.TotalAmount,
            order.Status.ToString(),
            order.CreatedAtUtc,
            order.Items.Select(i => new OrderItemDetailsDto(i.ProductName, i.Quantity, i.UnitPrice)).ToList()
        );

        // 3. حفظه في Redis لمدة 5 دقائق
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(orderDto), cacheOptions, cancellationToken);

        _logger.LogInformation("Order ID: {OrderId} fetched from DB and cached.", request.Id);
        return orderDto;
    }
}