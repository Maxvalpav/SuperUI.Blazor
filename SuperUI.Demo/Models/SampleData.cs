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
                Notes = $"Employee record #{i}"
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
}
