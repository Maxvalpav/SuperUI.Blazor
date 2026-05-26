using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SuperUI.Demo.Models;

public enum OrderStatus
{
    [Display(Name = "Новый")]
    New,
    [Display(Name = "В обработке")]
    Processing,
    [Display(Name = "Отправлен")]
    Shipped,
    [Display(Name = "Доставлен")]
    Delivered,
    [Display(Name = "Отменён")]
    Cancelled
}

public enum Priority
{
    [Description("Низкий приоритет")]
    Low,
    [Description("Средний приоритет")]
    Medium,
    [Description("Высокий приоритет")]
    High,
    [Description("Критический")]
    Critical
}

public enum Permission
{
    [Display(Name = "Чтение")]
    Read,
    [Display(Name = "Запись")]
    Write,
    [Display(Name = "Удаление")]
    Delete,
    [Display(Name = "Администратор")]
    Admin
}
