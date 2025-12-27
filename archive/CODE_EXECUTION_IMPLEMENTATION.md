# Code Execution Service Implementation - Day 7-8

## Overview

Implemented code execution service using Judge0 API for running and validating user-submitted code in multiple programming languages.

## Implementation Summary

### Backend Implementation ✅

1. **CodeExecutionService** (`backend/Vector.Api/Services/CodeExecutionService.cs`)
   - Judge0 API integration
   - Support for 6 languages: Python, JavaScript, Java, C++, C#, Go
   - Language ID mapping for Judge0
   - Asynchronous code execution with polling
   - Timeout handling (5 seconds CPU, 10 seconds wall time)
   - Memory limits (128 MB)
   - Security sandboxing via Judge0

2. **CodeExecutionController** (Already existed, now functional)
   - `POST /api/codeexecution/execute` - Execute code with optional input
   - `POST /api/codeexecution/validate/{questionId}` - Validate code against test cases
   - `GET /api/codeexecution/languages` - Get supported languages

3. **Configuration**
   - Added `Judge0:BaseUrl` to `appsettings.json`
   - Registered service in `Program.cs` with HttpClient factory
   - Added Judge0 and RabbitMQ services to `docker-compose.yml`

### Frontend Implementation ✅

1. **CodeExecutionService** (`frontend/src/services/codeExecution.service.ts`)
   - `executeCode()` method - Execute code with input
   - `validateSolution()` method - Validate against test cases
   - `getSupportedLanguages()` method - Get language list

2. **QuestionDetailPage Updates**
   - `handleRunCode()` - Executes code with first test case input
   - `handleSubmit()` - Validates code against all test cases
   - Results displayed in test result tab
   - Execution metrics (runtime, memory) shown

### Docker Configuration ✅

1. **Judge0 Service**
   - Image: `judge0/judge0:1.13.0`
   - Port: `2358`
   - Uses shared PostgreSQL (separate `judge0` database)
   - Uses shared Redis
   - Requires RabbitMQ for queue management

2. **RabbitMQ Service**
   - Image: `rabbitmq:3.12-management-alpine`
   - Ports: `5672` (AMQP), `15672` (Management UI)
   - Credentials: `judge0/judge0`

3. **Database Setup**
   - Created `docker/init-judge0-db.sql` for database initialization
   - Judge0 uses separate database on same PostgreSQL instance

## Supported Languages

| Language | Judge0 ID | Version |
|----------|-----------|---------|
| Python | 92 | 3.11.1 |
| JavaScript (Node.js) | 93 | 18.15.0 |
| Java | 91 | 17.0.2 |
| C++ | 54 | GCC 9.2.0 |
| C# | 51 | .NET 6.0.102 |
| Go | 60 | 1.19.5 |

## Security Features

- **Timeout**: 5 seconds CPU time, 10 seconds wall time
- **Memory Limit**: 128 MB per execution
- **File Size Limit**: 1 MB
- **Process Limit**: 60 processes/threads
- **Sandboxing**: Provided by Judge0 (isolated containers)
- **Network Restrictions**: No network access (via Judge0)
- **File System Restrictions**: Limited access (via Judge0)

## API Endpoints

### Execute Code
```
POST /api/codeexecution/execute
Authorization: Bearer <token>
Content-Type: application/json

{
  "sourceCode": "print('Hello, World!')",
  "language": "python",
  "stdin": "optional input"
}
```

**Response:**
```json
{
  "output": "Hello, World!",
  "error": null,
  "status": "Accepted",
  "runtime": 0.05,
  "memory": 10240,
  "compileOutput": null
}
```

### Validate Solution
```
POST /api/codeexecution/validate/{questionId}
Authorization: Bearer <token>
Content-Type: application/json

{
  "sourceCode": "def solution(): ...",
  "language": "python"
}
```

**Response:**
```json
[
  {
    "testCaseId": "guid",
    "testCaseNumber": 1,
    "passed": true,
    "output": "expected output",
    "error": null,
    "runtime": 0.05,
    "memory": 10240,
    "status": "Accepted"
  }
]
```

### Get Supported Languages
```
GET /api/codeexecution/languages
Authorization: Bearer <token>
```

**Response:**
```json
[
  {
    "name": "Python 3",
    "value": "python",
    "judge0LanguageId": 92,
    "version": "3.11.1"
  }
]
```

## Status Codes

Judge0 execution status codes mapped to user-friendly messages:
- `3` = Accepted
- `4` = Wrong Answer
- `5` = Time Limit Exceeded
- `6` = Compilation Error
- `7-12` = Runtime Errors (various)
- `13` = Internal Error
- `14` = Exec Format Error

## Setup Instructions

### 1. Create Judge0 Database

The `judge0` database needs to be created in PostgreSQL. Run:

```bash
docker exec -it vector-postgres psql -U postgres -c "CREATE DATABASE judge0;"
```

Or use the initialization script:
```bash
docker exec -i vector-postgres psql -U postgres < docker/init-judge0-db.sql
```

### 2. Start Services

```bash
cd docker
docker-compose up -d
```

This will start:
- PostgreSQL (with judge0 database)
- Redis
- RabbitMQ
- Judge0
- Backend
- Frontend

### 3. Verify Judge0 is Running

```bash
# Judge0 doesn't have a /health endpoint, but you can test with /languages
curl http://localhost:2358/languages
```

Should return a JSON array of supported languages. If you get a response, Judge0 is working correctly.

## Testing

### Manual Testing

1. **Test Code Execution:**
   - Navigate to a coding question
   - Write code in the editor
   - Click "Run" button
   - Check results in "Test Result" tab

2. **Test Code Validation:**
   - Navigate to a coding question
   - Write solution code
   - Click "Submit" button
   - Check test case results

### API Testing

```bash
# Get auth token first
TOKEN="your_jwt_token"

# Execute code
curl -X POST http://localhost:5000/api/codeexecution/execute \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceCode": "print(\"Hello, World!\")",
    "language": "python"
  }'

# Get supported languages
curl http://localhost:5000/api/codeexecution/languages \
  -H "Authorization: Bearer $TOKEN"
```

## Known Limitations

1. **Judge0 Database**: Must be created manually on first setup
2. **Polling**: Uses polling mechanism (500ms intervals) - could be optimized with webhooks
3. **Timeout**: 30-second maximum wait time for execution results
4. **Language Support**: Limited to 6 languages (can be extended)

## Next Steps

- [ ] Add unit tests for CodeExecutionService
- [ ] Add integration tests for code execution endpoints
- [ ] Optimize polling with webhooks (if Judge0 supports)
- [ ] Add more languages (Ruby, Rust, etc.)
- [ ] Add code execution history/analytics
- [ ] Implement rate limiting for code execution

## Files Modified/Created

### Backend
- ✅ `backend/Vector.Api/Services/CodeExecutionService.cs` (created)
- ✅ `backend/Vector.Api/Program.cs` (updated - service registration)
- ✅ `backend/Vector.Api/appsettings.json` (updated - Judge0 config)

### Frontend
- ✅ `frontend/src/services/codeExecution.service.ts` (updated - actual implementation)
- ✅ `frontend/src/pages/questions/QuestionDetailPage.tsx` (updated - integrated execution)

### Docker
- ✅ `docker/docker-compose.yml` (updated - added Judge0 and RabbitMQ)
- ✅ `docker/init-judge0-db.sql` (created - database init script)
- ✅ `docker/init-judge0-db.sh` (created - alternative init script)

### Documentation
- ✅ `STAGE2_IMPLEMENTATION.md` (updated - marked Day 7-8 as complete)

