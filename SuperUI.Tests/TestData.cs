namespace SuperUI.Tests;

public sealed record Person(int Id, string Name, string Dept, decimal Salary);

public static class TestData
{
    public static List<Person> People() => new()
    {
        new(1, "Alice",  "IT",     100_000m),
        new(2, "Bob",    "HR",      80_000m),
        new(3, "Carol",  "IT",     120_000m),
        new(4, "Dave",   "Sales",   90_000m),
        new(5, "Eve",    "IT",     110_000m),
    };
}
