using System;
using System.Collections.Generic;
using Web_ban_do_thu_cong_my_nghe.Data;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels
{
    public class AdminDashboardVM
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueThisYear { get; set; }
        public int ProductCount { get; set; }
        public int CategoryCount { get; set; }
        public int InventoryInStock { get; set; }
        public int UnitsSold { get; set; }
        public int StaffCount { get; set; }
        public int CustomerCount { get; set; }
        public int PendingOrders { get; set; }
        public int ShippingOrders { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<TopProductVM> TopProducts { get; set; } = new();
    }

    public class TopProductVM
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Stock { get; set; }
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class AdminOrderIndexVM
    {
        public List<Order> Orders { get; set; } = new();
        public int PendingCount { get; set; }
        public int ShippingCount { get; set; }
        public int CompletedCount { get; set; }
        public int TotalUnitsSold { get; set; }
        public int InventoryInStock { get; set; }
        public decimal RevenueToday { get; set; }
    }

    public class AdminReportVM
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public List<TopProductVM> TopProducts { get; set; } = new();
        public List<InventoryAlertVM> InventoryAlerts { get; set; } = new();
        public List<RevenuePoint> RevenuePoints { get; set; } = new();

        public List<InventoryAlertVM> LowStocks
        {
            get => InventoryAlerts;
            set => InventoryAlerts = value;
        }

        public List<RevenuePoint> RevenueSeries
        {
            get => RevenuePoints;
            set => RevenuePoints = value;
        }
    }

    public class InventoryAlertVM
    {
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int Threshold { get; set; } = 10;

        public int Stock
        {
            get => CurrentStock;
            set => CurrentStock = value;
        }
    }

    public class RevenuePoint
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
    }
}