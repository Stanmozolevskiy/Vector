# Judge0 Windows Docker Limitation

## Issue

Judge0 CE cannot execute code properly when running in Docker on Windows due to cgroups limitations.

### Error Messages
- `Failed to create control group /sys/fs/cgroup/memory/box-13/: No such file or directory`
- `No such file or directory @ rb_sysopen - /box/script.js`
- Status: `13 - Internal Error`

### Root Cause

Judge0 uses [isolate](https://github.com/ioi/isolate) for secure code execution, which requires:
- Linux cgroups (v1 or v2) for resource isolation
- Ability to create control groups dynamically
- Full Linux kernel features

Docker Desktop on Windows (even with WSL2 backend) has limitations with cgroups support, preventing Judge0 from creating the necessary isolation environments.

## Current Configuration

✅ **Backend Configuration**: Correctly configured to connect to `judge0-ce:2358`
✅ **Judge0 Service**: Running and healthy, API endpoints responding
✅ **Network**: Services can communicate
❌ **Code Execution**: Fails due to cgroups/isolate limitations

## Solutions

### Option 1: Use WSL2 with Full Linux (Recommended for Local Development)

1. Install WSL2 with a Linux distribution (Ubuntu recommended)
2. Install Docker inside WSL2 (not Docker Desktop)
3. Run Judge0 from within WSL2 Linux environment
4. This provides full cgroups support

### Option 2: Use Judge0 Official API (Quick Solution)

Use Judge0's hosted API for development:
- Update `Judge0:BaseUrl` to `https://judge0-ce.p.rapidapi.com` (requires API key)
- Or use `https://ce.judge0.com` (free tier available)

### Option 3: Use Alternative Code Execution Service

Consider alternatives that work better on Windows:
- **Piston API**: Lightweight, Docker-based, works on Windows
- **CodeX API**: Simple code execution service
- **Custom solution**: Build a simple code execution service using Docker containers

### Option 4: Linux VM or Remote Server

Run Judge0 on:
- Linux VM (VirtualBox, VMware)
- Remote Linux server
- Cloud instance (AWS EC2, DigitalOcean, etc.)

## Current Setup Status

- ✅ Judge0 CE service: `vector-judge0-ce` (running on port 2358)
- ✅ Backend configured: `http://judge0-ce:2358`
- ✅ All services healthy and communicating
- ❌ Code execution fails due to Windows/cgroups limitation

## Next Steps

1. **For immediate development**: Consider using Judge0's official API or an alternative service
2. **For production**: Deploy Judge0 on a Linux server/VM with proper cgroups support
3. **For local testing**: Set up WSL2 with native Linux Docker

## Testing

To verify the issue, test directly:
```powershell
$body = '{"source_code":"print(\"test\");","language_id":71,"stdin":"","cpu_time_limit":5,"memory_limit":131072,"wall_time_limit":10}'
Invoke-WebRequest -Uri "http://localhost:2358/submissions?base64_encoded=false&wait=true" -Method POST -Body $body -ContentType "application/json"
```

Expected: Status 201, but Status ID 13 (Internal Error) with message about `/box/script.py`

