using MediatR;
using Microsoft.Extensions.Logging;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Diagnostics;
using OrderFlow.Domain;

namespace OrderFlow.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IApplicationDbContext context,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. بدء التتبع (Tracing)
        using var activity = OrderFlowDiagnostics.ActivitySource.StartActivity("CreateOrder");

        _logger.LogInformation("Starting order creation for customer: {CustomerName} with {ItemCount} items",
            request.CustomerName, request.Items?.Count ?? 0);

        if (request.Items == null || !request.Items.Any())
        {
            _logger.LogWarning("Order creation failed: items list is empty for customer {CustomerName}", request.CustomerName);
            throw new ArgumentException("Order must contain at least one item.");
        }

        var order = new Order(request.CustomerName);

        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductName, item.Quantity, item.UnitPrice);
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        // 2. تحديث المقاييس (Metrics)
        OrderFlowDiagnostics.OrdersCreatedCounter.Add(1);
        OrderFlowDiagnostics.PendingOrdersCounter.Add(1);

        // 3. التسجيل المنظم للنجاح (Structured Logging)
        _logger.LogInformation("Order created successfully with ID: {OrderId} and Total Amount: {TotalAmount}",
            order.Id, order.TotalAmount);

        // إضافة وسوم للـ Trace لسهولة الفحص في Grafana
        activity?.SetTag("order.id", order.Id.ToString());
        activity?.SetTag("order.total", order.TotalAmount);

        return order.Id;
    }
}