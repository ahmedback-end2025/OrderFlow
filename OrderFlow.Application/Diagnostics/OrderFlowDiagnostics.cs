using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderFlow.Application.Diagnostics
{
    

    public static class OrderFlowDiagnostics
    {
        public const string ServiceName = "OrderFlow";

        // 1. Tracing Source (لتتبع العمليات)
        public static readonly ActivitySource ActivitySource = new(ServiceName);

        // 2. Metrics Meter (لجمع المقاييس)
        public static readonly Meter Meter = new(ServiceName, "1.0.0");

        // العدادات المطلوبة
        public static readonly Counter<int> OrdersCreatedCounter =
            Meter.CreateCounter<int>("orders_created_total", description: "Total orders created");

        public static readonly UpDownCounter<int> PendingOrdersCounter =
            Meter.CreateUpDownCounter<int>("orders_pending_current", description: "Current pending orders");

        // 3. تخزين حالة التبعيات (Redis & SQL Server)
        public static readonly ConcurrentDictionary<string, int> DependencyHealth = new();

        static OrderFlowDiagnostics()
        {
            // مقياس يقرأ حالة التبعيات كأرقام (1 = سليم، 0 = معطل) لإرسالها لـ Prometheus و Grafana
            Meter.CreateObservableGauge("dependency_health",
                () => DependencyHealth.Select(x => new Measurement<int>(x.Value, new KeyValuePair<string, object?>("dependency", x.Key))),
                description: "1 if healthy, 0 if unhealthy");
        }
    }
}
