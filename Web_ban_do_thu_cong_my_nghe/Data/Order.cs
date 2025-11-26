using System;
using System.Collections.Generic;

namespace Web_ban_do_thu_cong_my_nghe.Data;

public partial class Order
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public int? Status { get; set; }

    public decimal TotalMoney { get; set; }

    public string? Notes { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string ShippingPhone { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual User? User { get; set; }
}
