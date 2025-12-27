# Judge0 Official API Limitations (Free Tier)

## Overview

This document outlines the limitations of using Judge0 Official API (free tier) for code execution in the Vector platform.

**Base URL**: `https://ce.judge0.com`  
**Tier**: Free (no API key required)

---

## Rate Limits

### Daily Submission Limits
- **100 submissions per day** (free tier)
- **10 requests per minute** (rate limiting)
- Resets at midnight UTC

### Impact
- For MVP: Sufficient for development and testing
- For production: May need to upgrade to paid tier for higher limits
- **Recommendation**: Monitor usage and upgrade when approaching limits

---

## Execution Limits

### CPU Time Limit
- **Default**: 2 seconds per program
- **Maximum Configurable**: 15 seconds
- **Current Setting**: 5 seconds (configured in `CodeExecutionService.cs`)

### Memory Limit
- **Default**: 128 MB
- **Maximum Configurable**: 256 MB
- **Current Setting**: 128 MB (131,072 KB)

### Wall Time Limit
- **Default**: 5 seconds
- **Maximum Configurable**: 20 seconds
- **Current Setting**: 10 seconds

### Process/Thread Limits
- **Default**: 60 processes/threads
- **Maximum Configurable**: 120 processes/threads
- **Current Setting**: Default (60)

### Impact
- Most coding interview problems can be solved within these limits
- Complex algorithms or inefficient solutions may hit time limits
- Memory-intensive solutions may fail
- **Recommendation**: These limits are appropriate for interview practice

---

## Bandwidth Limits

### Request Size
- **Maximum source code size**: Not explicitly documented, but typically 1-2 MB
- **Maximum stdin size**: Similar limits apply
- **Response size**: Limited by API response format

### Network Bandwidth
- **No explicit bandwidth limits** documented for free tier
- **Rate limiting** (10 requests/minute) effectively limits bandwidth
- **Latency**: Depends on Judge0 server location and network conditions

### Impact
- Large code files may be rejected
- Multiple simultaneous requests may be rate-limited
- **Recommendation**: Keep code submissions reasonable in size

---

## Feature Limitations

### Supported Languages
- **60+ languages** supported
- **Current Implementation**: Python, JavaScript, Java, C++, C#, Go
- All major interview languages are available

### API Features
- ✅ Synchronous execution (`wait=true`)
- ✅ Asynchronous execution with polling
- ✅ Base64 encoding (optional, we use `false`)
- ✅ Multiple test case support
- ❌ **No custom compiler flags** (free tier)
- ❌ **No file uploads** (code must be in request body)
- ❌ **No persistent storage** (each submission is independent)

### Impact
- All required features for interview practice are available
- Advanced features may require paid tier
- **Recommendation**: Current features are sufficient for MVP

---

## Error Handling

### Common Errors
1. **Rate Limit Exceeded** (429)
   - **Cause**: More than 10 requests per minute
   - **Solution**: Implement request queuing or retry with backoff

2. **Daily Limit Exceeded** (429)
   - **Cause**: More than 100 submissions per day
   - **Solution**: Upgrade to paid tier or wait for reset

3. **Time Limit Exceeded** (Status 5)
   - **Cause**: Code execution exceeds CPU/wall time limit
   - **Solution**: Optimize code or increase limits (if possible)

4. **Memory Limit Exceeded** (Status 4)
   - **Cause**: Code uses more than allocated memory
   - **Solution**: Optimize memory usage

5. **Compilation Error** (Status 6)
   - **Cause**: Syntax errors or compilation failures
   - **Solution**: Fix code errors

### Impact
- Proper error handling is implemented in `CodeExecutionService`
- Users see meaningful error messages
- **Recommendation**: Monitor error rates and adjust limits if needed

---

## Cost Considerations

### Free Tier
- **Cost**: $0
- **Limitations**: As documented above
- **Suitable for**: MVP, development, testing, small-scale production

### Paid Tier (RapidAPI)
- **Cost**: Varies by plan (typically $10-50/month)
- **Benefits**: 
  - Higher rate limits (1000+ requests/day)
  - Faster response times
  - Priority support
  - Custom configurations
- **When to upgrade**: When approaching free tier limits

### Self-Hosting Alternative
- **Cost**: ~$33/month (EC2 instance)
- **Benefits**:
  - No rate limits
  - Full control
  - Custom configurations
- **When to consider**: When free tier is insufficient and paid tier is too expensive

---

## Monitoring Recommendations

### Metrics to Track
1. **Daily submission count**: Monitor against 100/day limit
2. **Rate limit hits**: Track 429 errors
3. **Execution failures**: Monitor time/memory limit errors
4. **Response times**: Track API latency
5. **Error rates**: Monitor compilation/runtime errors

### Alerts to Set Up
- **Warning**: 80% of daily limit reached (80 submissions)
- **Critical**: Daily limit exceeded
- **Warning**: Rate limit errors > 5% of requests
- **Warning**: Average response time > 5 seconds

---

## Mitigation Strategies

### For Rate Limits
1. **Request Queuing**: Queue requests and process sequentially
2. **Caching**: Cache results for identical code submissions
3. **User Limits**: Implement per-user rate limiting
4. **Upgrade**: Move to paid tier when needed

### For Execution Limits
1. **Code Optimization**: Encourage efficient solutions
2. **Limit Adjustments**: Increase limits within API constraints
3. **Error Messages**: Provide clear feedback on limit violations
4. **Alternative Execution**: Consider self-hosting for complex problems

### For Daily Limits
1. **Usage Monitoring**: Track daily usage
2. **User Education**: Inform users about limits
3. **Tiered Access**: Premium users get higher limits
4. **Upgrade Path**: Easy migration to paid tier

---

## Comparison: Free Tier vs Paid Tier vs Self-Hosted

| Feature | Free Tier | Paid Tier (RapidAPI) | Self-Hosted |
|---------|-----------|---------------------|-------------|
| **Daily Submissions** | 100 | 1000+ | Unlimited |
| **Rate Limit** | 10/min | 100+/min | Unlimited |
| **CPU Time Limit** | 15s max | 15s max | Configurable |
| **Memory Limit** | 256 MB max | 256 MB max | Configurable |
| **Cost** | $0 | $10-50/month | ~$33/month |
| **Setup Complexity** | None | API key only | High |
| **Maintenance** | None | None | Required |
| **Reliability** | High | High | Medium |
| **Scalability** | Limited | Good | Excellent |

---

## Recommendations for MVP

### Current Implementation (Free Tier)
✅ **Suitable for MVP** because:
- 100 submissions/day is sufficient for initial testing
- All required features are available
- No setup or maintenance required
- Zero cost

### When to Upgrade
Consider upgrading when:
- Daily submissions consistently > 80
- Rate limit errors become frequent
- User base grows beyond 50-100 active users
- Response times become unacceptable

### Migration Path
1. **Short-term**: Continue with free tier
2. **Medium-term**: Upgrade to RapidAPI paid tier if limits are hit
3. **Long-term**: Consider self-hosting if cost-effective at scale

---

## References

- [Judge0 Official API](https://ce.judge0.com)
- [Judge0 Documentation](https://ce.judge0.com/docs)
- [Judge0 Languages](https://ce.judge0.com/languages)
- [RapidAPI Judge0](https://rapidapi.com/judge0-official/api/judge0-ce) - Paid tier

---

## Last Updated

December 16, 2025

