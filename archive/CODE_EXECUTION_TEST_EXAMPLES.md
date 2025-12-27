# Code Execution Endpoint Test Examples

## Authentication Required
All endpoints require JWT authentication. First, get a token:

```bash
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "admin@vector.com",
  "password": "Admin123!"
}

Response:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "...",
  "user": { ... }
}
```

---

## 1. GET /api/codeexecution/languages

**Request:**
```http
GET http://localhost:5000/api/codeexecution/languages
Authorization: Bearer {token}
```

**Expected Response:**
```json
[
  {
    "name": "Python 3",
    "value": "python",
    "judge0LanguageId": 71,
    "version": "3.8.1"
  },
  {
    "name": "JavaScript (Node.js)",
    "value": "javascript",
    "judge0LanguageId": 63,
    "version": "12.14.0"
  },
  {
    "name": "Java",
    "value": "java",
    "judge0LanguageId": 62,
    "version": "OpenJDK 13.0.1"
  },
  {
    "name": "C++",
    "value": "cpp",
    "judge0LanguageId": 54,
    "version": "GCC 9.2.0"
  },
  {
    "name": "C#",
    "value": "csharp",
    "judge0LanguageId": 51,
    "version": "Mono 6.6.0.161"
  },
  {
    "name": "Go",
    "value": "go",
    "judge0LanguageId": 60,
    "version": "1.13.5"
  }
]
```

---

## 2. POST /api/codeexecution/execute

### Example 1: JavaScript - Simple Output

**Request:**
```http
POST http://localhost:5000/api/codeexecution/execute
Authorization: Bearer {token}
Content-Type: application/json

{
  "sourceCode": "console.log('Hello, World!');",
  "language": "javascript",
  "stdin": ""
}
```

**Expected Response:**
```json
{
  "status": "Accepted",
  "output": "Hello, World!\n",
  "error": "",
  "runtime": 52.5,
  "memory": 10240,
  "compileOutput": null
}
```

### Example 2: Python - Simple Output

**Request:**
```http
POST http://localhost:5000/api/codeexecution/execute
Authorization: Bearer {token}
Content-Type: application/json

{
  "sourceCode": "print('Hello from Python!')",
  "language": "python",
  "stdin": ""
}
```

**Expected Response:**
```json
{
  "status": "Accepted",
  "output": "Hello from Python!\n",
  "error": "",
  "runtime": 45.2,
  "memory": 8192,
  "compileOutput": null
}
```

### Example 3: JavaScript - With Input (stdin)

**Request:**
```http
POST http://localhost:5000/api/codeexecution/execute
Authorization: Bearer {token}
Content-Type: application/json

{
  "sourceCode": "const readline = require('readline');\nconst rl = readline.createInterface({ input: process.stdin });\nrl.on('line', (line) => { console.log('Input:', line); });",
  "language": "javascript",
  "stdin": "Test Input Data"
}
```

**Expected Response:**
```json
{
  "status": "Accepted",
  "output": "Input: Test Input Data\n",
  "error": "",
  "runtime": 78.3,
  "memory": 12288,
  "compileOutput": null
}
```

### Example 4: Error Case - Syntax Error

**Request:**
```http
POST http://localhost:5000/api/codeexecution/execute
Authorization: Bearer {token}
Content-Type: application/json

{
  "sourceCode": "console.log('Missing quote);",
  "language": "javascript",
  "stdin": ""
}
```

**Expected Response:**
```json
{
  "status": "Compilation Error",
  "output": "",
  "error": "SyntaxError: Invalid or unexpected token\n...",
  "runtime": 0,
  "memory": 0,
  "compileOutput": "SyntaxError: Invalid or unexpected token"
}
```

---

## 3. POST /api/codeexecution/validate/{questionId}

**Request:**
```http
POST http://localhost:5000/api/codeexecution/validate/{questionId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "sourceCode": "var twoSum = function(nums, target) {\n    return [0, 1];\n};",
  "language": "javascript"
}
```

**Note:** `stdin` is not needed for validation - it uses test case inputs from the database.

**Expected Response:**
```json
[
  {
    "testCaseId": "guid-here",
    "testCaseNumber": 1,
    "passed": true,
    "output": "[0,1]",
    "error": null,
    "runtime": 45.2,
    "memory": 10240,
    "status": "Accepted"
  },
  {
    "testCaseId": "guid-here",
    "testCaseNumber": 2,
    "passed": false,
    "output": "[0,1]",
    "error": null,
    "runtime": 42.1,
    "memory": 10240,
    "status": "Wrong Answer"
  }
]
```

---

## Testing with cURL

### 1. Get Authentication Token
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@vector.com","password":"Admin123!"}'
```

### 2. Test Languages Endpoint
```bash
curl -X GET http://localhost:5000/api/codeexecution/languages \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

### 3. Test Execute Endpoint
```bash
curl -X POST http://localhost:5000/api/codeexecution/execute \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceCode": "console.log(\"Hello, World!\");",
    "language": "javascript",
    "stdin": ""
  }'
```

### 4. Test Validate Endpoint
```bash
curl -X POST http://localhost:5000/api/codeexecution/validate/YOUR_QUESTION_ID \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "sourceCode": "var twoSum = function(nums, target) { return [0, 1]; };",
    "language": "javascript"
  }'
```

---

## Testing with Postman

1. **Import Collection:**
   - Create a new collection "Code Execution API"
   - Add environment variable `baseUrl` = `http://localhost:5000/api`
   - Add environment variable `token` = (from login response)

2. **Setup Pre-request Script for Auth:**
   ```javascript
   // Auto-login if token expired
   if (!pm.environment.get("token")) {
       pm.sendRequest({
           url: pm.environment.get("baseUrl") + "/auth/login",
           method: 'POST',
           header: { 'Content-Type': 'application/json' },
           body: {
               mode: 'raw',
               raw: JSON.stringify({
                   email: "admin@vector.com",
                   password: "Admin123!"
               })
           }
       }, function (err, res) {
           if (!err) {
               var jsonData = res.json();
               pm.environment.set("token", jsonData.accessToken);
           }
       });
   }
   ```

3. **Test Endpoints:**
   - Use `{{baseUrl}}/codeexecution/languages`
   - Use `{{baseUrl}}/codeexecution/execute`
   - Use `{{baseUrl}}/codeexecution/validate/{{questionId}}`
   - Set Authorization header: `Bearer {{token}}`

---

## Expected Status Values

- `"Accepted"` - Code executed successfully
- `"Wrong Answer"` - Code executed but output doesn't match expected
- `"Time Limit Exceeded"` - Code took too long to execute
- `"Compilation Error"` - Code has syntax errors
- `"Runtime Error"` - Code crashed during execution
- `"Memory Limit Exceeded"` - Code used too much memory

---

## Troubleshooting

### If endpoints return 401 Unauthorized:
- Check that token is valid
- Token may have expired (15 minutes default)
- Re-authenticate to get new token

### If endpoints return 500 Internal Server Error:
- Check backend logs: `docker-compose logs backend --tail 50`
- Check Judge0 is running: `docker ps | grep judge0`
- Check Judge0 health: `curl http://localhost:2358/languages`

### If code execution times out:
- Check Judge0 logs: `docker-compose logs judge0 --tail 50`
- Verify Judge0 is healthy: `docker ps | grep judge0`
- Check network connectivity between backend and Judge0

### If validation fails:
- Ensure question has test cases in database
- Check question ID is valid
- Verify test cases are not marked as hidden

