using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Diagnostics;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Infrastructure.BackgroundServices;

public class OrderProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderProcessingWorker> _logger;

    public OrderProcessingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Order Processing Worker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15)); // يعمل كل 15 ثانية

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                // جلب الطلبات التي ما زالت معلقة
                var pendingOrders = await context.Orders
                    .Where(o => o.Status == OrderStatus.Pending)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                if (pendingOrders.Any())
                {
                    _logger.LogInformation("Worker processing {Count} pending orders...", pendingOrders.Count);

                    foreach (var order in pendingOrders)
                    {
                        order.MarkAsCompleted();
                    }

                    await context.SaveChangesAsync(stoppingToken);

                    // إنقاص عداد الطلبات المعلقة بنفس عدد الطلبات المكتملة
                    OrderFlowDiagnostics.PendingOrdersCounter.Add(-pendingOrders.Count);

                    _logger.LogInformation("Successfully completed {Count} orders.", pendingOrders.Count);
                }
            }
            catch (Exception ex)
            {
                // تسجيل الأخطاء بمستوى Error (مطلب رئيسي في اللوجز)
                _logger.LogError(ex, "An error occurred while processing orders in the background.");
            }
        }

        _logger.LogInformation("Background Order Processing Worker stopped.");
    }
}