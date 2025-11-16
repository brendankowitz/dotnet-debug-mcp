# Example Debugging Session

This document shows an example of how an LLM (like Claude) would use the .NET Debug MCP server to debug a buggy application.

## Scenario: Off-by-One Bug

The TestApp has a bug in the `CalculateSum` function - it starts the loop at index 1 instead of 0, causing it to miss the first element.

### Expected Behavior
```
Sum of [1, 2, 3, 4, 5] = 15
```

### Actual Behavior
```
Sum of [1, 2, 3, 4, 5] = 14  ❌
```

---

## Debugging Session

### User Request
> "Debug the TestApp logic error and find why the sum is incorrect"

### LLM Debugging Workflow

#### Step 1: Start Debugging Session

**Tool:** `debug_start`
```json
{
  "program": "/path/to/TestApp.dll",
  "args": ["logic"]
}
```

**Response:**
```json
{
  "success": true,
  "sessionId": "default",
  "program": "/path/to/TestApp.dll",
  "message": "Debug session 'default' started successfully"
}
```

#### Step 2: Set Breakpoint at Suspicious Location

The LLM analyzes the code and sets a breakpoint in the `CalculateSum` method:

**Tool:** `debug_set_breakpoint`
```json
{
  "file": "/path/to/Calculator.cs",
  "line": 29
}
```

**Response:**
```json
{
  "success": true,
  "breakpoint": {
    "id": 1,
    "verified": true,
    "line": 29,
    "file": "/path/to/Calculator.cs"
  },
  "message": "Breakpoint set at Calculator.cs:29"
}
```

#### Step 3: Continue to Breakpoint

**Tool:** `debug_continue`
```json
{}
```

**Response:**
```json
{
  "success": true,
  "status": "Stopped: breakpoint (thread 1)",
  "isStopped": true,
  "hasExited": false,
  "stopReason": "breakpoint",
  "threadId": 1
}
```

#### Step 4: Get Stack Trace

**Tool:** `debug_get_stack_trace`
```json
{}
```

**Response:**
```json
{
  "success": true,
  "threadId": 1,
  "stopReason": "breakpoint",
  "frameCount": 3,
  "frames": [
    {
      "id": 1000,
      "index": 0,
      "name": "Calculator.CalculateSum",
      "file": "/path/to/Calculator.cs",
      "fileName": "Calculator.cs",
      "line": 29,
      "column": 13
    },
    {
      "id": 1001,
      "index": 1,
      "name": "Program.TestLogicError",
      "file": "/path/to/Program.cs",
      "fileName": "Program.cs",
      "line": 93,
      "column": 20
    },
    {
      "id": 1002,
      "index": 2,
      "name": "Program.Main",
      "file": "/path/to/Program.cs",
      "fileName": "Program.cs",
      "line": 26,
      "column": 21
    }
  ],
  "callStackSummary": "Calculator.CalculateSum -> Program.TestLogicError -> Program.Main"
}
```

#### Step 5: Inspect Variables

**Tool:** `debug_get_variables`
```json
{
  "scope": "locals"
}
```

**Response:**
```json
{
  "success": true,
  "frameId": 0,
  "frameLocation": "Calculator.CalculateSum at Calculator.cs:29",
  "scopeFilter": "locals",
  "variableCount": 3,
  "variables": [
    {
      "name": "numbers",
      "value": "{int[5]}",
      "type": "int[]",
      "scope": "Locals"
    },
    {
      "name": "sum",
      "value": "0",
      "type": "int",
      "scope": "Locals"
    },
    {
      "name": "i",
      "value": "1",
      "type": "int",
      "scope": "Locals"
    }
  ]
}
```

**🔍 LLM Analysis:** The variable `i` is `1` at the start of the loop - this is suspicious! It should be `0` to include the first element.

#### Step 6: Evaluate Expression to Verify

**Tool:** `debug_evaluate`
```json
{
  "expression": "i"
}
```

**Response:**
```json
{
  "success": true,
  "expression": "i",
  "result": "1",
  "type": "int",
  "frameId": 0,
  "frameLocation": "Calculator.CalculateSum at Calculator.cs:29"
}
```

**Tool:** `debug_evaluate`
```json
{
  "expression": "numbers[0]"
}
```

**Response:**
```json
{
  "success": true,
  "expression": "numbers[0]",
  "result": "1",
  "type": "int",
  "frameId": 0,
  "frameLocation": "Calculator.CalculateSum at Calculator.cs:29"
}
```

**🔍 LLM Analysis:** Confirmed! The loop starts at `i = 1`, missing `numbers[0]` which has value `1`. This explains why the sum is 14 instead of 15.

#### Step 7: Step Through to Confirm

**Tool:** `debug_step_over`
```json
{}
```

**Tool:** `debug_get_variables` (after step)
```json
{
  "scope": "locals"
}
```

**Response:**
```json
{
  "variables": [
    {
      "name": "i",
      "value": "1",
      "type": "int"
    },
    {
      "name": "sum",
      "value": "2",
      "type": "int"
    }
  ]
}
```

**🔍 LLM Analysis:** After first iteration, `sum = 2` (which is `numbers[1]`). The first element `numbers[0] = 1` was never added!

#### Step 8: Stop Debugging

**Tool:** `debug_stop`
```json
{}
```

**Response:**
```json
{
  "success": true,
  "sessionId": "default",
  "message": "Debug session 'default' stopped successfully"
}
```

---

## LLM Diagnosis

### Root Cause Found

**File:** `Calculator.cs`
**Line:** 29
**Bug:** Off-by-one error in loop initialization

**Current Code (Buggy):**
```csharp
for (int i = 1; i < numbers.Length; i++)  // ❌ Starts at 1
{
    sum += numbers[i];
}
```

**Fixed Code:**
```csharp
for (int i = 0; i < numbers.Length; i++)  // ✅ Starts at 0
{
    sum += numbers[i];
}
```

### Evidence

1. **Breakpoint Analysis:** Stopped at first iteration with `i = 1`
2. **Variable Inspection:** `i` starts at 1 instead of 0
3. **Expression Evaluation:** `numbers[0] = 1` is never included in sum
4. **Step Verification:** First iteration adds `numbers[1]` (2) instead of `numbers[0]` (1)

### Result

The bug causes the function to skip the first element of the array, resulting in:
- Expected: `1 + 2 + 3 + 4 + 5 = 15`
- Actual: `2 + 3 + 4 + 5 = 14` ❌

### Proposed Fix

Change line 29 in `Calculator.cs`:
```diff
- for (int i = 1; i < numbers.Length; i++)
+ for (int i = 0; i < numbers.Length; i++)
```

---

## Tools Used

1. ✅ `debug_start` - Started debugging session
2. ✅ `debug_set_breakpoint` - Set strategic breakpoint
3. ✅ `debug_continue` - Ran to breakpoint
4. ✅ `debug_get_stack_trace` - Analyzed call stack
5. ✅ `debug_get_variables` - Inspected local variables
6. ✅ `debug_evaluate` - Verified hypotheses
7. ✅ `debug_step_over` - Stepped through execution
8. ✅ `debug_stop` - Cleaned up session

---

## Benefits of LLM-Driven Debugging

1. **Automated Root Cause Analysis** - LLM identifies the exact line and issue
2. **Evidence-Based Diagnosis** - Uses actual runtime state to confirm hypotheses
3. **Step-by-Step Verification** - Methodically verifies each assumption
4. **Complete Fix Proposal** - Provides exact code change needed
5. **No Human Intervention** - Entire debugging process is autonomous

---

## Next Steps

After the LLM identifies the bug, it can:

1. **Apply the fix** to the source code
2. **Verify the fix** by re-running the test
3. **Create a test case** to prevent regression
4. **Document the bug** for future reference
5. **Commit the changes** with detailed explanation

This demonstrates the power of combining LLM reasoning with programmatic debugging capabilities!
