using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OrderFlow.Application.Common.Interfaces;
using OrderFlow.Application.Diagnostics;
using OrderFlow.Diagnostics;
using OrderFlow.Infrastructure.BackgroundServices;
using OrderFlow.Infrastructure.Data;
using OrderFlow.Infrastructure.BackgroundServices;

namespace OrderFlow
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. ????? ???????? ???? CQRS
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<OrderFlowDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<OrderFlowDbContext>());

            builder.Services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(IApplicationDbContext).Assembly));

            // 2. Health Checks
            var redisConnection = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";

            builder.Services.AddHealthChecks()
                .AddSqlServer(connectionString!, name: "sqlserver")
                .AddRedis(redisConnection, name: "redis");

            builder.Services.AddSingleton<IHealthCheckPublisher, MetricsHealthCheckPublisher>();
            builder.Services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Delay = TimeSpan.FromSeconds(3); // ??? ????? ??? 3 ????? ?? ????? ???????
                options.Period = TimeSpan.FromSeconds(10); // ????? ????? ?? 10 ?????
            });

          //  var redisConnection = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
                             
            // ????? IDistributedCache ????? ??? Redis
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "OrderFlow_";
            });

            // 3. OpenTelemetry (Metrics & Tracing)
            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics => metrics
                    .AddMeter(OrderFlowDiagnostics.ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddPrometheusExporter())
                .WithTracing(tracing => tracing
                    .AddSource(OrderFlowDiagnostics.ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddRedisInstrumentation());


            builder.Services.AddHostedService<OrderProcessingWorker>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();
            app.MapControllers();

            // 4. Endpoints ???????? ??????
            app.MapHealthChecks("/health");
            app.MapPrometheusScrapingEndpoint(); // ???? /metrics ???? ????? ??? Prometheus

            app.Run();
        }
    }
}