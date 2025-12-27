# Code Execution Troubleshooting Guide

## Issues Identified

### 1. Code Execution Not Working
**Symptoms:**
- Code doesn't execute when clicking "Run"
- No output shown
- Errors not displayed

**Potential Causes:**
- Judge0 service not accessible from backend
- Language ID mismatch
- Network connectivity issues
- Authentication token issues

### 2. Solution Validation Not Working
**Symptoms:**
- Submit button doesn't validate test cases
- Test results not shown
- Solutions not saved

**Potential Causes:**
- Test cases not found for question
- Validation endpoint errors
- Solution submission endpoint errors

### 3. Solution History Not Working
**Symptoms:**
- Solution history page empty
- Filters not working
- No solutions displayed

**Potential Causes:**
- API endpoint response format mismatch
- Authentication issues
- Database query issues

## Debugging Steps

1. Check Judge0 is running: `docker ps | grep judge0`
2. Test Judge0 directly: `curl http://localhost:2358/languages`
3. Check backend logs: `docker-compose logs backend`
4. Check frontend console for errors
5. Test API endpoints directly with Postman/curl

