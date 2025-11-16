namespace TestApp;

/// <summary>
/// Calculator class with intentional bugs for debugging practice
/// </summary>
public class Calculator
{
    /// <summary>
    /// BUG: Doesn't check for division by zero
    /// </summary>
    public int Divide(int a, int b)
    {
        // Missing validation: if (b == 0) throw new ArgumentException("Cannot divide by zero");
        return a / b; // DivideByZeroException when b = 0
    }

    /// <summary>
    /// BUG: Doesn't validate array index
    /// </summary>
    public int GetElement(int[] array, int index)
    {
        // Missing validation: if (index < 0 || index >= array.Length)
        return array[index]; // IndexOutOfRangeException when index is invalid
    }

    /// <summary>
    /// BUG: Off-by-one error - starts at index 1 instead of 0
    /// </summary>
    public int CalculateSum(int[] numbers)
    {
        int sum = 0;

        // BUG: Should start at i = 0, not i = 1
        for (int i = 1; i < numbers.Length; i++)
        {
            sum += numbers[i];
        }

        return sum; // Will miss the first element!
    }

    /// <summary>
    /// Correct implementation for comparison
    /// </summary>
    public int CalculateSumCorrect(int[] numbers)
    {
        int sum = 0;

        for (int i = 0; i < numbers.Length; i++)
        {
            sum += numbers[i];
        }

        return sum;
    }

    /// <summary>
    /// BUG: Uses multiplication instead of addition
    /// </summary>
    public int Add(int a, int b)
    {
        // BUG: Should be a + b
        return a * b; // Logic error - wrong operation
    }

    /// <summary>
    /// BUG: Complex calculation with subtle error
    /// </summary>
    public double CalculateAverage(int[] numbers)
    {
        if (numbers == null || numbers.Length == 0)
        {
            return 0;
        }

        int sum = 0;
        for (int i = 0; i < numbers.Length; i++)
        {
            sum += numbers[i];
        }

        // BUG: Integer division instead of floating-point division
        return sum / numbers.Length; // Should be: (double)sum / numbers.Length
    }
}
