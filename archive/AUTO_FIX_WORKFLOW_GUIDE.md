# Auto-Fix Workflow Guide

## Overview

The Vector project now includes automated task creation for CI/CD build failures. When a build fails, the system automatically:
1. Creates a GitHub issue with error details
2. Creates a `.cursor-fix-tasks.md` file in the repository root

## How It Works

### Workflow Trigger
The `.github/workflows/auto-fix-issues.yml` workflow triggers when:
- Frontend CI/CD workflow fails
- Backend CI/CD workflow fails
- On branches: `develop`, `staging`, `main`

### What Happens Automatically

1. **GitHub Issue Created:**
   - Title: `🔧 Fix Required: [Workflow Name] Build Failed on [branch]`
   - Labels: `auto-generated`, `ci-failure`, `bug`
   - Body: Contains error summary and link to failed workflow run

2. **Cursor Fix Tasks File Created:**
   - Location: `.cursor-fix-tasks.md` in repository root
   - Contains: Workflow details, branch, commit SHA, action items
   - Purpose: Provides Cursor AI with context for fixing

### Using the Auto-Fix System

#### When a Build Fails

1. **Check GitHub:**
   - Go to: https://github.com/[your-repo]/issues
   - Look for the auto-generated issue
   - Review error details

2. **Check Repository:**
   - Look for `.cursor-fix-tasks.md` in the root
   - Read the file for specific error details
   - Use the information to guide your fixes

3. **Fix the Issues:**
   - Address the problems mentioned in the issue
   - Test locally before pushing
   - Ensure linting and tests pass

4. **Clean Up:**
   - Delete `.cursor-fix-tasks.md` after fixing
   - Close the GitHub issue
   - Push fixes to trigger a new build

#### Example Workflow

```powershell
# 1. Pull the auto-generated fix file
git pull origin develop

# 2. Review the fix tasks
cat .cursor-fix-tasks.md

# 3. Fix the issues (Cursor AI can help)
# ... make your fixes ...

# 4. Test locally
cd frontend
npm run lint
npm run build

cd ../backend
dotnet test

# 5. Clean up and push
rm .cursor-fix-tasks.md
git add -A
git commit -m "Fix CI/CD build errors"
git push origin develop
```

## Current Implementation

### Auto-Fix Issues Workflow

**File:** `.github/workflows/auto-fix-issues.yml`

**Features:**
- Monitors Frontend CI/CD and Backend CI/CD workflows
- Triggers only on workflow failures
- Creates detailed error summaries
- Updates existing issues instead of creating duplicates
- Commits `.cursor-fix-tasks.md` to the repository

**Limitations:**
- Full log access requires additional API permissions
- Protected branches may prevent auto-commit
- Works best with open branches (develop, staging)

## Cursor Rules Integration

### .cursorrules File

**Location:** `.cursorrules` in repository root

**Purpose:**
- Documents React best practices
- Provides examples of correct/incorrect patterns
- Guides on fixing common ESLint errors

**Key Rules:**
1. **DO NOT** call setState synchronously in useEffect
2. **DO** use useState initializer for state based on props/params
3. **DO** use setTimeout/requestAnimationFrame if state update needed
4. **DO** use cleanup functions for timers/subscriptions

### Example Patterns

#### ✅ Correct Pattern
```typescript
// Initialize state based on prop/param
const [error, setError] = useState(() => 
  !token ? 'Error message' : ''
);

// Use existing hook state
const { isLoading } = useAuth();
if (isLoading) return <Loading />;
```

#### ❌ Incorrect Pattern
```typescript
// Synchronous setState in effect
useEffect(() => {
  setError('Error message'); // Triggers cascading renders
}, [token]);
```

## Future Enhancements

### Planned Improvements
1. **Enhanced Error Parsing:**
   - Extract specific error messages from logs
   - Categorize errors (linting, build, deployment)
   - Provide suggested fixes

2. **Auto-Fix Suggestions:**
   - Use AI to suggest code fixes
   - Create pull requests with fixes
   - Run tests automatically

3. **Notification Integration:**
   - Send Slack/Discord notifications
   - Email notifications for critical failures
   - Summary reports

4. **Build Metrics:**
   - Track build success/failure rates
   - Measure time to fix
   - Identify recurring issues

## Troubleshooting

### Issue: No GitHub Issue Created
**Possible Causes:**
- Workflow didn't have permissions to create issues
- Issue already exists (check for duplicates)
- Workflow failed to complete

**Solution:**
- Check GitHub Actions workflow run logs
- Verify GitHub token has issue creation permissions

### Issue: .cursor-fix-tasks.md Not Created
**Possible Causes:**
- Protected branch prevents commit
- Workflow doesn't have write permissions
- Workflow failed before file creation

**Solution:**
- Check workflow logs for errors
- Manually create the file based on issue details

### Issue: Too Many Auto-Generated Issues
**Solution:**
- The workflow updates existing issues instead of creating new ones
- Close resolved issues to prevent clutter
- Add filters to issue search

## Best Practices

1. **Review Before Fixing:**
   - Read the auto-generated issue carefully
   - Check the failed workflow run for full context
   - Understand the root cause before fixing

2. **Test Locally:**
   - Always test fixes locally before pushing
   - Run linting and tests
   - Verify the fix addresses the root cause

3. **Clean Up:**
   - Delete `.cursor-fix-tasks.md` after fixing
   - Close GitHub issues when resolved
   - Add comments to issues explaining the fix

4. **Prevent Recurrence:**
   - Update `.cursorrules` if needed
   - Add tests to catch similar issues
   - Document common pitfalls

## References

- Cursor Rules: `.cursorrules`
- Auto-Fix Workflow: `.github/workflows/auto-fix-issues.yml`
- React Best Practices: https://react.dev/learn/you-might-not-need-an-effect

