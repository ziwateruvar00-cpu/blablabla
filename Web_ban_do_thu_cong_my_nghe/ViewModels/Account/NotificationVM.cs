using System;

namespace Web_ban_do_thu_cong_my_nghe.ViewModels.Account
{
    public class NotificationVM
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int StatusCode { get; set; }
    }
}
