using System;

namespace TestApp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== .NET Debug MCP Test Application ===");
        Console.WriteLine("This application contains intentional bugs for testing the debugger.\n");

        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "null":
                    TestNullReference();
                    break;
                case "divide":
                    TestDivideByZero();
                    break;
                case "index":
                    TestIndexOutOfRange();
                    break;
                case "logic":
                    TestLogicError();
                    break;
                case "loop":
                    TestInfiniteLoop();
                    break;
                case "all":
                    RunAllTests();
                    break;
                default:
                    ShowHelp();
                    break;
            }
        }
        else
        {
            ShowHelp();
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("Usage: TestApp [test-name]");
        Console.WriteLine("\nAvailable tests:");
        Console.WriteLine("  null     - Null reference exception");
        Console.WriteLine("  divide   - Division by zero");
        Console.WriteLine("  index    - Index out of range");
        Console.WriteLine("  logic    - Logic error (off-by-one)");
        Console.WriteLine("  loop     - Infinite loop");
        Console.WriteLine("  all      - Run all tests (non-infinite ones)");
    }

    static void TestNullReference()
    {
        Console.WriteLine("\n[TEST] Null Reference Exception");
        var processor = new DataProcessor();

        // This will throw NullReferenceException
        processor.ProcessData(null);
    }

    static void TestDivideByZero()
    {
        Console.WriteLine("\n[TEST] Division by Zero");
        var calculator = new Calculator();

        // This will throw DivideByZeroException
        int result = calculator.Divide(10, 0);
        Console.WriteLine($"Result: {result}");
    }

    static void TestIndexOutOfRange()
    {
        Console.WriteLine("\n[TEST] Index Out of Range");
        var calculator = new Calculator();
        int[] numbers = { 1, 2, 3, 4, 5 };

        // This will throw IndexOutOfRangeException
        int element = calculator.GetElement(numbers, 10);
        Console.WriteLine($"Element: {element}");
    }

    static void TestLogicError()
    {
        Console.WriteLine("\n[TEST] Logic Error (Off-by-One)");
        var calculator = new Calculator();
        int[] numbers = { 1, 2, 3, 4, 5 };

        int sum = calculator.CalculateSum(numbers);
        Console.WriteLine($"Sum: {sum}");
        Console.WriteLine($"Expected: 15, Got: {sum}");

        if (sum != 15)
        {
            Console.WriteLine("ERROR: Sum is incorrect!");
        }
    }

    static void TestInfiniteLoop()
    {
        Console.WriteLine("\n[TEST] Infinite Loop");
        Console.WriteLine("WARNING: This will run forever. Use debugger to break!");

        var processor = new DataProcessor();
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // This will loop forever
        processor.ProcessItems(items);
    }

    static void RunAllTests()
    {
        Console.WriteLine("\n=== Running All Tests ===\n");

        try
        {
            TestLogicError();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED: {ex.Message}");
        }

        try
        {
            TestDivideByZero();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED: {ex.Message}");
        }

        try
        {
            TestIndexOutOfRange();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED: {ex.Message}");
        }

        try
        {
            TestNullReference();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAILED: {ex.Message}");
        }

        Console.WriteLine("\n=== Test Run Complete ===");
    }
}
