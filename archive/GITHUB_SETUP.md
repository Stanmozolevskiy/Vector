# GitHub Connection Guide

This guide will help you connect your local repository to GitHub.

## Account Setup Decision

**Recommended**: Use your existing personal GitHub account unless:
- This is a commercial/business project requiring separation
- You need organizational structure for team collaboration
- You want complete anonymity

For personal/side projects, using your existing account is simpler and builds your portfolio. You can always create an organization later or move the repository.

## Step 1: Create a GitHub Repository

1. Go to [GitHub.com](https://github.com) and sign in
2. Click the **+** icon in the top right corner
3. Select **New repository**
4. Name your repository (e.g., `Vector`)
5. **DO NOT** initialize with README, .gitignore, or license (we already have these)
6. Click **Create repository**

## Step 2: Connect Your Local Repository

After creating the repository on GitHub, you'll see a page with setup instructions. Use one of these methods:

### Method 1: Using Git Commands (Recommended)

Run these commands in your terminal (replace `YOUR_USERNAME` with your GitHub username):

```bash
git remote add origin https://github.com/YOUR_USERNAME/Vector.git
git branch -M main
git push -u origin main
```

### Method 2: Using Cursor's Git Integration

1. Open the **Source Control** panel in Cursor (Ctrl+Shift+G)
2. Click the **...** menu (three dots) at the top
3. Select **Remote** → **Add Remote**
4. Enter the remote name: `origin`
5. Enter the remote URL: `https://github.com/YOUR_USERNAME/Vector.git`
6. Click **Add**
7. Then push using: **Push** → **Push to origin/main** (or use the command below)

## Step 3: Push Your Code

If you haven't already pushed, run:

```bash
git push -u origin main
```

**Note**: If your default branch is `master` instead of `main`, use:
```bash
git branch -M main
git push -u origin main
```

## Authentication

When you push, you may be prompted to authenticate. You have two options:

### Option A: Personal Access Token (Recommended)
1. Go to GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Generate a new token with `repo` scope
3. Use this token as your password when pushing

### Option B: GitHub Desktop
- Install GitHub Desktop and authenticate through it

### Option C: SSH Keys
- Set up SSH keys for passwordless authentication

## Verify Connection

After pushing, verify the connection:

```bash
git remote -v
```

You should see:
```
origin  https://github.com/YOUR_USERNAME/Vector.git (fetch)
origin  https://github.com/YOUR_USERNAME/Vector.git (push)
```

## Troubleshooting

- **Authentication failed**: Make sure you're using a Personal Access Token, not your GitHub password
- **Repository not found**: Double-check the repository name and your username
- **Branch name mismatch**: Run `git branch -M main` to rename your branch to `main`

