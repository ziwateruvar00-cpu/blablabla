using System.Collections.Generic;
using System.Linq;

namespace Web_ban_do_thu_cong_my_nghe.Helpers;

public static class OrderStatusHelper
{
    public const int Pending = 0;
    public const int Shipping = 1;
    public const int Completed = 2;

    private static readonly IReadOnlyDictionary<int, string> StatusLabels = new Dictionary<int, string>
    {
        { Pending, "Chờ xác nhận" },
        { Shipping, "Đang giao hàng" },
        { Completed, "Đã hoàn thành" }
    };

    public static IReadOnlyDictionary<int, string> AllStatuses => StatusLabels;

    public static bool IsValid(int status) => StatusLabels.ContainsKey(status);

    public static string GetLabel(int status) => StatusLabels.TryGetValue(status, out var label)
        ? label
        : "Không xác định";

    public static string GetLabel(int? status)
    {
        if (!status.HasValue)
        {
            return StatusLabels[Pending];
        }

        return GetLabel(status.Value);
    }

    public static int Normalize(int? status) => status.HasValue && StatusLabels.ContainsKey(status.Value)
        ? status.Value
        : Pending;

    public static string GetStatusText(int status) => GetLabel(status);

    public static string GetStatusText(int? status) => GetLabel(status);

    public static string GetStatusClass(int status) => status switch
    {
        Pending => "badge-warning",
        Shipping => "badge-info",
        Completed => "badge-success",
        _ => "badge-secondary"
    };

    public static string GetStatusClass(int? status) => GetStatusClass(Normalize(status));

    public static IEnumerable<(int Value, string Label)> GetStatusOptions() => StatusLabels.Select(kvp => (kvp.Key, kvp.Value));
}
