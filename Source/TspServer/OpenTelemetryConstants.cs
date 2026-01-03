using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TspServer;

internal sealed class OpenTelemetryConstants
{
    // Имя сервиса
    public const string ServiceName = "TCP server";
    public const string ServiceVersion = "1.0.0";

    // ActivitySource для трассировки
    public static readonly ActivitySource MyActivitySource = new(ServiceName);

    // Meter для метрик
    public static readonly Meter MyMeter = new(ServiceName);
}
