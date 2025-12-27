# Judge0 Compatibility Notes

## Overview
All mock interview coding challenges in the Vector platform are designed to be compatible with Judge0 API format.

## Test Case Format

### Storage Format
Test cases are stored in the database with the following format:
- **Input**: Plain string (used as `stdin` in Judge0)
- **ExpectedOutput**: Plain string (used as `expected_output` in Judge0)

### Example Test Case
```json
{
  "testCaseNumber": 1,
  "input": "[2, 7, 11, 15]\n9",
  "expectedOutput": "[0, 1]",
  "isHidden": false,
  "explanation": "The sum of nums[0] + nums[1] = 2 + 7 = 9"
}
```

### Judge0 Submission Format
When executing code, test cases are converted to Judge0 format:
```json
{
  "source_code": "def twoSum(nums, target): ...",
  "language_id": 71,  // Python
  "stdin": "[2, 7, 11, 15]\n9",  // From test case Input
  "expected_output": "[0, 1]",    // From test case ExpectedOutput
  "cpu_time_limit": 5,
  "memory_limit": 128000
}
```

## Supported Languages

All supported languages map to Judge0 language IDs:
- **JavaScript**: 63 (Node.js 12.14.0)
- **Python**: 71 (Python 3.8.1)
- **Java**: 62 (OpenJDK 13.0.1)
- **C++**: 54 (GCC 9.2.0)
- **C#**: 51 (Mono 6.6.0.161)
- **Go**: 60 (Go 1.13.5)

## Test Case Input Format Guidelines

### For Array Inputs
```
[1, 2, 3]
7
```
- First line: Array as string
- Second line: Target value

### For Multiple Parameters
```
5
[1, 2, 3, 4, 5]
```
- Each parameter on a new line

### For String Inputs
```
"hello world"
```
- Plain string format

## Expected Output Format

### For Array Outputs
```
[0, 1]
```
- Plain array format as string

### For Boolean Outputs
```
true
```
or
```
false
```

### For Number Outputs
```
42
```

## Validation

The `QuestionService` validates test cases to ensure:
1. Input is not empty
2. ExpectedOutput is not empty
3. Test case number is greater than 0
4. Format is compatible with Judge0 (plain strings, not JSON)

## Implementation Status

✅ **Backend**: Test case format validated and stored as plain strings
✅ **Frontend**: AddQuestionPage includes Judge0 format instructions
✅ **Database**: QuestionTestCase model stores Input and ExpectedOutput as strings
✅ **API**: Test cases are returned in Judge0-compatible format

## Next Steps

When implementing code execution (Week 2, Day 7-8):
1. Use test case `Input` directly as `stdin` in Judge0 submission
2. Use test case `ExpectedOutput` directly as `expected_output` in Judge0 submission
3. No format conversion needed - already in correct format

