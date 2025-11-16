namespace TestApp;

/// <summary>
/// Data processor with null reference bugs
/// </summary>
public class DataProcessor
{
    /// <summary>
    /// BUG: Doesn't check for null input
    /// </summary>
    public void ProcessData(string? input)
    {
        // Missing validation: if (input == null) throw new ArgumentNullException(nameof(input));

        Console.WriteLine($"Processing: {input}");

        // BUG: Will throw NullReferenceException if input is null
        var length = input.Length;
        Console.WriteLine($"Length: {length}");

        // BUG: Will throw NullReferenceException if input is null
        var upper = input.ToUpper();
        Console.WriteLine($"Uppercase: {upper}");
    }

    /// <summary>
    /// BUG: Infinite loop - forgot to increment counter
    /// </summary>
    public void ProcessItems(List<int> items)
    {
        if (items == null || items.Count == 0)
        {
            return;
        }

        int i = 0;
        while (i < items.Count)
        {
            Console.WriteLine($"Processing item: {items[i]}");

            // BUG: Forgot to increment i!
            // i++;  // <-- This line is missing!
        }

        Console.WriteLine("Done processing items");
    }

    /// <summary>
    /// BUG: Null reference in property access
    /// </summary>
    public string GetUserDisplayName(User? user)
    {
        // BUG: Doesn't check if user is null
        return $"{user.FirstName} {user.LastName}"; // NullReferenceException if user is null
    }

    /// <summary>
    /// BUG: Null reference in nested property access
    /// </summary>
    public string GetUserCity(User? user)
    {
        // BUG: Doesn't check if user or user.Address is null
        return user.Address.City; // NullReferenceException if user or Address is null
    }

    /// <summary>
    /// Correct implementation for comparison
    /// </summary>
    public void ProcessDataCorrect(string? input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input), "Input cannot be null");
        }

        Console.WriteLine($"Processing: {input}");
        var length = input.Length;
        Console.WriteLine($"Length: {length}");

        var upper = input.ToUpper();
        Console.WriteLine($"Uppercase: {upper}");
    }
}

/// <summary>
/// User model for testing null reference scenarios
/// </summary>
public class User
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Address? Address { get; set; }
}

/// <summary>
/// Address model for testing null reference scenarios
/// </summary>
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}
