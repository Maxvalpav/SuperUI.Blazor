namespace SuperUI.Demo.Models;

// ── Enums ──────────────────────────────────────────────────────────────────────

public enum PaymentMethod { CreditCard, DebitCard, PayPal, BankTransfer, Cash }
public enum ProjectPriority { Low, Medium, High, Critical }
public enum ProjectStatus { Planning, Active, OnHold, Completed, Cancelled }
public enum Currency { USD, EUR, RUB, GBP, JPY }
public enum TxType { Credit, Debit, Transfer, Fee, Refund }
public enum SensorType { Temperature, Humidity, Pressure, Light, Motion, Gas }
public enum SensorStatus { Normal, Warning, Alert, Offline }
public enum Major { ComputerScience, Mathematics, Physics, Biology, Chemistry, Engineering, Economics, Law, Medicine, Arts }
public enum YearOfStudy { Freshman, Sophomore, Junior, Senior, Graduate }

// ── Existing models (kept compatible) ──────────────────────────────────────────

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
    public decimal? Cost { get; set; }
    public double? WeightKg { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public double Rating { get; set; }
    public string SKU { get; set; } = string.Empty;
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ShipDate { get; set; }
    public decimal Total { get; set; }
    public decimal? Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public string Customer { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public OrderStatus Status { get; set; }
    public bool IsPaid { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public double? Weight { get; set; }
}

// ── New models ─────────────────────────────────────────────────────────────────

public class BigEmployee
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public Guid GlobalId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DepartmentType Department { get; set; }
    public string Position { get; set; } = string.Empty;
    public ExperienceLevel Level { get; set; }
    public decimal Salary { get; set; }
    public decimal? Bonus { get; set; }
    public double TaxRate { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? LastReviewDate { get; set; }
    public DateOnly BirthDate { get; set; }
    public TimeOnly ShiftStart { get; set; }
    public bool IsActive { get; set; }
    public bool IsManager { get; set; }
    public int? YearsOfExperience { get; set; }
    public int ProjectsCount { get; set; }
    public double Rating { get; set; }
    public float Efficiency { get; set; }
    public short VacationDaysRemaining { get; set; }
    public byte SickDaysThisYear { get; set; }
    public DateTimeOffset LastLogin { get; set; }
    public TimeSpan SessionDuration { get; set; }
    public string? Notes { get; set; }
    public string[] Skills { get; set; } = [];
}

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DepartmentType Department { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal Budget { get; set; }
    public decimal? Spent { get; set; }
    public int TeamSize { get; set; }
    public ProjectPriority Priority { get; set; }
    public ProjectStatus Status { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public TimeSpan Duration { get; set; }
}

public class FinancialTransaction
{
    public long Id { get; set; }
    public Guid TransactionId { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public Currency Currency { get; set; }
    public TxType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public DateOnly ValueDate { get; set; }
    public bool IsReconciled { get; set; }
    public decimal? Fee { get; set; }
    public double? ExchangeRate { get; set; }
}

public class SensorReading
{
    public long Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public SensorType SensorType { get; set; }
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public float MinThreshold { get; set; }
    public float MaxThreshold { get; set; }
    public string Unit { get; set; } = string.Empty;
    public byte BatteryLevel { get; set; }
    public short SignalStrength { get; set; }
    public bool IsAlert { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public SensorStatus Status { get; set; }
    public DateTime? LastMaintenance { get; set; }
}

public class Student
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public double GPA { get; set; }
    public int CreditsCompleted { get; set; }
    public bool IsActive { get; set; }
    public Major Major { get; set; }
    public YearOfStudy Year { get; set; }
    public decimal? Scholarship { get; set; }
    public bool HasGraduated { get; set; }
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
    [System.ComponentModel.DataAnnotations.Display(Name = "Адрес", Order = 6)]
    public Address Address { get; set; } = new();
    [System.ComponentModel.DataAnnotations.Display(Name = "Проекты", Order = 7)]
    public List<EmployeeProject> Projects { get; set; } = new();
    [System.ComponentModel.DataAnnotations.Display(Name = "Навыки", Order = 8)]
    public List<EmployeeSkill> Skills { get; set; } = new();
}

// ── Service ────────────────────────────────────────────────────────────────────

public class SampleDataService
{
    private static readonly string[] FirstNames = { "Иван", "Петр", "Сергей", "Алексей", "Дмитрий", "Николай", "Владимир", "Андрей", "Юрий", "Константин", "Мария", "Анна", "Елена", "Ольга", "Татьяна" };
    private static readonly string[] LastNames = { "Иванов", "Петров", "Сидоров", "Смирнов", "Соколов", "Лебедев", "Козлов", "Новиков", "Морозов", "Волков" };
    private static readonly string[] EnFirstNames = { "James", "John", "Robert", "Michael", "William", "Mary", "Patricia", "Jennifer", "Linda", "Barbara" };
    private static readonly string[] EnLastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
    private static readonly DepartmentType[] Departments = { DepartmentType.Engineering, DepartmentType.Design, DepartmentType.Marketing, DepartmentType.Product, DepartmentType.HR };
    private static readonly string[] Categories = { "Electronics", "Clothing", "Books", "Food", "Furniture", "Sports", "Toys" };
    private static readonly string[] Positions = { "Junior Developer", "Senior Developer", "Team Lead", "Architect", "Manager", "Designer", "QA Engineer", "DevOps Engineer" };
    private static readonly string[] Managers = { "Иван Иванов", "Петр Петров", "Сергей Сидоров", "Алексей Смирнов", "Дмитрий Соколов" };
    private static readonly string[] SkillNames = { "C#", "JavaScript", "Python", "SQL", "React", "Azure", "Docker", "Git", "Kubernetes", "TypeScript" };
    private static readonly string[] Categories2 = { "Cleaning", "Lawn", "HVAC", "Electrical", "Plumbing" };

    public static List<Employee> GenerateEmployees(int count = 10000, int seed = 42)
    {
        var r = new Random(seed);
        var list = new List<Employee>(count);
        for (int i = 1; i <= count; i++)
            list.Add(new Employee
            {
                Id = i,
                FirstName = FirstNames[r.Next(FirstNames.Length)],
                LastName = LastNames[r.Next(LastNames.Length)],
                Email = $"employee{i}@company.com",
                Department = Departments[r.Next(Departments.Length)],
                Salary = r.Next(30000, 150000),
                HireDate = DateTime.Now.AddDays(-r.Next(1, 3650)),
                IsActive = r.Next(0, 100) > 10,
                Notes = $"Employee record #{i}",
                Phone = $"+7 ({r.Next(900, 999)}) {r.Next(100, 999)}-{r.Next(10, 99)}-{r.Next(10, 99)}",
                Position = Positions[r.Next(Positions.Length)],
                Manager = Managers[r.Next(Managers.Length)],
                YearsOfExperience = r.Next(0, 30),
                Level = new[] { "Junior", "Middle", "Senior", "Lead", "Principal" }[r.Next(5)],
                ProjectsCount = r.Next(1, 50),
                Rating = (decimal)(r.NextDouble() * 5),
                LastVacationDate = DateTime.Now.AddDays(-r.Next(30, 365))
            });
        return list;
    }

    public static List<Product> GenerateProducts(int count = 1000, int seed = 42)
    {
        var r = new Random(seed);
        var list = new List<Product>(count);
        for (int i = 1; i <= count; i++)
            list.Add(new Product
            {
                Id = i,
                Name = $"Product {i}",
                Category = Categories[r.Next(Categories.Length)],
                Price = r.Next(100, 10000) + (decimal)r.NextDouble(),
                Stock = r.Next(0, 1000),
                Description = "High-quality product with excellent features",
                CreatedDate = DateTime.Now.AddDays(-r.Next(1, 365)),
                IsAvailable = r.Next(0, 100) > 20,
                Cost = r.Next(0, 100) > 30 ? (decimal)r.NextDouble() * (decimal)r.Next(50, 8000) : null,
                WeightKg = r.Next(0, 100) > 40 ? r.NextDouble() * 50 : null,
                ExpiryDate = r.Next(0, 100) > 60 ? DateOnly.FromDateTime(DateTime.Now.AddDays(r.Next(30, 730))) : null,
                Rating = Math.Round(r.NextDouble() * 5, 1),
                SKU = $"SKU-{Categories[r.Next(Categories.Length)].ToUpper().Substring(0, 2)}-{i:D4}"
            });
        return list;
    }

    public static List<Order> GenerateOrders(int count = 5000, int seed = 42)
    {
        var r = new Random(seed);
        var statuses = Enum.GetValues<OrderStatus>();
        var payments = Enum.GetValues<PaymentMethod>();
        var list = new List<Order>(count);
        for (int i = 1; i <= count; i++)
            list.Add(new Order
            {
                Id = i,
                OrderNumber = $"ORD-{DateTime.Now.Year}-{i:D6}",
                OrderDate = DateTime.Now.AddDays(-r.Next(1, 365)),
                ShipDate = r.Next(0, 100) > 20 ? DateTime.Now.AddDays(-r.Next(0, 30)) : null,
                Total = r.Next(1000, 100000) + (decimal)r.NextDouble(),
                Tax = r.Next(0, 100) > 10 ? (decimal)Math.Round(r.NextDouble() * 20000, 2) : null,
                ShippingCost = r.Next(0, 2000) + (decimal)r.NextDouble(),
                Customer = $"{FirstNames[r.Next(FirstNames.Length)]} {LastNames[r.Next(LastNames.Length)]}",
                ItemCount = r.Next(1, 50),
                Status = statuses[r.Next(statuses.Length)],
                IsPaid = r.Next(0, 100) > 15,
                PaymentMethod = payments[r.Next(payments.Length)],
                Weight = r.Next(0, 100) > 30 ? r.NextDouble() * 100 : null
            });
        return list;
    }

    public static List<BigEmployee> GenerateBigEmployees(int count = 10000, int seed = 42)
    {
        var r = new Random(seed);
        var levels = Enum.GetValues<ExperienceLevel>();
        var list = new List<BigEmployee>(count);
        var names = new[] { "Александр", "Дмитрий", "Максим", "Сергей", "Андрей", "Алексей", "Елена", "Ольга", "Наталья", "Ирина" };
        var surnames = new[] { "Кузнецов", "Попов", "Васильев", "Зайцев", "Павлов", "Семенов", "Голубев", "Виноградов", "Белов", "Федоров" };
        for (int i = 1; i <= count; i++)
        {
            var fn = names[r.Next(names.Length)];
            var ln = surnames[r.Next(surnames.Length)];
            list.Add(new BigEmployee
            {
                Id = i,
                EmployeeCode = $"EMP-{i:D6}",
                GlobalId = Guid.NewGuid(),
                FirstName = fn,
                LastName = ln,
                MiddleName = r.Next(0, 100) > 30 ? names[r.Next(names.Length)] + "ович" : null,
                Email = $"{fn.ToLowerInvariant()}.{ln.ToLowerInvariant()}@company.com",
                Phone = r.Next(0, 100) > 20 ? $"+7 ({r.Next(900, 999)}) {r.Next(100, 999)}-{r.Next(10, 99)}-{r.Next(10, 99)}" : null,
                Department = Departments[r.Next(Departments.Length)],
                Position = Positions[r.Next(Positions.Length)],
                Level = levels[r.Next(levels.Length)],
                Salary = r.Next(50000, 250000),
                Bonus = r.Next(0, 100) > 40 ? (decimal)r.Next(10000, 80000) : null,
                TaxRate = Math.Round(r.NextDouble() * 0.3 + 0.05, 2),
                HireDate = DateTime.Now.AddDays(-r.Next(1, 5000)),
                LastReviewDate = r.Next(0, 100) > 20 ? DateTime.Now.AddDays(-r.Next(0, 365)) : null,
                BirthDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-r.Next(20, 60)).AddDays(-r.Next(0, 365))),
                ShiftStart = TimeOnly.FromTimeSpan(TimeSpan.FromHours(r.Next(6, 10))),
                IsActive = r.Next(0, 100) > 8,
                IsManager = r.Next(0, 100) > 80,
                YearsOfExperience = r.Next(0, 100) > 10 ? r.Next(1, 35) : null,
                ProjectsCount = r.Next(1, 30),
                Rating = Math.Round(r.NextDouble() * 5, 1),
                Efficiency = (float)Math.Round(r.NextDouble() * 0.5 + 0.5, 2),
                VacationDaysRemaining = (short)r.Next(0, 28),
                SickDaysThisYear = (byte)r.Next(0, 15),
                LastLogin = new DateTimeOffset(DateTime.Now.AddDays(-r.Next(0, 30)).AddHours(-r.Next(0, 12)).Ticks, TimeSpan.FromHours(r.Next(2, 5))),
                SessionDuration = TimeSpan.FromMinutes(r.Next(15, 480)),
                Notes = r.Next(0, 100) > 60 ? $"Performance review notes for {fn} {ln}. Overall rating: {Math.Round(r.NextDouble() * 5, 1)}/5. Areas for improvement: communication, leadership." : null,
                Skills = r.GetItems(SkillNames, r.Next(2, 6))
            });
        }
        return list;
    }

    public static List<Project> GenerateProjects(int count = 500, int seed = 42)
    {
        var r = new Random(seed);
        var priorities = Enum.GetValues<ProjectPriority>();
        var statuses = Enum.GetValues<ProjectStatus>();
        var projectNames = new[] { "CRM Platform", "Mobile App", "Analytics Dashboard", "E-Commerce Engine", "AI Assistant", "Data Pipeline", "Cloud Migration", "DevOps Toolchain", "Security Audit", "Customer Portal" };
        var list = new List<Project>(count);
        for (int i = 0; i < count; i++)
        {
            var start = DateOnly.FromDateTime(DateTime.Now.AddDays(-r.Next(30, 730)));
            var end = r.Next(0, 100) > 30 ? start.AddDays(r.Next(30, 365)) : (DateOnly?)null;
            list.Add(new Project
            {
                Id = Guid.NewGuid(),
                Name = projectNames[r.Next(projectNames.Length)] + $" #{i + 1}",
                Department = Departments[r.Next(Departments.Length)],
                StartDate = start,
                EndDate = end,
                Budget = r.Next(100000, 10000000),
                Spent = r.Next(0, 100) > 10 ? (decimal)r.Next(50000, 8000000) : null,
                TeamSize = r.Next(3, 30),
                Priority = priorities[r.Next(priorities.Length)],
                Status = statuses[r.Next(statuses.Length)],
                IsActive = r.Next(0, 100) > 30,
                LastUpdated = new DateTimeOffset(DateTime.Now.AddDays(-r.Next(0, 60)).Ticks, TimeSpan.FromHours(r.Next(2, 5))),
                Duration = end.HasValue ? end.Value.ToDateTime(default) - start.ToDateTime(default) : TimeSpan.FromDays(r.Next(30, 365))
            });
        }
        return list;
    }

    public static List<FinancialTransaction> GenerateTransactions(int count = 2000, int seed = 42)
    {
        var r = new Random(seed);
        var currencies = Enum.GetValues<Currency>();
        var types = Enum.GetValues<TxType>();
        var list = new List<FinancialTransaction>(count);
        for (int i = 0; i < count; i++)
        {
            var amount = (decimal)(r.NextDouble() * 100000 - 50000);
            var ts = new DateTimeOffset(DateTime.Now.AddDays(-r.Next(0, 180)).AddHours(-r.Next(0, 12)).AddMinutes(-r.Next(0, 60)).Ticks, TimeSpan.FromHours(r.Next(2, 5)));
            list.Add(new FinancialTransaction
            {
                Id = 1000000 + i,
                TransactionId = Guid.NewGuid(),
                AccountId = $"ACC-{r.Next(1000, 9999)}",
                Amount = Math.Round(amount, 2),
                Balance = Math.Round((decimal)(r.NextDouble() * 500000), 2),
                Currency = currencies[r.Next(currencies.Length)],
                Type = types[r.Next(types.Length)],
                Description = new[] { "Payment to vendor", "Salary transfer", "Invoice payment", "Refund", "Fee deduction", "Interest payment", "Transfer between accounts" }[r.Next(7)],
                Timestamp = ts,
                ValueDate = DateOnly.FromDateTime(ts.DateTime),
                IsReconciled = r.Next(0, 100) > 25,
                Fee = r.Next(0, 100) > 50 ? (decimal)Math.Round(r.NextDouble() * 500, 2) : null,
                ExchangeRate = r.Next(0, 100) > 60 ? Math.Round(r.NextDouble() * 100 + 0.5, 4) : null
            });
        }
        return list;
    }

    public static List<SensorReading> GenerateSensorReadings(int count = 5000, int seed = 42)
    {
        var r = new Random(seed);
        var types = Enum.GetValues<SensorType>();
        var statuses = Enum.GetValues<SensorStatus>();
        var list = new List<SensorReading>(count);
        for (int i = 0; i < count; i++)
        {
            var st = types[r.Next(types.Length)];
            var (val, unit, minT, maxT) = st switch
            {
                SensorType.Temperature => (r.NextDouble() * 60 - 10, "°C", -20f, 60f),
                SensorType.Humidity => (r.NextDouble() * 100, "%", 0f, 100f),
                SensorType.Pressure => (r.NextDouble() * 200 + 900, "hPa", 800f, 1100f),
                SensorType.Light => (r.NextDouble() * 10000, "lux", 0f, 15000f),
                SensorType.Motion => (r.NextDouble() * 100, "%", 0f, 100f),
                _ => (r.NextDouble() * 500, "ppm", 0f, 1000f)
            };
            var ts = DateTime.Now.AddMinutes(-r.Next(0, 10080));
            list.Add(new SensorReading
            {
                Id = 800000 + i,
                DeviceId = $"SENSOR-{st.ToString().ToUpper()}-{r.Next(1, 999):D3}",
                SensorType = st,
                Timestamp = ts,
                Value = Math.Round(val, 2),
                MinThreshold = minT,
                MaxThreshold = maxT,
                Unit = unit,
                BatteryLevel = (byte)r.Next(10, 100),
                SignalStrength = (short)-r.Next(30, 90),
                IsAlert = val < minT || val > maxT,
                Latitude = Math.Round(55.0 + r.NextDouble() * 10, 4),
                Longitude = Math.Round(37.0 + r.NextDouble() * 10, 4),
                Status = val < minT || val > maxT ? SensorStatus.Alert : r.Next(0, 100) > 90 ? SensorStatus.Warning : SensorStatus.Normal,
                LastMaintenance = r.Next(0, 100) > 40 ? DateTime.Now.AddDays(-r.Next(0, 90)) : null
            });
        }
        return list;
    }

    public static List<Student> GenerateStudents(int count = 500, int seed = 42)
    {
        var r = new Random(seed);
        var majors = Enum.GetValues<Major>();
        var years = Enum.GetValues<YearOfStudy>();
        var list = new List<Student>(count);
        for (int i = 1; i <= count; i++)
        {
            var fn = EnFirstNames[r.Next(EnFirstNames.Length)];
            var ln = EnLastNames[r.Next(EnLastNames.Length)];
            var enroll = DateTime.Now.AddYears(-r.Next(1, 6)).AddDays(-r.Next(0, 180));
            list.Add(new Student
            {
                Id = i,
                FirstName = fn,
                LastName = ln,
                Email = $"{fn.ToLowerInvariant()}.{ln.ToLowerInvariant()}@university.edu",
                BirthDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-r.Next(17, 30)).AddDays(-r.Next(0, 365))),
                EnrollmentDate = enroll,
                GPA = Math.Round(r.NextDouble() * 4, 2),
                CreditsCompleted = r.Next(0, 150),
                IsActive = r.Next(0, 100) > 10,
                Major = majors[r.Next(majors.Length)],
                Year = years[r.Next(years.Length)],
                Scholarship = r.Next(0, 100) > 60 ? (decimal)(r.NextDouble() * 100000) : null,
                HasGraduated = r.Next(0, 100) > 85
            });
        }
        return list;
    }

    public static List<EmployeeDetailed> GenerateDetailedEmployees(int count = 100, int seed = 42)
    {
        var r = new Random(seed);
        var cities = new[] { "Москва", "Санкт-Петербург", "Новосибирск", "Екатеринбург", "Казань" };
        var streets = new[] { "Ленина", "Пушкина", "Гагарина", "Мира", "Советская" };
        var projNames = new[] { "CRM System", "Mobile App", "Analytics Platform", "E-commerce", "AI Assistant" };
        var roles = new[] { "Developer", "Lead", "Architect", "QA", "Designer" };
        var skillLevels = new[] { "Junior", "Middle", "Senior", "Expert" };
        var list = new List<EmployeeDetailed>(count);
        for (int i = 1; i <= count; i++)
        {
            var emp = new EmployeeDetailed
            {
                Id = i,
                FirstName = FirstNames[r.Next(FirstNames.Length)],
                LastName = LastNames[r.Next(LastNames.Length)],
                Department = Departments[r.Next(Departments.Length)],
                Salary = r.Next(50000, 200000),
                IsActive = r.Next(0, 100) > 15,
                Address = new Address
                {
                    Country = "Россия",
                    City = cities[r.Next(cities.Length)],
                    Street = $"ул. {streets[r.Next(streets.Length)]}, д. {r.Next(1, 150)}",
                    PostalCode = $"{r.Next(100000, 999999)}"
                }
            };
            int pc = r.Next(1, 5);
            for (int p = 0; p < pc; p++)
                emp.Projects.Add(new EmployeeProject { Id = p + 1, Name = projNames[r.Next(projNames.Length)], Role = roles[r.Next(roles.Length)], StartDate = DateTime.Now.AddMonths(-r.Next(1, 36)), HoursPerWeek = r.Next(10, 40) });
            int sc = r.Next(2, 7);
            var used = new HashSet<string>();
            for (int s = 0; s < sc; s++)
            {
                var sk = SkillNames[r.Next(SkillNames.Length)];
                if (used.Add(sk)) emp.Skills.Add(new EmployeeSkill { Name = sk, Level = skillLevels[r.Next(skillLevels.Length)], YearsOfExperience = r.Next(1, 10) });
            }
            list.Add(emp);
        }
        return list;
    }
}
