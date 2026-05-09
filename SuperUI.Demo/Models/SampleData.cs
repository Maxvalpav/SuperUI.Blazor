namespace SuperUI.Demo.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public bool IsAvailable { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

// ── AutoDetail demo models ────────────────────────────────────────────────────

public class Address
{
    [System.ComponentModel.DataAnnotations.Display(Name = "Страна", Order = 1)]
    public string Country { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Display(Name = "Город", Order = 2)]
    public string City { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Display(Name = "Улица", Order = 3)]
    public string Street { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Display(Name = "Индекс", Order = 4)]
    public string PostalCode { get; set; } = string.Empty;
}

public class EmployeeProject
{
    [System.ComponentModel.DataAnnotations.Display(Name = "ID", Order = 1)]
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Display(Name = "Проект", Order = 2)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Display(Name = "Роль", Order = 3)]
    public string Role { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Display(Name = "Начало", Order = 4)]
    public DateTime StartDate { get; set; }

    [System.ComponentModel.DataAnnotations.Display(Name = "Часов/нед", Order = 5)]
    public int HoursPerWeek { get; set; }
}

public class EmployeeSkill
{
    [System.ComponentModel.DataAnnotations.Display(Name = "Навык", Order = 1)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Display(Name = "Уровень", Order = 2)]
    public string Level { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Display(Name = "Лет опыта", Order = 3)]
    public int YearsOfExperience { get; set; }
}

public class EmployeeDetailed
{
    [System.ComponentModel.DataAnnotations.Display(Name = "ID", Order = 0)]
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Display(Name = "Имя", Order = 1)]
    public string FirstName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Display(Name = "Фамилия", Order = 2)]
    public string LastName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Display(Name = "Отдел", Order = 3)]
    public DepartmentType Department { get; set; }

    [System.ComponentModel.DataAnnotations.Display(Name = "Зарплата", Order = 4)]
    [System.ComponentModel.DataAnnotations.DisplayFormat(DataFormatString = "N0")]
    public decimal Salary { get; set; }

    [System.ComponentModel.DataAnnotations.Display(Name = "Активен", Order = 5)]
    public bool IsActive { get; set; }

    // Object property → rendered as SgDataForm
    [System.ComponentModel.DataAnnotations.Display(Name = "Адрес", Order = 6)]
    public Address Address { get; set; } = new();

    // Collection property → rendered as SgDataGrid
    [System.ComponentModel.DataAnnotations.Display(Name = "Проекты", Order = 7)]
    public List<EmployeeProject> Projects { get; set; } = new();

    // Collection property → rendered as SgDataGrid
    [System.ComponentModel.DataAnnotations.Display(Name = "Навыки", Order = 8)]
    public List<EmployeeSkill> Skills { get; set; } = new();
}

public class SampleDataService
{
    private static readonly string[] FirstNames = 
    { 
        "Иван", "Петр", "Сергей", "Алексей", "Дмитрий", 
        "Николай", "Владимир", "Андрей", "Юрий", "Константин",
        "Мария", "Анна", "Елена", "Ольга", "Татьяна"
    };

    private static readonly string[] LastNames = 
    { 
        "Иванов", "Петров", "Сидоров", "Смирнов", "Соколов",
        "Лебедев", "Козлов", "Новиков", "Морозов", "Волков"
    };

    private static readonly DepartmentType[] Departments = 
    { 
        DepartmentType.Engineering, DepartmentType.Design, DepartmentType.Marketing, 
        DepartmentType.Product, DepartmentType.HR
    };

    private static readonly string[] Categories = 
    { 
        "Electronics", "Clothing", "Books", "Food", "Furniture", "Sports", "Toys"
    };

    private static readonly string[] OrderStatuses = 
    { 
        "Pending", "Processing", "Shipped", "Delivered", "Cancelled"
    };

    public static List<Employee> GenerateEmployees(int count = 10000)
    {
        var employees = new List<Employee>();
        var random = new Random(42);

        var positions = new[] { "Junior Developer", "Senior Developer", "Team Lead", "Architect", "Manager", "Designer", "QA Engineer", "DevOps Engineer" };
        var managers = new[] { "Иван Иванов", "Петр Петров", "Сергей Сидоров", "Алексей Смирнов", "Дмитрий Соколов" };

        for (int i = 1; i <= count; i++)
        {
            employees.Add(new Employee
            {
                Id = i,
                FirstName = FirstNames[random.Next(FirstNames.Length)],
                LastName = LastNames[random.Next(LastNames.Length)],
                Email = $"employee{i}@company.com",
                Department = Departments[random.Next(Departments.Length)],
                Salary = random.Next(30000, 150000),
                HireDate = DateTime.Now.AddDays(-random.Next(1, 3650)),
                IsActive = random.Next(0, 100) > 10,
                Notes = $"Employee record #{i}",
                Phone = $"+7 ({random.Next(900, 999)}) {random.Next(100, 999)}-{random.Next(10, 99)}-{random.Next(10, 99)}",
                Position = positions[random.Next(positions.Length)],
                Manager = managers[random.Next(managers.Length)],
                YearsOfExperience = random.Next(0, 30),
                Level = new[] { "Junior", "Middle", "Senior", "Lead", "Principal" }[random.Next(5)],
                ProjectsCount = random.Next(1, 50),
                Rating = (decimal)(random.NextDouble() * 5),
                LastVacationDate = DateTime.Now.AddDays(-random.Next(30, 365))
            });
        }

        return employees;
    }

    public static List<Product> GenerateProducts(int count = 1000)
    {
        var products = new List<Product>();
        var random = new Random(42);

        for (int i = 1; i <= count; i++)
        {
            products.Add(new Product
            {
                Id = i,
                Name = $"Product {i}",
                Category = Categories[random.Next(Categories.Length)],
                Price = random.Next(100, 10000) + (decimal)random.NextDouble(),
                Stock = random.Next(0, 1000),
                Description = $"High-quality product with excellent features",
                CreatedDate = DateTime.Now.AddDays(-random.Next(1, 365)),
                IsAvailable = random.Next(0, 100) > 20
            });
        }

        return products;
    }

    public static List<Order> GenerateOrders(int count = 5000)
    {
        var orders = new List<Order>();
        var random = new Random(42);

        for (int i = 1; i <= count; i++)
        {
            orders.Add(new Order
            {
                Id = i,
                OrderNumber = $"ORD-{DateTime.Now.Year}-{i:D6}",
                OrderDate = DateTime.Now.AddDays(-random.Next(1, 365)),
                Total = random.Next(1000, 100000) + (decimal)random.NextDouble(),
                Status = OrderStatuses[random.Next(OrderStatuses.Length)],
                Customer = $"{FirstNames[random.Next(FirstNames.Length)]} {LastNames[random.Next(LastNames.Length)]}",
                ItemCount = random.Next(1, 50)
            });
        }

        return orders;
    }

    public static List<EmployeeDetailed> GenerateDetailedEmployees(int count = 100)
    {
        var employees = new List<EmployeeDetailed>();
        var random = new Random(42);

        var cities = new[] { "Москва", "Санкт-Петербург", "Новосибирск", "Екатеринбург", "Казань" };
        var streets = new[] { "Ленина", "Пушкина", "Гагарина", "Мира", "Советская" };
        var projects = new[] { "CRM System", "Mobile App", "Analytics Platform", "E-commerce", "AI Assistant" };
        var roles = new[] { "Developer", "Lead", "Architect", "QA", "Designer" };
        var skills = new[] { "C#", "JavaScript", "Python", "SQL", "React", "Azure", "Docker", "Git" };
        var levels = new[] { "Junior", "Middle", "Senior", "Expert" };

        for (int i = 1; i <= count; i++)
        {
            var emp = new EmployeeDetailed
            {
                Id = i,
                FirstName = FirstNames[random.Next(FirstNames.Length)],
                LastName = LastNames[random.Next(LastNames.Length)],
                Department = Departments[random.Next(Departments.Length)],
                Salary = random.Next(50000, 200000),
                IsActive = random.Next(0, 100) > 15,
                Address = new Address
                {
                    Country = "Россия",
                    City = cities[random.Next(cities.Length)],
                    Street = $"ул. {streets[random.Next(streets.Length)]}, д. {random.Next(1, 150)}",
                    PostalCode = $"{random.Next(100000, 999999)}"
                }
            };

            // Generate 1-4 projects
            int projectCount = random.Next(1, 5);
            for (int p = 0; p < projectCount; p++)
            {
                emp.Projects.Add(new EmployeeProject
                {
                    Id = p + 1,
                    Name = projects[random.Next(projects.Length)],
                    Role = roles[random.Next(roles.Length)],
                    StartDate = DateTime.Now.AddMonths(-random.Next(1, 36)),
                    HoursPerWeek = random.Next(10, 40)
                });
            }

            // Generate 2-6 skills
            int skillCount = random.Next(2, 7);
            var usedSkills = new HashSet<string>();
            for (int s = 0; s < skillCount; s++)
            {
                var skill = skills[random.Next(skills.Length)];
                if (usedSkills.Add(skill))
                {
                    emp.Skills.Add(new EmployeeSkill
                    {
                        Name = skill,
                        Level = levels[random.Next(levels.Length)],
                        YearsOfExperience = random.Next(1, 10)
                    });
                }
            }

            employees.Add(emp);
        }

        return employees;
    }

}
