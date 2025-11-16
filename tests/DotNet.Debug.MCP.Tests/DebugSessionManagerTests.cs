using DotNet.Debug.MCP.Services;
using Xunit;

namespace DotNet.Debug.MCP.Tests;

public class DebugSessionManagerTests
{
    [Fact]
    public void DebugSession_InitializesWithCorrectDefaults()
    {
        // Arrange & Act
        var session = new DebugSession
        {
            SessionId = "test",
            Program = "/path/to/app.dll"
        };

        // Assert
        Assert.Equal("test", session.SessionId);
        Assert.Equal("/path/to/app.dll", session.Program);
        Assert.False(session.IsLaunched);
        Assert.False(session.IsStopped);
        Assert.False(session.HasExited);
        Assert.True(session.IsHealthy() == false); // No client yet
    }

    [Fact]
    public void DebugSession_TracksActivityTimestamp()
    {
        // Arrange
        var session = new DebugSession();
        var initialTime = session.LastActivity;

        // Act
        Thread.Sleep(10);
        session.UpdateActivity();

        // Assert
        Assert.True(session.LastActivity > initialTime);
    }

    [Fact]
    public void DebugSession_CanAddAndRetrieveOutput()
    {
        // Arrange
        var session = new DebugSession();

        // Act
        session.AddOutput("stdout", "Line 1");
        session.AddOutput("stdout", "Line 2");
        session.AddOutput("stderr", "Error 1");

        // Assert
        var allOutput = session.GetOutput("all", 100);
        Assert.Equal(3, allOutput.Count);

        var stdoutOnly = session.GetOutput("stdout", 100);
        Assert.Equal(2, stdoutOnly.Count);

        var stderrOnly = session.GetOutput("stderr", 100);
        Assert.Single(stderrOnly);
    }

    [Fact]
    public void DebugSession_LimitsOutputTo1000Lines()
    {
        // Arrange
        var session = new DebugSession();

        // Act
        for (int i = 0; i < 1500; i++)
        {
            session.AddOutput("stdout", $"Line {i}");
        }

        // Assert
        var output = session.GetOutput("all", 2000);
        Assert.Equal(1000, output.Count);

        // Should have the last 1000 lines
        Assert.Contains("Line 1499", output.Last().Text);
    }

    [Fact]
    public void DebugSession_CanAddAndRetrieveBreakpoints()
    {
        // Arrange
        var session = new DebugSession();

        // Act
        session.AddBreakpoint(new TrackedBreakpoint
        {
            Id = 1,
            File = "/path/to/Program.cs",
            Line = 42,
            Verified = true,
            Condition = "x > 10"
        });

        session.AddBreakpoint(new TrackedBreakpoint
        {
            Id = 2,
            File = "/path/to/Calculator.cs",
            Line = 15,
            Verified = true
        });

        // Assert
        var breakpoints = session.GetBreakpoints();
        Assert.Equal(2, breakpoints.Count);
        Assert.Equal(42, breakpoints[0].Line);
        Assert.Equal("x > 10", breakpoints[0].Condition);
        Assert.Equal(15, breakpoints[1].Line);
    }

    [Fact]
    public void DebugSession_IsHealthy_ReturnsFalseWithoutClient()
    {
        // Arrange
        var session = new DebugSession();

        // Act & Assert
        Assert.False(session.IsHealthy());
    }

    [Fact]
    public void DebugSession_IsHealthy_ReturnsFalseAfterExit()
    {
        // Arrange
        var session = new DebugSession
        {
            HasExited = true
        };

        // Act & Assert
        Assert.False(session.IsHealthy());
    }
}
