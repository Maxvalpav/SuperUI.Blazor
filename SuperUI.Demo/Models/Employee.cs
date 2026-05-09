using System.ComponentModel.DataAnnotations;

namespace SuperUI.Demo.Models;

public enum DepartmentType
{
    [Display(Name = "Разработка")]
    Engineering,
    [Display(Name = "Дизайн")]
    Design,
    [Display(Name = "Маркетинг")]
    Marketing,
    [Display(Name = "Продукт")]
    Product,
    [Display(Name = "HR")]
    HR
}

public class Employee
{
    public int Id { get; set; }

    [Display(Name = "Имя", Order = 1, Prompt = "Введите имя")]
    [Required(ErrorMessage = "Имя обязательно")]
    [StringLength(50, ErrorMessage = "Не более 50 символов")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Фамилия", Order = 2, Prompt = "Введите фамилию")]
    [Required(ErrorMessage = "Фамилия обязательна")]
    [StringLength(50, ErrorMessage = "Не более 50 символов")]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Email", Order = 3, Prompt = "user@company.com")]
    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Отдел", Order = 4)]
    public DepartmentType Department { get; set; } = DepartmentType.Engineering;

    [Display(Name = "Зарплата", Order = 5)]
    [Range(0, 1000000, ErrorMessage = "Зарплата должна быть от 0 до 1,000,000")]
    public decimal Salary { get; set; }

    [Display(Name = "Дата приема", Order = 6)]
    public DateTime HireDate { get; set; } = DateTime.Today;

    [Display(Name = "Активен", Order = 7)]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Заметки", Order = 8)]
    [StringLength(500, ErrorMessage = "Не более 500 символов")]
    public string? Notes { get; set; }

    [Display(Name = "Телефон", Order = 9)]
    [Phone(ErrorMessage = "Некорректный номер телефона")]
    public string? Phone { get; set; }

    [Display(Name = "Должность", Order = 10)]
    [StringLength(100, ErrorMessage = "Не более 100 символов")]
    public string? Position { get; set; }

    [Display(Name = "Руководитель", Order = 11)]
    [StringLength(100, ErrorMessage = "Не более 100 символов")]
    public string? Manager { get; set; }

    [Display(Name = "Опыт (лет)", Order = 12)]
    [Range(0, 60, ErrorMessage = "Опыт должен быть от 0 до 60 лет")]
    public int? YearsOfExperience { get; set; }

    [Display(Name = "Уровень", Order = 13)]
    [StringLength(50, ErrorMessage = "Не более 50 символов")]
    public string? Level { get; set; }

    [Display(Name = "Проектов", Order = 14)]
    [Range(0, 1000, ErrorMessage = "Количество проектов должно быть от 0 до 1000")]
    public int? ProjectsCount { get; set; }

    [Display(Name = "Рейтинг", Order = 15)]
    [Range(0, 5, ErrorMessage = "Рейтинг должен быть от 0 до 5")]
    public decimal? Rating { get; set; }

    [Display(Name = "Последний отпуск", Order = 16)]
    public DateTime? LastVacationDate { get; set; }
}
