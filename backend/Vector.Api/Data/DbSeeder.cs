using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Vector.Api.Helpers;
using Vector.Api.Models;

namespace Vector.Api.Data;

/// <summary>
/// Seeds the database with initial data (admin user, etc.)
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Seeds the database with default admin user if it doesn't exist
    /// </summary>
    public static async Task SeedAdminUser(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Check if any admin users exist
            var adminExists = await context.Users.AnyAsync(u => u.Role == "admin");

            if (adminExists)
            {
                logger.LogInformation("Admin user already exists. Skipping seed.");
                return;
            }

            // Create default admin user
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@vector.com",
                PasswordHash = PasswordHasher.HashPassword("Admin@123"), // Default password
                FirstName = "System",
                LastName = "Administrator",
                Role = "admin",
                EmailVerified = true, // Admin is pre-verified
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            logger.LogWarning(
                "⚠️ DEFAULT ADMIN USER CREATED ⚠️\n" +
                "Email: admin@vector.com\n" +
                "Password: Admin@123\n" +
                "⚠️ CHANGE THIS PASSWORD IMMEDIATELY IN PRODUCTION! ⚠️"
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed admin user");
            throw;
        }
    }

    /// <summary>
    /// Seeds all initial data
    /// </summary>
    public static async Task SeedDatabase(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting database seeding...");

        // Seed admin user first (critical)
        try
        {
            await SeedAdminUser(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed admin user, but continuing with other seeds...");
        }

        // Seed achievement definitions (critical for gamification)
        try
        {
            await DbInitializer.SeedAchievementDefinitionsAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed achievement definitions, but continuing...");
        }

        // Seed interview questions (non-critical)
        try
        {
            await SeedInterviewQuestions(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed interview questions, but this is non-critical. Continuing...");
        }

        logger.LogInformation("Database seeding completed");
    }

    /// <summary>
    /// Seeds sample interview questions for testing
    /// </summary>
    public static async Task SeedInterviewQuestions(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Check if SQL questions exist
            var existingSqlQuestions = await context.InterviewQuestions
                .Where(q => q.QuestionType == "SQL")
                .ToListAsync();
            
            // Get all existing question titles upfront to avoid duplicates
            var allExistingTitles = await context.InterviewQuestions
                .Select(q => q.Title)
                .ToListAsync();
            
            // Replace all questions with fresh seed. User profile data (Users, passwords, etc.) is
            // never touched — only question-related tables are cleared and repopulated.
            var existingQuestions = await context.InterviewQuestions.ToListAsync();
            if (existingQuestions.Any())
            {
                var questionIds = existingQuestions.Select(q => q.Id).ToList();

                // Clear question FK references (nullable) so we can delete questions
                var scheduledSessions = await context.ScheduledInterviewSessions
                    .Where(s => s.AssignedQuestionId != null && questionIds.Contains(s.AssignedQuestionId!.Value))
                    .ToListAsync();
                foreach (var s in scheduledSessions) s.AssignedQuestionId = null;

                var liveSessions = await context.LiveInterviewSessions
                    .Where(s => s.FirstQuestionId != null || s.SecondQuestionId != null || s.ActiveQuestionId != null)
                    .ToListAsync();
                foreach (var s in liveSessions)
                {
                    s.FirstQuestionId = null;
                    s.SecondQuestionId = null;
                    s.ActiveQuestionId = null;
                }

                var whiteboardData = await context.WhiteboardData
                    .Where(w => w.QuestionId != null && questionIds.Contains(w.QuestionId!.Value))
                    .ToListAsync();
                foreach (var w in whiteboardData) w.QuestionId = null;

                await context.SaveChangesAsync();

                // Delete question-dependent rows (required FKs) — we never touch Users, profile, password, etc.
                var userSolutionsToDelete = await context.UserSolutions.Where(s => questionIds.Contains(s.QuestionId)).ToListAsync();
                context.UserSolutions.RemoveRange(userSolutionsToDelete);

                var draftsToDelete = await context.UserCodeDrafts.Where(d => questionIds.Contains(d.QuestionId)).ToListAsync();
                context.UserCodeDrafts.RemoveRange(draftsToDelete);

                var solvedToDelete = await context.UserSolvedQuestions.Where(s => questionIds.Contains(s.QuestionId)).ToListAsync();
                context.UserSolvedQuestions.RemoveRange(solvedToDelete);

                var bookmarksToDelete = await context.QuestionBookmarks.Where(b => questionIds.Contains(b.QuestionId)).ToListAsync();
                context.QuestionBookmarks.RemoveRange(bookmarksToDelete);

                var commentsToDelete = await context.InterviewQuestionComments.Where(c => questionIds.Contains(c.QuestionId)).ToListAsync();
                var commentIds = commentsToDelete.Select(c => c.Id).ToList();
                var commentVotesToDelete = await context.InterviewQuestionCommentVotes.Where(v => commentIds.Contains(v.CommentId)).ToListAsync();
                context.InterviewQuestionCommentVotes.RemoveRange(commentVotesToDelete);
                context.InterviewQuestionComments.RemoveRange(commentsToDelete);

                var votesToDelete = await context.QuestionVotes.Where(v => questionIds.Contains(v.QuestionId)).ToListAsync();
                context.QuestionVotes.RemoveRange(votesToDelete);

                var dailyChallengesToDelete = await context.DailyChallenges.Where(d => questionIds.Contains(d.QuestionId)).ToListAsync();
                var dailyChallengeIds = dailyChallengesToDelete.Select(d => d.Id).ToList();
                var challengeAttemptsToDelete = await context.UserChallengeAttempts.Where(a => dailyChallengeIds.Contains(a.ChallengeId)).ToListAsync();
                context.UserChallengeAttempts.RemoveRange(challengeAttemptsToDelete);
                context.DailyChallenges.RemoveRange(dailyChallengesToDelete);

                var testCasesToDelete = await context.QuestionTestCases.Where(tc => questionIds.Contains(tc.QuestionId)).ToListAsync();
                context.QuestionTestCases.RemoveRange(testCasesToDelete);

                var solutionsToDelete = await context.QuestionSolutions.Where(s => questionIds.Contains(s.QuestionId)).ToListAsync();
                context.QuestionSolutions.RemoveRange(solutionsToDelete);

                context.InterviewQuestions.RemoveRange(existingQuestions);
                await context.SaveChangesAsync();

                allExistingTitles = new List<string>();
                logger.LogInformation("Cleared all interview questions and related data for repopulation. User profiles and auth data were not touched.");
            }

            // Get admin user for CreatedBy
            var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Role == "admin");
            var createdBy = adminUser?.Id;

            var questions = new List<InterviewQuestion>();
            var now = DateTime.UtcNow;

            // Sample questions data
            var questionsData = new[]
            {
                new {
                    Title = "Two Sum",
                    Description = "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.\n\nYou may assume that each input would have exactly one solution, and you may not use the same element twice.\n\nYou can return the answer in any order.",
                    Difficulty = "Easy",
                    QuestionType = "Coding",
                    Category = "Arrays",
                    Tags = new[] { "Array", "Hash Table" },
                    CompanyTags = new[] { "Google", "Amazon", "Facebook" },
                    Constraints = "2 <= nums.length <= 10^4\n-10^9 <= nums[i] <= 10^9\n-10^9 <= target <= 10^9\nOnly one valid answer exists.",
                    Examples = new[] {
                        new { Input = "nums = [2,7,11,15], target = 9", Output = "[0,1]", Explanation = (string?)"Because nums[0] + nums[1] == 9, we return [0, 1]." },
                        new { Input = "nums = [3,2,4], target = 6", Output = "[1,2]", Explanation = (string?)null },
                        new { Input = "nums = [3,3], target = 6", Output = "[0,1]", Explanation = (string?)null }
                    },
                    Hints = new[] { 
                        "A really brute force way would be to search for all possible pairs of numbers but that would be too slow. Again, it's best to try out brute force solutions just for completeness. It is from these brute force solutions that you can come up with optimizations.",
                        "So, if we fix one of the numbers, say x, we have to scan the entire array to find the next number y which is value - x where value is the input parameter. Can we change our array somehow so that this search becomes faster?",
                        "The second train of thought is, without changing the array, can we use additional space somehow? Like maybe a hash map to speed up the search?"
                    },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(n)",
                    AcceptanceRate = 48.5
                },
                new {
                    Title = "Add Two Numbers",
                    Description = "You are given two non-empty linked lists representing two non-negative integers. The digits are stored in reverse order, and each of their nodes contains a single digit. Add the two numbers and return the sum as a linked list.\n\nYou may assume the two numbers do not contain any leading zero, except the number 0 itself.\n\n**Note:** The test system automatically converts arrays to linked lists for testing. For example, `[2,4,3]` is automatically converted to `2 → 4 → 3` before being passed to your function.",
                    Difficulty = "Medium",
                    QuestionType = "Coding",
                    Category = "Linked List",
                    Tags = new[] { "Linked List", "Math", "Recursion" },
                    CompanyTags = new[] { "Amazon", "Microsoft", "Bloomberg" },
                    Constraints = "The number of nodes in each linked list is in the range [1, 100].\n0 <= Node.val <= 9\nIt is guaranteed that the list represents a number that does not have leading zeros.",
                    Examples = new[] {
                        new { Input = "l1 = [2,4,3], l2 = [5,6,4]", Output = "[7,0,8]", Explanation = (string?)"342 + 465 = 807." },
                        new { Input = "l1 = [0], l2 = [0]", Output = "[0]", Explanation = (string?)null },
                        new { Input = "l1 = [9,9,9,9,9,9,9], l2 = [9,9,9,9]", Output = "[8,9,9,9,0,0,0,1]", Explanation = (string?)null }
                    },
                    Hints = new[] { 
                        "Make sure you understand what is required. You need to add two numbers represented as linked lists where digits are stored in reverse order. Try working through the example: l1 = [2,4,3] represents 342, and l2 = [5,6,4] represents 465.",
                        "Think about how you add numbers manually with pen and paper. You start from the rightmost digit, add them together, and if the sum is 10 or more, you carry over to the next digit. Apply the same logic here, but remember the digits are already stored in reverse order.",
                        "Use a dummy head node to simplify your code. Keep track of the carry using a variable. As you traverse both lists, add the current digits along with any carry from the previous addition. If the sum is 10 or more, update the carry for the next iteration."
                    },
                    TimeComplexityHint = "O(max(m,n))",
                    SpaceComplexityHint = "O(max(m,n))",
                    AcceptanceRate = 38.2
                },
                new {
                    Title = "Longest Substring Without Repeating Characters",
                    Description = "Given a string s, find the length of the longest substring without repeating characters.",
                    Difficulty = "Medium",
                    QuestionType = "Coding",
                    Category = "Strings",
                    Tags = new[] { "String", "Sliding Window", "Hash Table" },
                    CompanyTags = new[] { "Amazon", "Microsoft", "Bloomberg" },
                    Constraints = "0 <= s.length <= 5 * 10^4\ns consists of English letters, digits, symbols and spaces.",
                    Examples = new[] {
                        new { Input = "s = \"abcabcbb\"", Output = "3", Explanation = (string?)"The answer is \"abc\", with the length of 3." },
                        new { Input = "s = \"bbbbb\"", Output = "1", Explanation = (string?)"The answer is \"b\", with the length of 1." },
                        new { Input = "s = \"pwwkew\"", Output = "3", Explanation = (string?)"The answer is \"wke\", with the length of 3." }
                    },
                    Hints = new[] { 
                        "A brute force approach would check every possible substring, but that would be O(n³) which is too slow. Think about how to optimize this.",
                        "If your peer is stuck, ask them to write down all possible substrings for a small example like \"abcabcbb\". After this is done, you may advise them to use a sliding window approach.",
                        "The key insight is to use two pointers (left and right) to maintain a window of characters. Use a hash map to track the last seen index of each character. When you encounter a duplicate character, move the left pointer to skip past the previous occurrence."
                    },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(min(n,m))",
                    AcceptanceRate = 33.8
                },
                new {
                    Title = "Median of Two Sorted Arrays",
                    Description = "Given two sorted arrays nums1 and nums2 of size m and n respectively, return the median of the two sorted arrays.\n\nThe overall run time complexity should be O(log (m+n)).",
                    Difficulty = "Hard",
                    QuestionType = "Coding",
                    Category = "Arrays",
                    Tags = new[] { "Array", "Binary Search", "Divide and Conquer" },
                    CompanyTags = new[] { "Google", "Amazon", "Microsoft" },
                    Constraints = "nums1.length == m\nnums2.length == n\n0 <= m <= 1000\n0 <= n <= 1000\n1 <= m + n <= 2000\n-10^6 <= nums1[i], nums2[i] <= 10^6",
                    Examples = new[] {
                        new { Input = "nums1 = [1,3], nums2 = [2]", Output = "2.00000", Explanation = (string?)"merged array = [1,2,3] and median is 2." },
                        new { Input = "nums1 = [1,2], nums2 = [3,4]", Output = "2.50000", Explanation = (string?)"merged array = [1,2,3,4] and median is (2 + 3) / 2 = 2.5." }
                    },
                    Hints = new[] { 
                        "The naive approach would be to merge both arrays and find the median, but that would be O(m+n) time. The problem asks for O(log(m+n)) time, which suggests binary search.",
                        "If you're stuck, think about what the median means: it's the middle element(s) of a sorted array. For two sorted arrays, we need to find a partition such that elements on the left are less than or equal to elements on the right.",
                        "The solution involves binary searching on the smaller array to find the correct partition. For each partition, check if it satisfies the median condition: max(left) <= min(right) for both arrays."
                    },
                    TimeComplexityHint = "O(log(min(m,n)))",
                    SpaceComplexityHint = "O(1)",
                    AcceptanceRate = 35.4
                },
                new {
                    Title = "Longest Palindromic Substring",
                    Description = "Given a string s, return the longest palindromic substring in s.",
                    Difficulty = "Medium",
                    QuestionType = "Coding",
                    Category = "Strings",
                    Tags = new[] { "String", "Dynamic Programming" },
                    CompanyTags = new[] { "Amazon", "Microsoft", "Bloomberg" },
                    Constraints = "1 <= s.length <= 1000\ns consist of only digits and English letters.",
                    Examples = new[] {
                        new { Input = "s = \"babad\"", Output = "\"bab\"", Explanation = (string?)"\"aba\" is also a valid answer." },
                        new { Input = "s = \"cbbd\"", Output = "\"bb\"", Explanation = (string?)null }
                    },
                    Hints = new[] { 
                        "A brute force approach would check every possible substring to see if it's a palindrome, but that would be O(n³) which is too slow. Think about how to optimize.",
                        "If you're stuck, ask yourself: if you know that \"aba\" is a palindrome, can you quickly determine if \"cabac\" is also a palindrome? This suggests we can reuse previous computations.",
                        "Use dynamic programming or expand around centers. For each position, expand outward to find the longest palindrome centered at that position. Consider both odd-length (center at a character) and even-length (center between characters) palindromes."
                    },
                    TimeComplexityHint = "O(n^2)",
                    SpaceComplexityHint = "O(n^2)",
                    AcceptanceRate = 32.1
                },
                new {
                    Title = "Container With Most Water",
                    Description = "You are given an integer array height of length n. There are n vertical lines drawn such that the two endpoints of the ith line are (i, 0) and (i, height[i]).\n\nFind two lines that together with the x-axis form a container, such that the container contains the most water.\n\nReturn the maximum amount of water a container can store.",
                    Difficulty = "Medium",
                    QuestionType = "Coding",
                    Category = "Arrays",
                    Tags = new[] { "Array", "Two Pointers", "Greedy" },
                    CompanyTags = new[] { "Amazon", "Facebook", "Microsoft" },
                    Constraints = "n == height.length\n2 <= n <= 10^5\n0 <= height[i] <= 10^4",
                    Examples = new[] {
                        new { Input = "height = [1,8,6,2,5,4,8,3,7]", Output = "49", Explanation = (string?)"The above vertical lines are represented by array [1,8,6,2,5,4,8,3,7]. In this case, the max area of water the container can contain is 49." },
                        new { Input = "height = [1,1]", Output = "1", Explanation = (string?)null }
                    },
                    Hints = new[] { 
                        "A brute force approach would check all pairs of lines and calculate the area for each, but that would be O(n²) which is too slow. Think about how to optimize.",
                        "If you're stuck, think about the area formula: area = min(height[left], height[right]) * (right - left). Notice that the width decreases as we move the pointers inward.",
                        "Use two pointers starting from both ends. At each step, calculate the area and update the maximum. Then, move the pointer pointing to the shorter line inward, because moving the taller line inward can only decrease the area."
                    },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(1)",
                    AcceptanceRate = 54.3
                },
                new {
                    Title = "Reverse Linked List",
                    Description = "Given the head of a singly linked list, reverse the list, and return the reversed list.",
                    Difficulty = "Easy",
                    QuestionType = "Coding",
                    Category = "Linked List",
                    Tags = new[] { "Linked List", "Recursion" },
                    CompanyTags = new[] { "Amazon", "Microsoft", "Apple" },
                    Constraints = "The number of nodes in the list is the range [0, 5000].\n-5000 <= Node.val <= 5000",
                    Examples = new[] {
                        new { Input = "head = [1,2,3,4,5]", Output = "[5,4,3,2,1]", Explanation = (string?)null },
                        new { Input = "head = [1,2]", Output = "[2,1]", Explanation = (string?)null },
                        new { Input = "head = []", Output = "[]", Explanation = (string?)null }
                    },
                    Hints = new[] { 
                        "Make sure you understand what reversing a linked list means. Draw out an example: [1,2,3,4,5] should become [5,4,3,2,1]. Notice that each node's next pointer needs to point to the previous node.",
                        "If you're stuck, try solving it iteratively first. You'll need three pointers: one for the previous node, one for the current node, and one for the next node (to save before overwriting).",
                        "For a recursive approach, think about reversing the rest of the list first, then connecting the current node. The base case is when you reach the end of the list (null)."
                    },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(1)",
                    AcceptanceRate = 68.2
                },
                new {
                    Title = "Valid Parentheses",
                    Description = "Given a string s containing just the characters '(', ')', '{', '}', '[' and ']', determine if the input string is valid.\n\nAn input string is valid if:\n1. Open brackets must be closed by the same type of brackets.\n2. Open brackets must be closed in the correct order.\n3. Every close bracket has a corresponding open bracket of the same type.",
                    Difficulty = "Easy",
                    QuestionType = "Coding",
                    Category = "Strings",
                    Tags = new[] { "String", "Stack" },
                    CompanyTags = new[] { "Amazon", "Google", "Microsoft" },
                    Constraints = "1 <= s.length <= 10^4\ns consists of parentheses only '()[]{}'.",
                    Examples = new[] {
                        new { Input = "s = \"()\"", Output = "true", Explanation = (string?)null },
                        new { Input = "s = \"()[]{}\"", Output = "true", Explanation = (string?)null },
                        new { Input = "s = \"(]\"", Output = "false", Explanation = (string?)null }
                    },
                    Hints = new[] { 
                        "Make sure you understand the requirements: every opening bracket must have a matching closing bracket of the same type, and they must be in the correct order. Try validating \"()[]{}\" and \"([)]\" to see the difference.",
                        "If you're stuck, think about the order of operations. When you see an opening bracket, you'll need to match it later. When you see a closing bracket, it must match the most recent unmatched opening bracket.",
                        "Use a stack (or array) to keep track of opening brackets. When you encounter a closing bracket, check if it matches the top of the stack. If it does, pop from the stack. If the stack is empty at the end, all brackets are matched."
                    },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(n)",
                    AcceptanceRate = 42.1
                },
                new {
                    Title = "Merge Two Sorted Lists",
                    Description = "You are given the heads of two sorted linked lists list1 and list2.\n\nMerge the two lists in a one sorted list. The list should be made by splicing together the nodes of the first two lists.\n\nReturn the head of the merged linked list.",
                    Difficulty = "Easy",
                    QuestionType = "Coding",
                    Category = "Linked List",
                    Tags = new[] { "Linked List", "Recursion" },
                    CompanyTags = new[] { "Amazon", "Microsoft", "Apple" },
                    Constraints = "The number of nodes in both lists is in the range [0, 50].\n-100 <= Node.val <= 100\nBoth list1 and list2 are sorted in non-decreasing order.",
                    Examples = new[] {
                        new { Input = "list1 = [1,2,4], list2 = [1,3,4]", Output = "[1,1,2,3,4,4]", Explanation = (string?)null },
                        new { Input = "list1 = [], list2 = []", Output = "[]", Explanation = (string?)null },
                        new { Input = "list1 = [], list2 = [0]", Output = "[0]", Explanation = (string?)null }
                    },
                    Hints = new[] { 
                        "Make sure you understand what merging means. You need to combine two sorted lists into one sorted list. Try working through the example: [1,2,4] and [1,3,4] should become [1,1,2,3,4,4].",
                        "If you're stuck, think about how you would merge two sorted arrays. You compare the current elements from both lists and add the smaller one to the result. Apply the same logic to linked lists.",
                        "Use two pointers, one for each list. Compare the values at the current positions, add the smaller value to the result, and advance that pointer. Continue until one list is exhausted, then append the remaining elements."
                    },
                    TimeComplexityHint = "O(n+m)",
                    SpaceComplexityHint = "O(1)",
                    AcceptanceRate = 58.3
                },
                new {
                    Title = "Maximum Subarray",
                    Description = "Given an integer array nums, find the contiguous subarray (containing at least one number) which has the largest sum and return its sum.\n\nA subarray is a contiguous part of an array.",
                    Difficulty = "Medium",
                    QuestionType = "Coding",
                    Category = "Arrays",
                    Tags = new[] { "Array", "Divide and Conquer", "Dynamic Programming" },
                    CompanyTags = new[] { "Amazon", "Microsoft", "LinkedIn" },
                    Constraints = "1 <= nums.length <= 10^5\n-10^4 <= nums[i] <= 10^4",
                    Examples = new[] {
                        new { Input = "nums = [-2,1,-3,4,-1,2,1,-5,4]", Output = "6", Explanation = (string?)"[4,-1,2,1] has the largest sum = 6." },
                        new { Input = "nums = [1]", Output = "1", Explanation = (string?)null },
                        new { Input = "nums = [5,4,-1,7,8]", Output = "23", Explanation = (string?)null }
                    },
                    Hints = new[] { 
                        "A brute force approach would check all possible subarrays, but that would be O(n³) or O(n²) which is too slow for large inputs. Think about how to optimize.",
                        "If you're stuck, think about this: if you know the maximum sum ending at position i-1, can you use that to find the maximum sum ending at position i? This suggests dynamic programming.",
                        "Use Kadane's algorithm: keep track of the maximum sum of a subarray ending at the current position. At each position, decide whether to extend the previous subarray or start a new one. The answer is the maximum of all these local maximums."
                    },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(1)",
                    AcceptanceRate = 49.8
                },
                // SQL Questions for Data Engineers
                new {
                    Title = "Second Highest Salary",
                    Description = "Write a SQL query to get the second highest salary from the Employee table. If there is no second highest salary, then the query should return null.\n\nTable: Employee\n+----+--------+\n| Id | Salary |\n+----+--------+\n| 1  | 100    |\n| 2  | 200    |\n| 3  | 300    |\n+----+--------+",
                    Difficulty = "Easy",
                    QuestionType = "SQL",
                    Category = "Database",
                    Tags = new[] { "SQL", "Subquery", "MAX", "LIMIT" },
                    CompanyTags = new[] { "Amazon", "Microsoft", "Bloomberg" },
                    Constraints = "There will be at least one record in the Employee table.\nSalary values are positive integers.",
                    Examples = new[] {
                        new { Input = "Employee table:\n| Id | Salary |\n|----|--------|\n| 1  | 100    |\n| 2  | 200    |\n| 3  | 300    |", Output = "| SecondHighestSalary |\n|---------------------|\n| 200                 |", Explanation = (string?)"The second highest salary is 200." },
                        new { Input = "Employee table:\n| Id | Salary |\n|----|--------|\n| 1  | 100    |", Output = "| SecondHighestSalary |\n|---------------------|\n| null                |", Explanation = (string?)"There is no second highest salary, so return null." }
                    },
                    Hints = new[] { 
                        "Think about how to find the maximum value first, then find the maximum value that is less than the overall maximum.",
                        "You can use a subquery to exclude the highest salary, then find the maximum of the remaining salaries.",
                        "Alternatively, you can use LIMIT and OFFSET, but remember to handle the case where there are fewer than 2 distinct salaries."
                    },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(1)",
                    AcceptanceRate = 35.2
                },
                new {
                    Title = "Employees Earning More Than Their Managers",
                    Description = "The Employee table holds all employees including their managers. Every employee has an Id, and there is also a column for the manager Id.\n\nGiven the Employee table, write a SQL query that finds out employees who earn more than their managers.\n\nTable: Employee\n+----+-------+--------+-----------+\n| Id | Name  | Salary | ManagerId |\n+----+-------+--------+-----------+\n| 1  | Joe   | 70000  | 3         |\n| 2  | Henry | 80000  | 4         |\n| 3  | Sam   | 60000  | NULL      |\n| 4  | Max   | 90000  | NULL      |\n+----+-------+--------+-----------+",
                    Difficulty = "Easy",
                    QuestionType = "SQL",
                    Category = "Database",
                    Tags = new[] { "SQL", "JOIN", "Self Join" },
                    CompanyTags = new[] { "Amazon", "Google", "Facebook" },
                    Constraints = "ManagerId can be NULL.\nAll employees have unique Ids.\nSalary values are positive integers.",
                    Examples = new[] {
                        new { Input = "Employee table:\n| Id | Name  | Salary | ManagerId |\n|----|-------|--------|-----------|\n| 1  | Joe   | 70000  | 3         |\n| 2  | Henry | 80000  | 4         |\n| 3  | Sam   | 60000  | NULL      |\n| 4  | Max   | 90000  | NULL      |", Output = "| Employee |\n|----------|\n| Joe      |", Explanation = (string?)"Joe is the only employee who earns more than his manager (Sam earns 60000, Joe earns 70000)." }
                    },
                    Hints = new[] { 
                        "This is a self-join problem. You need to join the Employee table with itself.",
                        "Think about joining where Employee.Id = Employee.ManagerId, then compare salaries.",
                        "Make sure to use table aliases to distinguish between the employee and manager records."
                    },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(n)",
                    AcceptanceRate = 62.8
                },
                new {
                    Title = "Rank Scores",
                    Description = "Write a SQL query to rank scores. If there is a tie between two scores, both should have the same ranking. Note that after a tie, the next ranking number should be the next consecutive integer value. In other words, there should be no \"holes\" between ranks.\n\nTable: Scores\n+----+-------+\n| Id | Score |\n+----+-------+\n| 1  | 3.50  |\n| 2  | 3.65  |\n| 3  | 4.00  |\n| 4  | 3.85  |\n| 5  | 4.00  |\n| 6  | 3.65  |\n+----+-------+",
                    Difficulty = "Medium",
                    QuestionType = "SQL",
                    Category = "Database",
                    Tags = new[] { "SQL", "Window Functions", "DENSE_RANK", "RANK" },
                    CompanyTags = new[] { "Microsoft", "Oracle", "Salesforce" },
                    Constraints = "Scores are floating point numbers.\nAll scores are unique or can have duplicates.",
                    Examples = new[] {
                        new { Input = "Scores table:\n| Id | Score |\n|----|-------|\n| 1  | 3.50  |\n| 2  | 3.65  |\n| 3  | 4.00  |\n| 4  | 3.85  |\n| 5  | 4.00  |\n| 6  | 3.65  |", Output = "| Score | Rank |\n|-------|------|\n| 4.00  | 1    |\n| 4.00  | 1    |\n| 3.85  | 2    |\n| 3.65  | 3    |\n| 3.65  | 3    |\n| 3.50  | 4    |", Explanation = (string?)"Scores are ranked from highest to lowest, with ties getting the same rank and no gaps in ranking." }
                    },
                    Hints = new[] { 
                        "You'll need to use window functions like DENSE_RANK() or RANK().",
                        "DENSE_RANK() gives consecutive ranks without gaps, which is what we need here.",
                        "Remember to order by Score in descending order to get the highest scores first."
                    },
                    TimeComplexityHint = "O(n log n)",
                    SpaceComplexityHint = "O(n)",
                    AcceptanceRate = 55.3
                },
                new {
                    Title = "Department Top Three Salaries",
                    Description = "A company's executives are interested in seeing who earns the most money in each of the company's departments. A high earner in a department is an employee who has a salary in the top three unique salaries for that department.\n\nWrite a SQL query to find the employees who are high earners in each of the departments.\n\nTable: Employee\n+----+-------+--------+--------------+\n| Id | Name  | Salary | DepartmentId |\n+----+-------+--------+--------------+\n| 1  | Joe   | 85000  | 1            |\n| 2  | Henry | 80000  | 2            |\n| 3  | Sam   | 60000  | 2            |\n| 4  | Max   | 90000  | 1            |\n| 5  | Janet | 69000  | 1            |\n| 6  | Randy | 85000  | 1            |\n| 7  | Will  | 70000  | 1            |\n+----+-------+--------+--------------+\n\nTable: Department\n+----+----------+\n| Id | Name     |\n+----+----------+\n| 1  | IT       |\n| 2  | Sales    |\n+----+----------+",
                    Difficulty = "Hard",
                    QuestionType = "SQL",
                    Category = "Database",
                    Tags = new[] { "SQL", "Window Functions", "DENSE_RANK", "JOIN", "Subquery" },
                    CompanyTags = new[] { "Google", "Amazon", "Microsoft" },
                    Constraints = "Each department has at least one employee.\nSalary values are positive integers.\nThere may be multiple employees with the same salary in a department.",
                    Examples = new[] {
                        new { Input = "Employee table:\n| Id | Name  | Salary | DepartmentId |\n|----|-------|--------|--------------|\n| 1  | Joe   | 85000  | 1            |\n| 2  | Henry | 80000  | 2            |\n| 3  | Sam   | 60000  | 2            |\n| 4  | Max   | 90000  | 1            |\n| 5  | Janet | 69000  | 1            |\n| 6  | Randy | 85000  | 1            |\n| 7  | Will  | 70000  | 1            |\n\nDepartment table:\n| Id | Name  |\n|----|-------|\n| 1  | IT    |\n| 2  | Sales |", Output = "| Department | Employee | Salary |\n|------------|----------|--------|\n| IT         | Max      | 90000  |\n| IT         | Joe      | 85000  |\n| IT         | Randy    | 85000  |\n| IT         | Will     | 70000  |\n| Sales      | Henry    | 80000  |\n| Sales      | Sam      | 60000  |", Explanation = (string?)"In the IT department, Max earns the highest unique salary (90000), Joe and Randy both earn the second-highest unique salary (85000), and Will earns the third-highest unique salary (70000). In the Sales department, Henry earns the highest salary (80000) and Sam earns the second-highest salary (60000)." }
                    },
                    Hints = new[] { 
                        "You'll need to use window functions like DENSE_RANK() partitioned by DepartmentId.",
                        "Join the Employee table with the Department table to get department names.",
                        "Filter the results where the rank is <= 3 to get the top 3 earners per department.",
                        "Remember that DENSE_RANK() handles ties correctly - employees with the same salary get the same rank."
                    },
                    TimeComplexityHint = "O(n log n)",
                    SpaceComplexityHint = "O(n)",
                    AcceptanceRate = 42.7
                },
                new {
                    Title = "Consecutive Numbers",
                    Description = "Write a SQL query to find all numbers that appear at least three times consecutively.\n\nTable: Logs\n+----+-----+\n| Id | Num |\n+----+-----+\n| 1  |  1  |\n| 2  |  1  |\n| 3  |  1  |\n| 4  |  2  |\n| 5  |  1  |\n| 6  |  2  |\n| 7  |  2  |\n+----+-----+",
                    Difficulty = "Medium",
                    QuestionType = "SQL",
                    Category = "Database",
                    Tags = new[] { "SQL", "Self Join", "Window Functions", "LAG", "LEAD" },
                    CompanyTags = new[] { "Amazon", "Microsoft", "Adobe" },
                    Constraints = "Id is an auto-increment primary key.\nNum values are integers.",
                    Examples = new[] {
                        new { Input = "Logs table:\n| Id | Num |\n|----|-----|\n| 1  |  1  |\n| 2  |  1  |\n| 3  |  1  |\n| 4  |  2  |\n| 5  |  1  |\n| 6  |  2  |\n| 7  |  2  |", Output = "| ConsecutiveNums |\n|-----------------|\n| 1               |", Explanation = (string?)"1 is the only number that appears consecutively at least three times." }
                    },
                    Hints = new[] { 
                        "You can use window functions like LAG() and LEAD() to compare a number with its previous and next values.",
                        "Alternatively, you can use self-joins to compare each row with the next two rows.",
                        "Think about checking if Num equals the previous Num AND equals the next Num.",
                        "Make sure to handle cases where there might be gaps in the Id sequence (though in this problem Ids are consecutive)."
                    },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(n)",
                    AcceptanceRate = 48.9
                },
                // ── System Design questions ──────────────────────────────────────
                new {
                    Title = "Design a URL Shortener",
                    Description = "Design a URL shortening service like bit.ly or TinyURL.\n\nThe system should:\n1. Take a long URL and return a shortened URL.\n2. Redirect users from the short URL to the original long URL.\n3. Handle high read traffic (redirection is much more frequent than shortening).\n\nEstimate roughly 100M new URLs per day and 10B redirects per day.",
                    Difficulty = "Medium",
                    QuestionType = "System Design",
                    Category = "System Design",
                    Tags = new[] { "System Design", "Scalability", "Hashing", "Caching", "Database" },
                    CompanyTags = new[] { "Google", "Amazon", "Facebook", "Twitter" },
                    Constraints = "Short URL must be unique.\nSystem must be highly available.\nURL redirection should happen with minimal latency.\nURLs can optionally have an expiration time.",
                    Examples = new[] {
                        new { Input = "longUrl = \"https://www.example.com/very/long/path?query=param\"", Output = "shortUrl = \"https://tinyurl.com/abcd123\"", Explanation = (string?)"The service maps the long URL to a 7-character alias." }
                    },
                    Hints = new[] {
                        "Start with clarifying requirements: do you need analytics? custom aliases? expiration?",
                        "For generating short codes, consider base62 encoding of an auto-increment ID or a hash of the URL.",
                        "Think about the database schema: a mapping table with short_code, long_url, created_at, expires_at.",
                        "For scale: use a CDN and cache popular redirects in Redis with high TTL. The DB only needs to be hit for cache misses.",
                        "To handle 10B redirects/day (~115K/sec), you need read replicas and a distributed cache. Writes are far fewer."
                    },
                    TimeComplexityHint = "O(1)",
                    SpaceComplexityHint = "O(n)",
                    AcceptanceRate = 71.0
                },
                new {
                    Title = "Design a Rate Limiter",
                    Description = "Design a rate limiter that throttles API requests at a per-user or per-IP level.\n\nRequirements:\n- Limit each user to N requests per time window (e.g. 100 req/min).\n- Return HTTP 429 when the limit is exceeded.\n- The system serves 10M users and handles 100K req/sec globally.\n- Latency overhead from rate limiting must be < 5 ms.",
                    Difficulty = "Hard",
                    QuestionType = "System Design",
                    Category = "System Design",
                    Tags = new[] { "System Design", "Rate Limiting", "Redis", "Distributed Systems", "Algorithms" },
                    CompanyTags = new[] { "Stripe", "Cloudflare", "Netflix", "Amazon" },
                    Constraints = "Must work in a distributed environment across multiple API servers.\nMust be accurate (exact or near-exact counting).\nMinimal impact on request latency.",
                    Examples = new[] {
                        new { Input = "user_id=42, limit=100 req/min", Output = "Allow or deny + Retry-After header", Explanation = (string?)"Track request counts in a sliding window and reject once limit is reached." }
                    },
                    Hints = new[] {
                        "Compare algorithms: Fixed Window, Sliding Window Log, Sliding Window Counter, Token Bucket, Leaky Bucket.",
                        "Token Bucket is widely used: allows bursting, easy to implement with Redis INCR + TTL.",
                        "For a distributed setup, use Redis atomic operations (INCR, EXPIRE) or Lua scripts to avoid race conditions.",
                        "Consider where to enforce the limit: API Gateway (centralized) vs. each service (decentralized).",
                        "Edge cases: what if Redis is down? Fail open (allow all) or fail closed (deny all)?"
                    },
                    TimeComplexityHint = "O(1)",
                    SpaceComplexityHint = "O(n)",
                    AcceptanceRate = 58.3
                },
                new {
                    Title = "Design a Notification System",
                    Description = "Design a push notification system that can deliver messages to millions of users across multiple channels (push, email, SMS).\n\nRequirements:\n- Support in-app push, email, and SMS notifications.\n- Send notifications to 10M users within 10 minutes of a trigger.\n- Guarantee at-least-once delivery.\n- Allow users to configure notification preferences (opt-out per channel/type).",
                    Difficulty = "Medium",
                    QuestionType = "System Design",
                    Category = "System Design",
                    Tags = new[] { "System Design", "Message Queue", "Push Notifications", "Scalability", "Kafka" },
                    CompanyTags = new[] { "Meta", "Airbnb", "Uber", "LinkedIn" },
                    Constraints = "Notifications must be delivered in near-real-time.\nSystem must be fault-tolerant; lost notifications are not acceptable.\nUsers must be able to opt out of specific notification types.",
                    Examples = new[] {
                        new { Input = "event = \"order_shipped\", user_ids = [1M users]", Output = "10M notifications sent within 10 min across email, SMS, push", Explanation = (string?)"Notification service fans out through a message queue to channel-specific workers." }
                    },
                    Hints = new[] {
                        "Decouple the trigger from delivery using a message queue (Kafka/SQS). The trigger publishes an event; workers consume and send.",
                        "Fan-out: one event → one message per (user, channel). Use a separate topic/queue per channel (push, email, SMS) for independent scaling.",
                        "Store a user preference table: user_id, channel, notification_type, opted_in. Check before sending.",
                        "For retry and at-least-once delivery, use dead-letter queues and idempotency keys to prevent duplicate sends.",
                        "Third-party providers: APNs/FCM for push, SendGrid for email, Twilio for SMS. Handle rate limits and failures per provider."
                    },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(n)",
                    AcceptanceRate = 64.5
                },
                new {
                    Title = "Design a Key-Value Store",
                    Description = "Design a distributed key-value store (like Redis or DynamoDB) that can handle millions of read/write operations per second.\n\nRequirements:\n- Support GET, PUT, DELETE operations.\n- Achieve sub-millisecond latency for reads.\n- Data should be persisted to disk.\n- The system must be fault-tolerant and horizontally scalable.\n- Eventual consistency is acceptable.",
                    Difficulty = "Hard",
                    QuestionType = "System Design",
                    Category = "System Design",
                    Tags = new[] { "System Design", "Distributed Systems", "CAP Theorem", "Consistent Hashing", "Replication" },
                    CompanyTags = new[] { "Amazon", "Google", "Microsoft", "Apple" },
                    Constraints = "Must scale to petabytes of data across hundreds of nodes.\nMust tolerate node failures without data loss.\nNetwork partitions will happen; choose your CAP trade-off.",
                    Examples = new[] {
                        new { Input = "PUT(key=\"user:42:name\", value=\"Alice\")", Output = "OK", Explanation = (string?)"Key-value pair stored in the appropriate shard based on consistent hashing." }
                    },
                    Hints = new[] {
                        "Use consistent hashing to distribute keys across nodes. This minimizes reshuffling when nodes join/leave.",
                        "Replication: replicate each key to N nodes (e.g. N=3) for fault tolerance. Use a coordinator pattern for reads/writes.",
                        "For writes: use a Write-Ahead Log (WAL) for durability. Periodic compaction (SSTable/LSM-tree) keeps performance high.",
                        "For reads: serve from memory (LRU cache) for hot keys. On cache miss, read from disk.",
                        "Conflict resolution for eventual consistency: use vector clocks or last-write-wins (LWW) with timestamps."
                    },
                    TimeComplexityHint = "O(1)",
                    SpaceComplexityHint = "O(n)",
                    AcceptanceRate = 52.1
                }
            };

            int questionIndex = 0;
            foreach (var qData in questionsData)
            {
                // Skip if question already exists (by title)
                if (allExistingTitles.Contains(qData.Title))
                {
                    questionIndex++;
                    continue;
                }
                
                // Approve first 8 questions by default, all SQL questions, and all System Design questions
                bool isApproved = questionIndex < 8 || qData.QuestionType == "SQL" || qData.QuestionType == "System Design";
                
                var question = new InterviewQuestion
                {
                    Id = Guid.NewGuid(),
                    Title = qData.Title,
                    Description = qData.Description,
                    Difficulty = qData.Difficulty,
                    QuestionType = qData.QuestionType,
                    Category = qData.Category,
                    Tags = System.Text.Json.JsonSerializer.Serialize(qData.Tags),
                    CompanyTags = System.Text.Json.JsonSerializer.Serialize(qData.CompanyTags),
                    Constraints = qData.Constraints,
                    Examples = System.Text.Json.JsonSerializer.Serialize(qData.Examples),
                    Hints = System.Text.Json.JsonSerializer.Serialize(qData.Hints),
                    TimeComplexityHint = qData.TimeComplexityHint,
                    SpaceComplexityHint = qData.SpaceComplexityHint,
                    AcceptanceRate = qData.AcceptanceRate,
                    IsActive = true,
                    ApprovalStatus = isApproved ? "Approved" : "Pending",
                    ApprovedBy = isApproved ? createdBy : null,
                    ApprovedAt = isApproved ? now : (DateTime?)null,
                    CreatedBy = createdBy,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                questions.Add(question);
                questionIndex++;
            }

            // Seed Behavioral + Product Management questions (Exponent-style)
            var nonCodingQuestionsToAdd = BuildNonCodingSeedQuestions(createdBy, now, allExistingTitles);
            if (nonCodingQuestionsToAdd.Any())
            {
                questions.AddRange(nonCodingQuestionsToAdd);
            }

            context.InterviewQuestions.AddRange(questions);
            await context.SaveChangesAsync();

            // Fix misclassified questions (even if referenced, so they show under correct category)
            var categoryFixes = new[] {
                (Title: "Design a document processing pipeline.", QuestionType: "System Design", Category: "System Design"),
                (Title: "Find the number of users who called three or more people in the last week.", QuestionType: "SQL", Category: "Database"),
            };
            foreach (var (title, newType, newCategory) in categoryFixes)
            {
                var toFix = await context.InterviewQuestions.FirstOrDefaultAsync(q => q.Title == title);
                if (toFix != null && (toFix.QuestionType != newType || toFix.Category != newCategory))
                {
                    toFix.QuestionType = newType;
                    toFix.Category = newCategory;
                    toFix.UpdatedAt = DateTime.UtcNow;
                    logger.LogInformation("Updated question category: {Title} -> {Type}", title, newType);
                }
            }
            await context.SaveChangesAsync();

            // Now add test cases and solutions for each question
            await SeedQuestionTestCasesAndSolutions(context, logger, questions, createdBy);

            // Seed a few default "how to answer" comments for non-coding questions
            await SeedDefaultQuestionComments(context, logger, createdBy);

            logger.LogInformation("✅ Seeded {Count} interview questions", questions.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed interview questions, but this is non-critical. Continuing...");
        }
    }

    private static List<InterviewQuestion> BuildNonCodingSeedQuestions(Guid? createdBy, DateTime now, List<string> allExistingTitles)
    {
        var seedVideoUrl = "https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/videos/mock-interviews/what-is-exponent.mp4";

        var behavioral = new List<InterviewQuestion>
        {
            NewNonCodingQuestion("Tell me about a time when you made short-term sacrifices for long-term gains.", "Behavioral", "Behavioral",
                roles: new[] { "Product Manager", "Software Engineer" },
                companies: new[] { "Amazon", "Google" },
                tags: new[] { "Ownership", "Long-Term Thinking" },
                hints: new[] {
                    "Use STAR: Situation, Task, Action, Result. Emphasize what you gave up and why it mattered.",
                    "Quantify impact and explain trade-offs; close with what you learned."
                },
                videoUrl: seedVideoUrl),

            NewNonCodingQuestion("Tell me about something you built end-to-end without relying on others.", "Behavioral", "Behavioral",
                roles: new[] { "Product Manager", "Software Engineer" },
                companies: new[] { "Google", "Meta" },
                tags: new[] { "Bias for Action", "Deliver Results" },
                hints: new[] {
                    "Pick a scoped example where you owned requirements → execution → launch.",
                    "Highlight how you unblocked yourself and validated outcomes."
                }),

            NewNonCodingQuestion("Tell me about a time when you handled a difficult stakeholder.", "Behavioral", "Behavioral",
                roles: new[] { "Product Manager", "Customer Success Manager" },
                companies: new[] { "Meta", "Amazon", "Stripe" },
                tags: new[] { "Stakeholder Management", "Communication" },
                hints: new[] {
                    "Show how you aligned on goals, communicated trade-offs, and built trust.",
                    "Mention how you handled pushback and kept decisions data-driven."
                }),

            NewNonCodingQuestion("Tell me about a time you dropped the ball on something.", "Behavioral", "Behavioral",
                roles: new[] { "Product Manager", "Software Engineer" },
                companies: new[] { "Google", "Microsoft" },
                tags: new[] { "Accountability", "Learn and Be Curious" },
                hints: new[] {
                    "Own the mistake, explain what changed, and show the prevention mechanism you implemented."
                }),

            NewNonCodingQuestion("Tell me about a time you gained trust.", "Behavioral", "Behavioral",
                roles: new[] { "Product Manager", "Engineering Manager" },
                companies: new[] { "Amazon", "Apple" },
                tags: new[] { "Trust", "Leadership" }),

            NewNonCodingQuestion("Tell me about a time you had to solve a difficult problem.", "Behavioral", "Behavioral",
                roles: new[] { "Software Engineer", "Product Manager" },
                companies: new[] { "Amazon", "Stripe" },
                tags: new[] { "Problem Solving", "Execution" }),

            NewNonCodingQuestion("Tell me about a time you disagreed with your manager.", "Behavioral", "Behavioral",
                roles: new[] { "Product Manager", "Software Engineer" },
                companies: new[] { "Meta", "Netflix" },
                tags: new[] { "Disagree and Commit", "Communication" }),

            NewNonCodingQuestion("Tell me about a time you influenced without authority.", "Behavioral", "Behavioral",
                roles: new[] { "Product Manager", "Technical Program Manager" },
                companies: new[] { "Google", "Amazon" },
                tags: new[] { "Influence", "Leadership" }),

            NewNonCodingQuestion("Tell me about a time you improved a process.", "Behavioral", "Behavioral",
                roles: new[] { "Product Manager", "Software Engineer" },
                companies: new[] { "Microsoft", "Amazon" },
                tags: new[] { "Operational Excellence", "Ownership" }),

            NewNonCodingQuestion("Tell me about a time you received critical feedback.", "Behavioral", "Behavioral",
                roles: new[] { "Product Manager", "Software Engineer" },
                companies: new[] { "Google", "Apple" },
                tags: new[] { "Growth Mindset", "Coachability" }),

            NewNonCodingQuestion("Tell me about a time you had to make a decision with incomplete data.", "Behavioral", "Behavioral",
                roles: new[] { "Product Manager" },
                companies: new[] { "Amazon", "Meta" },
                tags: new[] { "Decision Making", "Judgment" }),

            NewNonCodingQuestion("Tell me about a time you managed a difficult customer.", "Behavioral", "Behavioral",
                roles: new[] { "Customer Success Manager", "Product Manager" },
                companies: new[] { "Amazon", "Stripe" },
                tags: new[] { "Customer Obsession", "Communication" }),

            // Role-specific behavioral questions (so the role filter is meaningfully populated)
            NewNonCodingQuestion("Tell me about a time you debugged a production incident.", "Behavioral", "Behavioral",
                roles: new[] { "Software Engineer" },
                companies: new[] { "Amazon", "Meta", "Google" },
                tags: new[] { "Incident Response", "Ownership" },
                hints: new[] {
                    "Set the scene (impact + urgency), then walk through your triage steps: isolate, mitigate, root-cause, prevent.",
                    "Emphasize communication: who you updated, how often, and what you learned after the postmortem."
                }),

            NewNonCodingQuestion("Tell me about a time you improved data quality or reliability.", "Behavioral", "Behavioral",
                roles: new[] { "Data Engineer" },
                companies: new[] { "Stripe", "Netflix", "Amazon" },
                tags: new[] { "Data Quality", "Operational Excellence" },
                hints: new[] {
                    "Describe the failure mode (bad schema, late data, broken pipeline) and how you detected it (alerts, checks, dashboards).",
                    "Explain the fix (contracts, validations, backfills) and how you prevented regressions."
                }),

            NewNonCodingQuestion("Tell me about an experiment you designed and how you interpreted the results.", "Behavioral", "Behavioral",
                roles: new[] { "Data Scientist" },
                companies: new[] { "Meta", "Google", "Microsoft" },
                tags: new[] { "Experimentation", "Decision Making" },
                hints: new[] {
                    "Start with the decision you needed to make, then define hypothesis, metrics, and guardrails.",
                    "Call out pitfalls (sample ratio mismatch, novelty effects) and how you validated the readout."
                }),

            NewNonCodingQuestion("Tell me about a model you shipped to production and how you monitored it.", "Behavioral", "Behavioral",
                roles: new[] { "Machine Learning Engineer" },
                companies: new[] { "Google", "Amazon", "Netflix" },
                tags: new[] { "ML in Production", "Monitoring" },
                hints: new[] {
                    "Cover the end-to-end path: data → training → evaluation → deployment → rollback plan.",
                    "Mention monitoring signals (latency, drift, performance, bias) and how you closed the loop with retraining."
                }),

            NewNonCodingQuestion("Tell me about a time you managed conflict on your team.", "Behavioral", "Behavioral",
                roles: new[] { "Engineering Manager" },
                companies: new[] { "Amazon", "Microsoft", "Apple" },
                tags: new[] { "People Leadership", "Communication" },
                hints: new[] {
                    "Focus on the root cause (goals, incentives, ambiguity), then describe how you facilitated alignment.",
                    "Explain the decision, how you got commitment, and what changed afterwards."
                }),

            NewNonCodingQuestion("Tell me about a time you drove alignment across multiple teams with competing priorities.", "Behavioral", "Behavioral",
                roles: new[] { "Technical Program Manager" },
                companies: new[] { "Google", "Amazon", "Meta" },
                tags: new[] { "Cross-Functional Leadership", "Program Management" },
                hints: new[] {
                    "Show your approach: clarify goals, map stakeholders, surface trade-offs, and write down decisions.",
                    "Highlight how you tracked risks/dependencies and kept execs and teams unblocked."
                }),

            NewNonCodingQuestion("Why do you think we should not hire you?", "Behavioral", "Behavioral",
                roles: new[] { "Software Engineer", "Data Engineer", "Data Scientist" },
                companies: new[] { "Amazon", "Google", "Meta" },
                tags: new[] { "Self-Awareness", "Growth Mindset" },
                hints: new[] {
                    "Pick one real weakness (not a humblebrag), then show what you’ve done to mitigate it.",
                    "Keep it specific: what you changed, how you measure improvement, and why it won’t block success in this role."
                }),

            NewNonCodingQuestion("Design a document processing pipeline.", "System Design", "System Design",
                roles: new[] { "Data Engineer" },
                companies: new[] { "Amazon", "Google", "Microsoft" },
                tags: new[] { "Pipeline Design", "Reliability" },
                hints: new[] {
                    "Clarify inputs/outputs, scale, SLAs, and failure modes before proposing architecture.",
                    "Walk through ingestion → parsing/OCR → enrichment → storage/indexing → serving → monitoring (retries, idempotency, backfills)."
                }),

            NewNonCodingQuestion("Find the number of users who called three or more people in the last week.", "SQL", "Database",
                roles: new[] { "Data Scientist", "Data Engineer" },
                companies: new[] { "Meta", "Google", "Amazon" },
                tags: new[] { "Analytics", "Structured Thinking" },
                hints: new[] {
                    "Define the event schema (caller, callee, timestamp) and the time window (timezone, inclusive bounds).",
                    "Count distinct callees per caller in the last 7 days, then count callers with >= 3. Call out edge cases (spam, self-calls)."
                }),
        };

        var productManagement = new List<InterviewQuestion>
        {
            NewNonCodingQuestion("How do you decide what to build next?", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Google", "Meta", "Amazon" },
                tags: new[] { "Prioritization", "Strategy" },
                hints: new[] {
                    "Discuss inputs (user research, data, strategy), then how you frame trade-offs (impact vs effort).",
                    "Mention alignment (eng/design/stakeholders) and how you communicate decisions."
                }),

            NewNonCodingQuestion("Walk me through launching a new product from scratch.", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Amazon", "Microsoft" },
                tags: new[] { "Go-To-Market", "Execution" },
                videoUrl: seedVideoUrl),

            NewNonCodingQuestion("How would you improve our search feature?", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Google", "Amazon" },
                tags: new[] { "Product Sense", "Metrics" },
                hints: new[] {
                    "Start with goals and users, define success metrics, propose hypotheses, and outline experiments."
                }),

            NewNonCodingQuestion("How do you define and track success for a feature?", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Meta", "Stripe" },
                tags: new[] { "Metrics", "Analytics" }),

            NewNonCodingQuestion("How do you handle trade-offs between growth and retention?", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Netflix", "Meta" },
                tags: new[] { "Strategy", "Experimentation" }),

            NewNonCodingQuestion("Tell me about a product you love and how you’d improve it.", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Apple", "Google" },
                tags: new[] { "Product Thinking", "User Empathy" }),

            NewNonCodingQuestion("How do you prioritize bugs vs new features?", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Amazon", "Microsoft" },
                tags: new[] { "Quality", "Prioritization" }),

            NewNonCodingQuestion("How do you write a good PRD?", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Google", "Meta" },
                tags: new[] { "Requirements", "Communication" }),

            NewNonCodingQuestion("How would you measure and improve onboarding?", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Stripe", "Amazon" },
                tags: new[] { "Funnels", "Activation" }),

            NewNonCodingQuestion("How do you partner with engineering when timelines slip?", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Meta", "Google" },
                tags: new[] { "Execution", "Stakeholder Management" },
                hints: new[] {
                    "Discuss re-scoping, sequencing, risk management, and stakeholder communication."
                }),

            NewNonCodingQuestion("How do you handle conflicting stakeholder requests?", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Amazon", "Google" },
                tags: new[] { "Stakeholder Management", "Prioritization" }),

            NewNonCodingQuestion("Design a product for busy parents to manage family scheduling.", "Product Management", "Product Management",
                roles: new[] { "Product Manager" },
                companies: new[] { "Google", "Meta" },
                tags: new[] { "Product Design", "User Research" }),

            NewNonCodingQuestion("As the PM for Lyft, what dashboard would you build to track the health of the app?", "Product Management", "Product Management",
                roles: new[] { "Product Manager", "Data Scientist" },
                companies: new[] { "Lyft", "Meta", "Google" },
                tags: new[] { "Dashboards", "Metrics" },
                hints: new[] {
                    "Start by defining “health” for each user group (riders, drivers) and the business (revenue, reliability).",
                    "Propose a layered dashboard: top-line KPIs, funnels, quality/reliability, geography/time breakdowns, and alerts."
                }),
        };

        // Remove duplicates by title against existing DB state
        behavioral = behavioral.Where(q => !allExistingTitles.Contains(q.Title)).ToList();
        productManagement = productManagement.Where(q => !allExistingTitles.Contains(q.Title)).ToList();

        // Related questions (2 per question, within the same type)
        SetRelatedQuestions(behavioral);
        SetRelatedQuestions(productManagement);

        var all = behavioral.Concat(productManagement).ToList();

        // Apply admin metadata
        foreach (var q in all)
        {
            q.IsActive = true;
            q.ApprovalStatus = "Approved";
            q.ApprovedBy = createdBy;
            q.ApprovedAt = createdBy != null ? now : (DateTime?)null;
            q.CreatedBy = createdBy;
            q.CreatedAt = now;
            q.UpdatedAt = now;
        }

        return all;

        InterviewQuestion NewNonCodingQuestion(
            string title,
            string questionType,
            string category,
            string[] roles,
            string[] companies,
            string[] tags,
            string[]? hints = null,
            string? videoUrl = null)
        {
            return new InterviewQuestion
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = title, // For non-coding questions we use Description as the question prompt
                Difficulty = "Medium",
                QuestionType = questionType,
                Category = category,
                CompanyTags = JsonSerializer.Serialize(companies),
                Tags = JsonSerializer.Serialize(tags),
                Hints = hints != null ? JsonSerializer.Serialize(hints) : null,
                VideoUrl = videoUrl,
                RoleTags = JsonSerializer.Serialize(roles),
                Constraints = null,
                Examples = null,
                TimeComplexityHint = null,
                SpaceComplexityHint = null,
                AcceptanceRate = null
            };
        }

        void SetRelatedQuestions(List<InterviewQuestion> group)
        {
            if (group.Count < 3) return;

            for (int i = 0; i < group.Count; i++)
            {
                var related = new List<Guid>
                {
                    group[(i + 1) % group.Count].Id,
                    group[(i + 2) % group.Count].Id
                };
                group[i].RelatedQuestionIds = JsonSerializer.Serialize(related);
            }
        }
    }

    private static async Task SeedDefaultQuestionComments(ApplicationDbContext context, ILogger logger, Guid? createdBy)
    {
        if (createdBy is null)
        {
            // If we don't have an admin user, skip seeding comments (comments require a user).
            return;
        }

        try
        {
            var adminUserId = createdBy.Value;
            var now = DateTime.UtcNow;
            const string prefix = "Vector tip:";

            var seeds = new[]
            {
                new
                {
                    Title = "Tell me about a time you debugged a production incident.",
                    Content =
                        $"{prefix} A strong incident story is about *process*, not heroics.\n\n" +
                        "- Situation: what broke and who it affected\n" +
                        "- Actions: triage → mitigate → root cause → follow-ups\n" +
                        "- Result: measurable impact (time-to-mitigate, prevented recurrence)\n" +
                        "- Communication: who you updated and how you coordinated"
                },
                new
                {
                    Title = "Tell me about a time you improved data quality or reliability.",
                    Content =
                        $"{prefix} Lead with the failure mode and detection, then prevention.\n\n" +
                        "- What was wrong (schema drift, late data, bad joins, missing backfills)\n" +
                        "- How you detected it (alerts, checks, SLAs)\n" +
                        "- Fix + prevention (contracts, validations, ownership, runbooks)\n" +
                        "- Outcome (reduced incidents, improved trust)"
                },
                new
                {
                    Title = "Tell me about an experiment you designed and how you interpreted the results.",
                    Content =
                        $"{prefix} Make the *decision* the star.\n\n" +
                        "- Hypothesis + primary metric + guardrails\n" +
                        "- Design details (unit of randomization, duration, power)\n" +
                        "- Readout (confidence, segments, pitfalls)\n" +
                        "- What you recommended and what shipped"
                },
                new
                {
                    Title = "Tell me about a model you shipped to production and how you monitored it.",
                    Content =
                        $"{prefix} Interviewers want to hear the full lifecycle.\n\n" +
                        "- Data + labeling + evaluation\n" +
                        "- Deployment (A/B, shadow, canary) + rollback plan\n" +
                        "- Monitoring (latency, drift, performance, bias)\n" +
                        "- Iteration loop (retraining, feature updates)"
                },
                new
                {
                    Title = "Tell me about a time you managed conflict on your team.",
                    Content =
                        $"{prefix} Show that you reduced ambiguity and rebuilt trust.\n\n" +
                        "- Diagnose the root cause (goals, roles, incentives)\n" +
                        "- Facilitate alignment (1:1s, shared doc, clear decision owner)\n" +
                        "- Set expectations and follow up\n" +
                        "- Outcome (better delivery, healthier collaboration)"
                },
                new
                {
                    Title = "Tell me about a time you drove alignment across multiple teams with competing priorities.",
                    Content =
                        $"{prefix} Anchor on clarity, trade-offs, and a durable plan.\n\n" +
                        "- North star + scope boundaries\n" +
                        "- Stakeholder map + decision log\n" +
                        "- Dependency/risk tracking + escalation path\n" +
                        "- Outcome (launch, de-risked timeline, avoided churn)"
                },
                new
                {
                    Title = "Why do you think we should not hire you?",
                    Content =
                        $"{prefix} This question is about *judgment* and *self-awareness*.\n\n" +
                        "- Choose one real gap that matters (skill, behavior, experience)\n" +
                        "- Give a concrete example of when it showed up\n" +
                        "- Explain what you changed (systems, habits, training)\n" +
                        "- Close with why you’re still a strong fit for this role"
                },
                new
                {
                    Title = "Design a document processing pipeline.",
                    Content =
                        $"{prefix} Don’t jump to tech—start with requirements and failure modes.\n\n" +
                        "- Inputs (PDFs, images), volume, latency, accuracy targets\n" +
                        "- Ingestion + queue + workers (idempotency, retries, DLQ)\n" +
                        "- Parsing/OCR + enrichment + validation\n" +
                        "- Storage/index + serving layer\n" +
                        "- Monitoring, backfills, and data quality checks"
                },
                new
                {
                    Title = "Find the number of users who called three or more people in the last week.",
                    Content =
                        $"{prefix} Even if you don’t write SQL, narrate the plan clearly.\n\n" +
                        "- Define the event table (caller_id, callee_id, timestamp)\n" +
                        "- Filter to last 7 days (timezone + boundaries)\n" +
                        "- Count *distinct* callees per caller\n" +
                        "- Count callers where distinct_callees >= 3\n" +
                        "- Mention edge cases (duplicates, spam, self-calls)"
                },
                new
                {
                    Title = "As the PM for Lyft, what dashboard would you build to track the health of the app?",
                    Content =
                        $"{prefix} Great dashboards answer: “Are we healthy?” and “Where is it broken?”\n\n" +
                        "- Pick personas (rider, driver, ops) and define “health” for each\n" +
                        "- Top-line KPIs + funnels + reliability/quality metrics\n" +
                        "- Breakdowns (geo, platform, time) and alert thresholds\n" +
                        "- Call out what you would *not* include to keep it focused"
                },
                new
                {
                    Title = "Design a product for busy parents to manage family scheduling.",
                    Content =
                        $"{prefix} Start with a sharp user + problem, then explore constraints.\n\n" +
                        "- Who: single parent vs two-parent household vs caregivers\n" +
                        "- Jobs-to-be-done: coordination, reminders, handoffs, visibility\n" +
                        "- MVP: shared calendar + roles + smart reminders\n" +
                        "- Differentiators: conflict resolution, routines, integrations, privacy\n" +
                        "- Metrics: weekly active families, tasks completed, missed events"
                },
            };

            var titles = seeds.Select(s => s.Title).ToList();
            var questionRows = await context.InterviewQuestions
                .Where(q => titles.Contains(q.Title))
                .Select(q => new { q.Id, q.Title })
                .ToListAsync();

            var questionIdByTitle = questionRows.ToDictionary(q => q.Title, q => q.Id);
            var questionIds = questionRows.Select(q => q.Id).ToList();

            if (!questionIds.Any())
            {
                return;
            }

            var alreadySeededQuestionIds = await context.InterviewQuestionComments
                .Where(c => c.UserId == adminUserId && questionIds.Contains(c.QuestionId))
                .Select(c => c.QuestionId)
                .Distinct()
                .ToListAsync();

            var alreadySeededSet = alreadySeededQuestionIds.ToHashSet();
            var toAdd = new List<InterviewQuestionComment>();

            foreach (var seed in seeds)
            {
                if (!questionIdByTitle.TryGetValue(seed.Title, out var qid)) continue;
                if (alreadySeededSet.Contains(qid)) continue;

                toAdd.Add(new InterviewQuestionComment
                {
                    Id = Guid.NewGuid(),
                    QuestionId = qid,
                    UserId = adminUserId,
                    Content = seed.Content,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            // Add one example reply thread (optional) to demonstrate structure
            var incidentParent = toAdd.FirstOrDefault(c => questionIdByTitle.TryGetValue("Tell me about a time you debugged a production incident.", out var qid) && c.QuestionId == qid);
            if (incidentParent != null)
            {
                toAdd.Add(new InterviewQuestionComment
                {
                    Id = Guid.NewGuid(),
                    QuestionId = incidentParent.QuestionId,
                    UserId = adminUserId,
                    ParentCommentId = incidentParent.Id,
                    Content = $"{prefix} Common pitfall: skipping the *prevention* step. Always end with what you changed so it won’t happen again.",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            var whyNotHireParent = toAdd.FirstOrDefault(c => questionIdByTitle.TryGetValue("Why do you think we should not hire you?", out var qid) && c.QuestionId == qid);
            if (whyNotHireParent != null)
            {
                toAdd.Add(new InterviewQuestionComment
                {
                    Id = Guid.NewGuid(),
                    QuestionId = whyNotHireParent.QuestionId,
                    UserId = adminUserId,
                    ParentCommentId = whyNotHireParent.Id,
                    Content = $"{prefix} Avoid “I’m a perfectionist.” Pick something real, keep it bounded, and show your mitigation strategy.",
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            if (toAdd.Any())
            {
                context.InterviewQuestionComments.AddRange(toAdd);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed default question comments (non-critical). Continuing...");
        }
    }

    /// <summary>
    /// Seeds test cases and solutions for questions
    /// </summary>
    public static async Task SeedQuestionTestCasesAndSolutions(
        ApplicationDbContext context, 
        ILogger logger, 
        List<InterviewQuestion> questions,
        Guid? createdBy)
    {
        try
        {
            var testCases = new List<QuestionTestCase>();
            var solutions = new List<QuestionSolution>();
            var now = DateTime.UtcNow;

            // Two Sum test cases and solution - check both new questions and existing ones
            var twoSum = questions.FirstOrDefault(q => q.Title == "Two Sum");
            if (twoSum == null)
            {
                twoSum = await context.InterviewQuestions
                    .FirstOrDefaultAsync(q => q.Title == "Two Sum");
            }
            
            if (twoSum == null)
            {
                logger.LogWarning("Two Sum question not found, skipping Two Sum test cases");
            }
            else
            {
            
            // Two Sum: Add exactly 10 test cases (3 visible + 7 hidden)
            // Generate large input test case (array with 1000 elements for performance testing)
            var largeNums = new List<int>();
            for (int i = 0; i < 1000; i++)
            {
                largeNums.Add(i);
            }
            var largeInput = $"{{\"nums\": [{string.Join(",", largeNums)}], \"target\": 1997}}";
            
            // Two Sum test cases: 3 visible + 7 hidden = 10 total
            var twoSumTestCases = new List<QuestionTestCase>
            {
                // 3 visible test cases
                new QuestionTestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = twoSum.Id,
                    TestCaseNumber = 1,
                    Input = "{\"nums\": [2,7,11,15], \"target\": 9}",
                    ExpectedOutput = "[0,1]",
                    IsHidden = false,
                    CreatedAt = now
                },
                new QuestionTestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = twoSum.Id,
                    TestCaseNumber = 2,
                    Input = "{\"nums\": [3,2,4], \"target\": 6}",
                    ExpectedOutput = "[1,2]",
                    IsHidden = false,
                    CreatedAt = now
                },
                new QuestionTestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = twoSum.Id,
                    TestCaseNumber = 3,
                    Input = "{\"nums\": [3,3], \"target\": 6}",
                    ExpectedOutput = "[0,1]",
                    IsHidden = false,
                    CreatedAt = now
                },
                // 7 hidden test cases
                new QuestionTestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = twoSum.Id,
                    TestCaseNumber = 4,
                    Input = "{\"nums\": [-1,-2,-3,-4,-5], \"target\": -8}",
                    ExpectedOutput = "[2,4]",
                    IsHidden = true,
                    CreatedAt = now
                },
                new QuestionTestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = twoSum.Id,
                    TestCaseNumber = 5,
                    Input = "{\"nums\": [1,1,1,1,1,4,1,1,1,1,1,7,1,1,1,1,1], \"target\": 11}",
                    ExpectedOutput = "[5,11]",
                    IsHidden = true,
                    CreatedAt = now
                },
                new QuestionTestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = twoSum.Id,
                    TestCaseNumber = 6,
                    Input = "{\"nums\": [0,4,3,0], \"target\": 0}",
                    ExpectedOutput = "[0,3]",
                    IsHidden = true,
                    CreatedAt = now
                },
                new QuestionTestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = twoSum.Id,
                    TestCaseNumber = 7,
                    Input = "{\"nums\": [1,5,3,7,9,2,8,4,6], \"target\": 10}",
                    ExpectedOutput = "[2,3]",
                    IsHidden = true,
                    CreatedAt = now
                },
                new QuestionTestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = twoSum.Id,
                    TestCaseNumber = 8,
                    Input = "{\"nums\": [-10,10,-5,5,0], \"target\": 0}",
                    ExpectedOutput = "[0,1]",
                    IsHidden = true,
                    CreatedAt = now
                },
                new QuestionTestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = twoSum.Id,
                    TestCaseNumber = 9,
                    Input = "{\"nums\": [100,200,300,400,500], \"target\": 600}",
                    ExpectedOutput = "[1,3]",
                    IsHidden = true,
                    CreatedAt = now
                },
                new QuestionTestCase
                {
                    Id = Guid.NewGuid(),
                    QuestionId = twoSum.Id,
                    TestCaseNumber = 10,
                    Input = largeInput,
                    ExpectedOutput = "[998,999]",
                    IsHidden = true,
                    CreatedAt = now
                }
            };
            
                // Check if Two Sum test cases already exist
                var existingTwoSumTestCases = await context.QuestionTestCases
                    .CountAsync(tc => tc.QuestionId == twoSum.Id);
                
                if (existingTwoSumTestCases == 0)
                {
                    testCases.AddRange(twoSumTestCases);
                }

                // Check if Two Sum solutions already exist
                var existingTwoSumSolutions = await context.QuestionSolutions
                    .CountAsync(s => s.QuestionId == twoSum.Id);
                
                if (existingTwoSumSolutions == 0)
                {
                    solutions.Add(new QuestionSolution
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = twoSum.Id,
                        Language = "JavaScript",
                Code = "function twoSum(nums, target) {\n    const numToIndex = {};\n    for (let i = 0; i < nums.length; i++) {\n        const complement = target - nums[i];\n        if (complement in numToIndex) {\n            return [numToIndex[complement], i];\n        }\n        numToIndex[nums[i]] = i;\n    }\n    return [];\n}",
                Explanation = "Solution 1: Hash map approach\nOur solution processes the input array by iterating through it while maintaining a hash map of previously seen elements and their indices. For each element, it calculates the complement (i.e., target - nums[i]) and checks if this complement is already present in the hash map. If the complement is found, it means we have identified a pair of indices that sum up to the target, and we return these indices. If no such pair is found by the end of the iteration, we return an empty array.\n\nThe solution uses a hash map (or unordered map) to achieve efficient lookups and insertions. This approach ensures that each element is processed only once, making it both readable and performant.\n\nTime Complexity: The solution has a time complexity of O(n), where n is the number of elements in the array. This is because we iterate through the array once, performing constant-time operations (hash map lookups and insertions) for each element.\n\nSpace Complexity: The solution uses O(n) space for the hash map that stores the indices of the elements. In the worst case, if all elements are unique, the hash map will contain n entries.",
                TimeComplexity = "O(n)",
                SpaceComplexity = "O(n)",
                IsOfficial = true,
                CreatedAt = now,
                UpdatedAt = now
            });

                    solutions.Add(new QuestionSolution
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = twoSum.Id,
                        Language = "Python",
                        Code = "def twoSum(nums, target):\n    hashmap = {}\n    for i, num in enumerate(nums):\n        complement = target - num\n        if complement in hashmap:\n            return [hashmap[complement], i]\n        hashmap[num] = i\n    return []",
                        Explanation = "Python implementation using dictionary for O(1) lookups.",
                        TimeComplexity = "O(n)",
                        SpaceComplexity = "O(n)",
                        IsOfficial = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            // Add comprehensive test cases and solutions for all other questions
            var questionTestCasesMap = new Dictionary<string, List<(string Input, string ExpectedOutput, bool IsHidden)>>();
            var questionSolutionsMap = new Dictionary<string, Dictionary<string, string>>();

            // Add Two Numbers
            var addTwoNumbers = questions.FirstOrDefault(q => q.Title == "Add Two Numbers");
            if (addTwoNumbers == null)
            {
                addTwoNumbers = await context.InterviewQuestions
                    .FirstOrDefaultAsync(q => q.Title == "Add Two Numbers");
            }
            if (addTwoNumbers != null)
            {
                questionTestCasesMap["Add Two Numbers"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases
                    ("{\"l1\": [2,4,3], \"l2\": [5,6,4]}", "[7,0,8]", false),
                    ("{\"l1\": [0], \"l2\": [0]}", "[0]", false),
                    ("{\"l1\": [9,9,9,9,9,9,9], \"l2\": [9,9,9,9]}", "[8,9,9,9,0,0,0,1]", false),
                    // 7 hidden test cases
                    ("{\"l1\": [1], \"l2\": [9,9,9,9,9,9,9,9,9,9]}", "[0,0,0,0,0,0,0,0,0,0,1]", true),
                    ("{\"l1\": [5,6,4], \"l2\": [2,4,3]}", "[7,0,8]", true),
                    ("{\"l1\": [1,8], \"l2\": [0]}", "[1,8]", true),
                    ("{\"l1\": [9,9,9], \"l2\": [1]}", "[0,0,0,1]", true),
                    ("{\"l1\": [5], \"l2\": [5]}", "[0,1]", true),
                    ("{\"l1\": [1,2,3,4,5], \"l2\": [5,4,3,2,1]}", "[6,6,6,6,6]", true),
                    ("{\"l1\": [9], \"l2\": [9,9,9,9,9]}", "[8,0,0,0,0,1]", true)
                };
                questionSolutionsMap["Add Two Numbers"] = new Dictionary<string, string>
                {
                    ["JavaScript"] = @"class ListNode {
    constructor(val, next = null) {
        this.val = val;
        this.next = next;
    }
}

function addTwoNumbers(l1, l2) {
    let dummy = new ListNode(0);
    let current = dummy;
    let carry = 0;
    
    while (l1 || l2 || carry) {
        let sum = (l1?.val || 0) + (l2?.val || 0) + carry;
        carry = Math.floor(sum / 10);
        current.next = new ListNode(sum % 10);
        current = current.next;
        l1 = l1?.next;
        l2 = l2?.next;
    }
    
    return dummy.next;
}",
                    ["Python"] = @"class ListNode:
    def __init__(self, val=0, next=None):
        self.val = val
        self.next = next

def addTwoNumbers(l1, l2):
    dummy = ListNode(0)
    current = dummy
    carry = 0
    
    while l1 or l2 or carry:
        sum_val = (l1.val if l1 else 0) + (l2.val if l2 else 0) + carry
        carry = sum_val // 10
        current.next = ListNode(sum_val % 10)
        current = current.next
        l1 = l1.next if l1 else None
        l2 = l2.next if l2 else None
    
    return dummy.next",
                    ["Java"] = @"public class ListNode {
    int val;
    ListNode next;
    ListNode() {}
    ListNode(int val) { this.val = val; }
    ListNode(int val, ListNode next) { this.val = val; this.next = next; }
}

public class Solution {
    public ListNode addTwoNumbers(ListNode l1, ListNode l2) {
        ListNode dummy = new ListNode(0);
        ListNode current = dummy;
        int carry = 0;
        
        while (l1 != null || l2 != null || carry != 0) {
            int sum = (l1 != null ? l1.val : 0) + (l2 != null ? l2.val : 0) + carry;
            carry = sum / 10;
            current.next = new ListNode(sum % 10);
            current = current.next;
            l1 = l1 != null ? l1.next : null;
            l2 = l2 != null ? l2.next : null;
        }
        
        return dummy.next;
    }
}",
                    ["C++"] = @"struct ListNode {
    int val;
    ListNode *next;
    ListNode() : val(0), next(nullptr) {}
    ListNode(int x) : val(x), next(nullptr) {}
    ListNode(int x, ListNode *next) : val(x), next(next) {}
};

class Solution {
public:
    ListNode* addTwoNumbers(ListNode* l1, ListNode* l2) {
        ListNode* dummy = new ListNode(0);
        ListNode* current = dummy;
        int carry = 0;
        
        while (l1 || l2 || carry) {
            int sum = (l1 ? l1->val : 0) + (l2 ? l2->val : 0) + carry;
            carry = sum / 10;
            current->next = new ListNode(sum % 10);
            current = current->next;
            l1 = l1 ? l1->next : nullptr;
            l2 = l2 ? l2->next : nullptr;
        }
        
        return dummy->next;
    }
};",
                    ["C#"] = @"public class ListNode {
    public int val;
    public ListNode next;
    public ListNode(int val=0, ListNode next=null) {
        this.val = val;
        this.next = next;
    }
}

public class Solution {
    public ListNode AddTwoNumbers(ListNode l1, ListNode l2) {
        ListNode dummy = new ListNode(0);
        ListNode current = dummy;
        int carry = 0;
        
        while (l1 != null || l2 != null || carry != 0) {
            int sum = (l1 != null ? l1.val : 0) + (l2 != null ? l2.val : 0) + carry;
            carry = sum / 10;
            current.next = new ListNode(sum % 10);
            current = current.next;
            l1 = l1 != null ? l1.next : null;
            l2 = l2 != null ? l2.next : null;
        }
        
        return dummy.next;
    }
}"
                };
            }

            // Longest Substring Without Repeating Characters
            var longestSubstring = questions.FirstOrDefault(q => q.Title == "Longest Substring Without Repeating Characters");
            if (longestSubstring != null)
            {
                questionTestCasesMap["Longest Substring Without Repeating Characters"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases
                    ("{\"s\": \"abcabcbb\"}", "3", false),
                    ("{\"s\": \"bbbbb\"}", "1", false),
                    ("{\"s\": \"pwwkew\"}", "3", false),
                    // 7 hidden test cases
                    ("{\"s\": \"\"}", "0", true),
                    ("{\"s\": \"a\"}", "1", true),
                    ("{\"s\": \"dvdf\"}", "3", true),
                    ("{\"s\": \"anviaj\"}", "5", true),
                    ("{\"s\": \"bbtablud\"}", "6", true),
                    ("{\"s\": \"tmmzuxt\"}", "5", true),
                    ("{\"s\": \"ohvhjdml\"}", "6", true),
                    ("{\"s\": \"abcdefghijklmnopqrstuvwxyz\"}", "26", true)
                };
                questionSolutionsMap["Longest Substring Without Repeating Characters"] = new Dictionary<string, string>
                {
                    ["JavaScript"] = "function lengthOfLongestSubstring(s) {\n    let map = new Map();\n    let maxLen = 0;\n    let start = 0;\n    \n    for (let end = 0; end < s.length; end++) {\n        if (map.has(s[end])) {\n            start = Math.max(start, map.get(s[end]) + 1);\n        }\n        map.set(s[end], end);\n        maxLen = Math.max(maxLen, end - start + 1);\n    }\n    \n    return maxLen;\n}",
                    ["Python"] = "def lengthOfLongestSubstring(s):\n    char_map = {}\n    max_len = 0\n    start = 0\n    \n    for end in range(len(s)):\n        if s[end] in char_map:\n            start = max(start, char_map[s[end]] + 1)\n        char_map[s[end]] = end\n        max_len = max(max_len, end - start + 1)\n    \n    return max_len"
                };
            }

            // Median of Two Sorted Arrays
            var medianArrays = questions.FirstOrDefault(q => q.Title == "Median of Two Sorted Arrays");
            if (medianArrays != null)
            {
                questionTestCasesMap["Median of Two Sorted Arrays"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases
                    ("{\"nums1\": [1,3], \"nums2\": [2]}", "2.00000", false),
                    ("{\"nums1\": [1,2], \"nums2\": [3,4]}", "2.50000", false),
                    ("{\"nums1\": [0,0], \"nums2\": [0,0]}", "0.00000", false),
                    // 7 hidden test cases
                    ("{\"nums1\": [1], \"nums2\": []}", "1.00000", true),
                    ("{\"nums1\": [], \"nums2\": [2]}", "2.00000", true),
                    ("{\"nums1\": [1,2], \"nums2\": []}", "1.50000", true),
                    ("{\"nums1\": [1,3,5], \"nums2\": [2,4,6]}", "3.50000", true),
                    ("{\"nums1\": [1,2,3,4,5], \"nums2\": [6,7,8,9,10]}", "5.50000", true),
                    ("{\"nums1\": [1,2], \"nums2\": [1,2,3]}", "2.00000", true),
                    ("{\"nums1\": [-5,-4,-3,-2,-1], \"nums2\": [1,2,3,4,5]}", "0.00000", true)
                };
                questionSolutionsMap["Median of Two Sorted Arrays"] = new Dictionary<string, string>
                {
                    ["JavaScript"] = "function findMedianSortedArrays(nums1, nums2) {\n    if (nums1.length > nums2.length) {\n        [nums1, nums2] = [nums2, nums1];\n    }\n    \n    let m = nums1.length, n = nums2.length;\n    let left = 0, right = m;\n    \n    while (left <= right) {\n        let partitionX = Math.floor((left + right) / 2);\n        let partitionY = Math.floor((m + n + 1) / 2) - partitionX;\n        \n        let maxX = partitionX === 0 ? -Infinity : nums1[partitionX - 1];\n        let minX = partitionX === m ? Infinity : nums1[partitionX];\n        let maxY = partitionY === 0 ? -Infinity : nums2[partitionY - 1];\n        let minY = partitionY === n ? Infinity : nums2[partitionY];\n        \n        if (maxX <= minY && maxY <= minX) {\n            if ((m + n) % 2 === 0) {\n                return (Math.max(maxX, maxY) + Math.min(minX, minY)) / 2;\n            } else {\n                return Math.max(maxX, maxY);\n            }\n        } else if (maxX > minY) {\n            right = partitionX - 1;\n        } else {\n            left = partitionX + 1;\n        }\n    }\n}",
                    ["Python"] = "def findMedianSortedArrays(nums1, nums2):\n    if len(nums1) > len(nums2):\n        nums1, nums2 = nums2, nums1\n    \n    m, n = len(nums1), len(nums2)\n    left, right = 0, m\n    \n    while left <= right:\n        partition_x = (left + right) // 2\n        partition_y = (m + n + 1) // 2 - partition_x\n        \n        max_x = float('-inf') if partition_x == 0 else nums1[partition_x - 1]\n        min_x = float('inf') if partition_x == m else nums1[partition_x]\n        max_y = float('-inf') if partition_y == 0 else nums2[partition_y - 1]\n        min_y = float('inf') if partition_y == n else nums2[partition_y]\n        \n        if max_x <= min_y and max_y <= min_x:\n            if (m + n) % 2 == 0:\n                return (max(max_x, max_y) + min(min_x, min_y)) / 2\n            else:\n                return max(max_x, max_y)\n        elif max_x > min_y:\n            right = partition_x - 1\n        else:\n            left = partition_x + 1"
                };
            }

            // Longest Palindromic Substring
            var longestPalindrome = questions.FirstOrDefault(q => q.Title == "Longest Palindromic Substring");
            if (longestPalindrome != null)
            {
                questionTestCasesMap["Longest Palindromic Substring"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases
                    ("{\"s\": \"babad\"}", "\"bab\"", false),
                    ("{\"s\": \"cbbd\"}", "\"bb\"", false),
                    ("{\"s\": \"a\"}", "\"a\"", false),
                    // 7 hidden test cases
                    ("{\"s\": \"ac\"}", "\"a\"", true),
                    ("{\"s\": \"racecar\"}", "\"racecar\"", true),
                    ("{\"s\": \"noon\"}", "\"noon\"", true),
                    ("{\"s\": \"abccba\"}", "\"abccba\"", true),
                    ("{\"s\": \"aab\"}", "\"aa\"", true),
                    ("{\"s\": \"banana\"}", "\"anana\"", true),
                    ("{\"s\": \"tracecars\"}", "\"racecar\"", true)
                };
                questionSolutionsMap["Longest Palindromic Substring"] = new Dictionary<string, string>
                {
                    ["JavaScript"] = "function longestPalindrome(s) {\n    let start = 0, maxLen = 0;\n    \n    function expandAroundCenter(left, right) {\n        while (left >= 0 && right < s.length && s[left] === s[right]) {\n            left--;\n            right++;\n        }\n        return right - left - 1;\n    }\n    \n    for (let i = 0; i < s.length; i++) {\n        let len1 = expandAroundCenter(i, i);\n        let len2 = expandAroundCenter(i, i + 1);\n        let len = Math.max(len1, len2);\n        \n        if (len > maxLen) {\n            maxLen = len;\n            start = i - Math.floor((len - 1) / 2);\n        }\n    }\n    \n    return s.substring(start, start + maxLen);\n}",
                    ["Python"] = "def longestPalindrome(s):\n    start = 0\n    max_len = 0\n    \n    def expand_around_center(left, right):\n        while left >= 0 and right < len(s) and s[left] == s[right]:\n            left -= 1\n            right += 1\n        return right - left - 1\n    \n    for i in range(len(s)):\n        len1 = expand_around_center(i, i)\n        len2 = expand_around_center(i, i + 1)\n        length = max(len1, len2)\n        \n        if length > max_len:\n            max_len = length\n            start = i - (length - 1) // 2\n    \n    return s[start:start + max_len]"
                };
            }

            // Container With Most Water
            var containerWater = questions.FirstOrDefault(q => q.Title == "Container With Most Water");
            if (containerWater != null)
            {
                questionTestCasesMap["Container With Most Water"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases
                    ("{\"height\": [1,8,6,2,5,4,8,3,7]}", "49", false),
                    ("{\"height\": [1,1]}", "1", false),
                    ("{\"height\": [1,2,1]}", "2", false),
                    // 7 hidden test cases
                    ("{\"height\": [1,2,3,4,5]}", "6", true),
                    ("{\"height\": [5,4,3,2,1]}", "6", true),
                    ("{\"height\": [2,3,4,5,18,17,6]}", "17", true),
                    ("{\"height\": [1,3,2,5,25,24,5]}", "24", true),
                    ("{\"height\": [10,9,8,7,6,5,4,3,2,1]}", "25", true),
                    ("{\"height\": [1,2,3,4,5,6,7,8,9,10]}", "25", true),
                    ("{\"height\": [1,100,6,2,5,4,8,3,7]}", "49", true)
                };
                questionSolutionsMap["Container With Most Water"] = new Dictionary<string, string>
                {
                    ["JavaScript"] = "function maxArea(height) {\n    let left = 0, right = height.length - 1;\n    let maxArea = 0;\n    \n    while (left < right) {\n        let area = Math.min(height[left], height[right]) * (right - left);\n        maxArea = Math.max(maxArea, area);\n        \n        if (height[left] < height[right]) {\n            left++;\n        } else {\n            right--;\n        }\n    }\n    \n    return maxArea;\n}",
                    ["Python"] = "def maxArea(height):\n    left, right = 0, len(height) - 1\n    max_area = 0\n    \n    while left < right:\n        area = min(height[left], height[right]) * (right - left)\n        max_area = max(max_area, area)\n        \n        if height[left] < height[right]:\n            left += 1\n        else:\n            right -= 1\n    \n    return max_area"
                };
            }

            // Reverse Linked List
            var reverseList = questions.FirstOrDefault(q => q.Title == "Reverse Linked List");
            if (reverseList != null)
            {
                questionTestCasesMap["Reverse Linked List"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases
                    ("{\"head\": [1,2,3,4,5]}", "[5,4,3,2,1]", false),
                    ("{\"head\": [1,2]}", "[2,1]", false),
                    ("{\"head\": []}", "[]", false),
                    // 7 hidden test cases
                    ("{\"head\": [1]}", "[1]", true),
                    ("{\"head\": [1,2,3]}", "[3,2,1]", true),
                    ("{\"head\": [1,2,3,4,5,6,7,8,9,10]}", "[10,9,8,7,6,5,4,3,2,1]", true),
                    ("{\"head\": [10,9,8,7,6,5,4,3,2,1]}", "[1,2,3,4,5,6,7,8,9,10]", true),
                    ("{\"head\": [1,1,1,1,1]}", "[1,1,1,1,1]", true),
                    ("{\"head\": [5,4,3,2,1]}", "[1,2,3,4,5]", true),
                    ("{\"head\": [0,1,2,3,4,5]}", "[5,4,3,2,1,0]", true)
                };
                questionSolutionsMap["Reverse Linked List"] = new Dictionary<string, string>
                {
                    ["JavaScript"] = "function reverseList(head) {\n    let prev = null;\n    let current = head;\n    \n    while (current) {\n        let next = current.next;\n        current.next = prev;\n        prev = current;\n        current = next;\n    }\n    \n    return prev;\n}",
                    ["Python"] = "def reverseList(head):\n    prev = None\n    current = head\n    \n    while current:\n        next_node = current.next\n        current.next = prev\n        prev = current\n        current = next_node\n    \n    return prev"
                };
            }

            // Valid Parentheses
            var validParentheses = questions.FirstOrDefault(q => q.Title == "Valid Parentheses");
            if (validParentheses != null)
            {
                questionTestCasesMap["Valid Parentheses"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases
                    ("{\"s\": \"()\"}", "true", false),
                    ("{\"s\": \"()[]{}\"}", "true", false),
                    ("{\"s\": \"(]\"}", "false", false),
                    // 7 hidden test cases
                    ("{\"s\": \"([)]\"}", "false", true),
                    ("{\"s\": \"{}\"}", "true", true),
                    ("{\"s\": \"[]\"}", "true", true),
                    ("{\"s\": \"({{}})\"}", "true", true),
                    ("{\"s\": \"((()))\"}", "true", true),
                    ("{\"s\": \"([{}])\"}", "true", true),
                    ("{\"s\": \"((\"}", "false", true),
                    ("{\"s\": \"))\"}", "false", true)
                };
                questionSolutionsMap["Valid Parentheses"] = new Dictionary<string, string>
                {
                    ["JavaScript"] = "function isValid(s) {\n    const stack = [];\n    const map = {\n        ')': '(',\n        '}': '{',\n        ']': '['\n    };\n    \n    for (let char of s) {\n        if (!map[char]) {\n            stack.push(char);\n        } else if (stack.pop() !== map[char]) {\n            return false;\n        }\n    }\n    \n    return stack.length === 0;\n}",
                    ["Python"] = "def isValid(s):\n    stack = []\n    mapping = {')': '(', '}': '{', ']': '['}\n    \n    for char in s:\n        if char not in mapping:\n            stack.append(char)\n        elif not stack or stack.pop() != mapping[char]:\n            return False\n    \n    return len(stack) == 0"
                };
            }

            // Merge Two Sorted Lists
            var mergeLists = questions.FirstOrDefault(q => q.Title == "Merge Two Sorted Lists");
            if (mergeLists != null)
            {
                questionTestCasesMap["Merge Two Sorted Lists"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases
                    ("{\"list1\": [1,2,4], \"list2\": [1,3,4]}", "[1,1,2,3,4,4]", false),
                    ("{\"list1\": [], \"list2\": []}", "[]", false),
                    ("{\"list1\": [], \"list2\": [0]}", "[0]", false),
                    // 7 hidden test cases
                    ("{\"list1\": [1], \"list2\": [2]}", "[1,2]", true),
                    ("{\"list1\": [2], \"list2\": [1]}", "[1,2]", true),
                    ("{\"list1\": [1,3,5], \"list2\": [2,4,6]}", "[1,2,3,4,5,6]", true),
                    ("{\"list1\": [1,2,3], \"list2\": [4,5,6]}", "[1,2,3,4,5,6]", true),
                    ("{\"list1\": [4,5,6], \"list2\": [1,2,3]}", "[1,2,3,4,5,6]", true),
                    ("{\"list1\": [1,1,1], \"list2\": [2,2,2]}", "[1,1,1,2,2,2]", true),
                    ("{\"list1\": [1,2,3,4,5], \"list2\": [6,7,8,9,10]}", "[1,2,3,4,5,6,7,8,9,10]", true)
                };
                questionSolutionsMap["Merge Two Sorted Lists"] = new Dictionary<string, string>
                {
                    ["JavaScript"] = "function mergeTwoLists(list1, list2) {\n    let dummy = new ListNode(0);\n    let current = dummy;\n    \n    while (list1 && list2) {\n        if (list1.val < list2.val) {\n            current.next = list1;\n            list1 = list1.next;\n        } else {\n            current.next = list2;\n            list2 = list2.next;\n        }\n        current = current.next;\n    }\n    \n    current.next = list1 || list2;\n    return dummy.next;\n}",
                    ["Python"] = "def mergeTwoLists(list1, list2):\n    dummy = ListNode(0)\n    current = dummy\n    \n    while list1 and list2:\n        if list1.val < list2.val:\n            current.next = list1\n            list1 = list1.next\n        else:\n            current.next = list2\n            list2 = list2.next\n        current = current.next\n    \n    current.next = list1 or list2\n    return dummy.next"
                };
            }

            // Maximum Subarray
            var maxSubarray = questions.FirstOrDefault(q => q.Title == "Maximum Subarray");
            if (maxSubarray != null)
            {
                questionTestCasesMap["Maximum Subarray"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases
                    ("{\"nums\": [-2,1,-3,4,-1,2,1,-5,4]}", "6", false),
                    ("{\"nums\": [1]}", "1", false),
                    ("{\"nums\": [5,4,-1,7,8]}", "23", false),
                    // 7 hidden test cases
                    ("{\"nums\": [-1]}", "-1", true),
                    ("{\"nums\": [1,2,3,4,5]}", "15", true),
                    ("{\"nums\": [-1,-2,-3,-4,-5]}", "-1", true),
                    ("{\"nums\": [1,-1,1,-1,1]}", "1", true),
                    ("{\"nums\": [1,2,-1,3,4]}", "9", true),
                    ("{\"nums\": [10,-5,8,-3,6]}", "16", true),
                    ("{\"nums\": [1,1,1,1,1,1,1,1,1,1]}", "10", true)
                };
                questionSolutionsMap["Maximum Subarray"] = new Dictionary<string, string>
                {
                    ["JavaScript"] = "function maxSubArray(nums) {\n    let maxSum = nums[0];\n    let currentSum = nums[0];\n    \n    for (let i = 1; i < nums.length; i++) {\n        currentSum = Math.max(nums[i], currentSum + nums[i]);\n        maxSum = Math.max(maxSum, currentSum);\n    }\n    \n    return maxSum;\n}",
                    ["Python"] = "def maxSubArray(nums):\n    max_sum = current_sum = nums[0]\n    \n    for i in range(1, len(nums)):\n        current_sum = max(nums[i], current_sum + nums[i])\n        max_sum = max(max_sum, current_sum)\n    \n    return max_sum"
                };
            }

            // SQL Questions - Second Highest Salary
            // Note: SQLite outputs plain values, not JSON. For null, it outputs empty string.
            var secondHighestSalary = questions.FirstOrDefault(q => q.Title == "Second Highest Salary");
            if (secondHighestSalary != null)
            {
                questionTestCasesMap["Second Highest Salary"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases - SQLite outputs plain values
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Salary INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 100), (2, 200), (3, 300);\"}", "200", false),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Salary INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 100);\"}", "", false),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Salary INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 100), (2, 200), (3, 200), (4, 300);\"}", "200", false),
                    // 7 hidden test cases
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Salary INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 50000), (2, 60000), (3, 70000), (4, 80000), (5, 90000);\"}", "80000", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Salary INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 1000), (2, 2000), (3, 3000), (4, 3000), (5, 3000);\"}", "2000", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Salary INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 500);\"}", "", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Salary INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 10), (2, 10);\"}", "", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Salary INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 100), (2, 100), (3, 200), (4, 300);\"}", "200", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Salary INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 10000), (2, 20000);\"}", "10000", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Salary INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 500), (2, 1000), (3, 1500), (4, 2000), (5, 2500), (6, 3000);\"}", "2500", true)
                };
                // Add SQL solution
                questionSolutionsMap["Second Highest Salary"] = new Dictionary<string, string>
                {
                    ["SQL"] = "SELECT MAX(Salary) AS SecondHighestSalary\nFROM Employee\nWHERE Salary < (SELECT MAX(Salary) FROM Employee);"
                };
            }

            // SQL Questions - Employees Earning More Than Their Managers
            // SQLite outputs plain text, one row per line
            var employeesEarningMore = questions.FirstOrDefault(q => q.Title == "Employees Earning More Than Their Managers");
            if (employeesEarningMore != null)
            {
                questionTestCasesMap["Employees Earning More Than Their Managers"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases - SQLite outputs plain text
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, ManagerId INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 'Joe', 70000, 3), (2, 'Henry', 80000, 4), (3, 'Sam', 60000, NULL), (4, 'Max', 90000, NULL);\"}", "Joe", false),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, ManagerId INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 'Alice', 50000, 2), (2, 'Bob', 60000, NULL), (3, 'Charlie', 55000, 2);\"}", "", false),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, ManagerId INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 'John', 80000, NULL), (2, 'Jane', 75000, 1), (3, 'Jim', 90000, 1);\"}", "Jim", false),
                    // 7 hidden test cases
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, ManagerId INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 'A', 50000, NULL), (2, 'B', 60000, 1), (3, 'C', 55000, 1);\"}", "B\nC", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, ManagerId INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 'X', 100000, NULL), (2, 'Y', 90000, 1), (3, 'Z', 95000, 1);\"}", "", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, ManagerId INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 'Manager', 50000, NULL), (2, 'Emp1', 40000, 1), (3, 'Emp2', 45000, 1);\"}", "", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, ManagerId INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 'CEO', 200000, NULL), (2, 'VP', 150000, 1), (3, 'Director', 100000, 2), (4, 'Manager', 80000, 3), (5, 'Employee', 120000, 3);\"}", "Employee", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, ManagerId INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 'A', 10000, NULL), (2, 'B', 20000, 1);\"}", "B", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, ManagerId INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 'M1', 50000, NULL), (2, 'E1', 40000, 1), (3, 'E2', 30000, 1), (4, 'E3', 60000, 1);\"}", "E3", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, ManagerId INT);\", \"data\": \"INSERT INTO Employee VALUES (1, 'Boss', 80000, NULL), (2, 'Worker1', 70000, 1), (3, 'Worker2', 85000, 1);\"}", "Worker2", true)
                };
                // Add SQL solution
                questionSolutionsMap["Employees Earning More Than Their Managers"] = new Dictionary<string, string>
                {
                    ["SQL"] = "SELECT e1.Name AS Employee\nFROM Employee e1\nJOIN Employee e2 ON e1.ManagerId = e2.Id\nWHERE e1.Salary > e2.Salary;"
                };
            }

            // SQL Questions - Rank Scores
            // SQLite outputs columns separated by | (pipe) character
            var rankScores = questions.FirstOrDefault(q => q.Title == "Rank Scores");
            if (rankScores != null)
            {
                questionTestCasesMap["Rank Scores"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases - SQLite outputs: Score|Rank per row (note: decimals may be formatted without trailing zeros)
                    ("{\"schema\": \"CREATE TABLE Scores (Id INT, Score DECIMAL(3,2));\", \"data\": \"INSERT INTO Scores VALUES (1, 3.50), (2, 3.65), (3, 4.00), (4, 3.85), (5, 4.00), (6, 3.65);\"}", "4|1\n4|1\n3.85|2\n3.65|3\n3.65|3\n3.5|4", false),
                    ("{\"schema\": \"CREATE TABLE Scores (Id INT, Score DECIMAL(3,2));\", \"data\": \"INSERT INTO Scores VALUES (1, 4.00), (2, 4.00), (3, 4.00);\"}", "4|1\n4|1\n4|1", false),
                    ("{\"schema\": \"CREATE TABLE Scores (Id INT, Score DECIMAL(3,2));\", \"data\": \"INSERT INTO Scores VALUES (1, 1.00), (2, 2.00), (3, 3.00);\"}", "3|1\n2|2\n1|3", false),
                    // 7 hidden test cases
                    ("{\"schema\": \"CREATE TABLE Scores (Id INT, Score DECIMAL(3,2));\", \"data\": \"INSERT INTO Scores VALUES (1, 3.50), (2, 3.50), (3, 3.50);\"}", "3.5|1\n3.5|1\n3.5|1", true),
                    ("{\"schema\": \"CREATE TABLE Scores (Id INT, Score DECIMAL(3,2));\", \"data\": \"INSERT INTO Scores VALUES (1, 5.00), (2, 4.50), (3, 4.00), (4, 3.50), (5, 3.00);\"}", "5|1\n4.5|2\n4|3\n3.5|4\n3|5", true),
                    ("{\"schema\": \"CREATE TABLE Scores (Id INT, Score DECIMAL(3,2));\", \"data\": \"INSERT INTO Scores VALUES (1, 2.50), (2, 2.50), (3, 3.00), (4, 3.00), (5, 1.00);\"}", "3|1\n3|1\n2.5|2\n2.5|2\n1|3", true),
                    ("{\"schema\": \"CREATE TABLE Scores (Id INT, Score DECIMAL(3,2));\", \"data\": \"INSERT INTO Scores VALUES (1, 0.00);\"}", "0|1", true),
                    ("{\"schema\": \"CREATE TABLE Scores (Id INT, Score DECIMAL(3,2));\", \"data\": \"INSERT INTO Scores VALUES (1, 4.75), (2, 4.50), (3, 4.25), (4, 4.00), (5, 3.75);\"}", "4.75|1\n4.5|2\n4.25|3\n4|4\n3.75|5", true),
                    ("{\"schema\": \"CREATE TABLE Scores (Id INT, Score DECIMAL(3,2));\", \"data\": \"INSERT INTO Scores VALUES (1, 1.50), (2, 1.50), (3, 2.00), (4, 2.00), (5, 2.50), (6, 2.50);\"}", "2.5|1\n2.5|1\n2|2\n2|2\n1.5|3\n1.5|3", true),
                    ("{\"schema\": \"CREATE TABLE Scores (Id INT, Score DECIMAL(3,2));\", \"data\": \"INSERT INTO Scores VALUES (1, 3.33), (2, 3.66), (3, 3.99), (4, 3.33), (5, 3.66);\"}", "3.99|1\n3.66|2\n3.66|2\n3.33|3\n3.33|3", true)
                };
                // Add SQL solution
                questionSolutionsMap["Rank Scores"] = new Dictionary<string, string>
                {
                    ["SQL"] = "SELECT Score, DENSE_RANK() OVER (ORDER BY Score DESC) AS Rank\nFROM Scores\nORDER BY Score DESC;"
                };
            }

            // SQL Questions - Department Top Three Salaries
            // SQLite outputs columns separated by | (pipe) character
            var deptTopThree = questions.FirstOrDefault(q => q.Title == "Department Top Three Salaries");
            if (deptTopThree != null)
            {
                questionTestCasesMap["Department Top Three Salaries"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases - SQLite outputs: Department|Employee|Salary per row
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, DepartmentId INT); CREATE TABLE Department (Id INT, Name VARCHAR(255));\", \"data\": \"INSERT INTO Employee VALUES (1, 'Joe', 85000, 1), (2, 'Henry', 80000, 2), (3, 'Sam', 60000, 2), (4, 'Max', 90000, 1), (5, 'Janet', 69000, 1), (6, 'Randy', 85000, 1), (7, 'Will', 70000, 1); INSERT INTO Department VALUES (1, 'IT'), (2, 'Sales');\"}", "IT|Max|90000\nIT|Joe|85000\nIT|Randy|85000\nIT|Will|70000\nSales|Henry|80000\nSales|Sam|60000", false),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, DepartmentId INT); CREATE TABLE Department (Id INT, Name VARCHAR(255));\", \"data\": \"INSERT INTO Employee VALUES (1, 'Alice', 50000, 1), (2, 'Bob', 60000, 1), (3, 'Charlie', 70000, 1); INSERT INTO Department VALUES (1, 'Engineering');\"}", "Engineering|Charlie|70000\nEngineering|Bob|60000\nEngineering|Alice|50000", false),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, DepartmentId INT); CREATE TABLE Department (Id INT, Name VARCHAR(255));\", \"data\": \"INSERT INTO Employee VALUES (1, 'X', 100000, 1), (2, 'Y', 100000, 1), (3, 'Z', 100000, 1), (4, 'W', 90000, 1); INSERT INTO Department VALUES (1, 'Finance');\"}", "Finance|X|100000\nFinance|Y|100000\nFinance|Z|100000\nFinance|W|90000", false),
                    // 7 hidden test cases
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, DepartmentId INT); CREATE TABLE Department (Id INT, Name VARCHAR(255));\", \"data\": \"INSERT INTO Employee VALUES (1, 'A', 50000, 1), (2, 'B', 60000, 1), (3, 'C', 70000, 1), (4, 'D', 80000, 1), (5, 'E', 90000, 1); INSERT INTO Department VALUES (1, 'IT');\"}", "IT|E|90000\nIT|D|80000\nIT|C|70000", true),
                    // Case 5: Only 2 unique salaries (75000, 50000) - since there are fewer than 3 unique, return all employees
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, DepartmentId INT); CREATE TABLE Department (Id INT, Name VARCHAR(255));\", \"data\": \"INSERT INTO Employee VALUES (1, 'Emp1', 75000, 1), (2, 'Emp2', 75000, 1), (3, 'Emp3', 75000, 1), (4, 'Emp4', 50000, 1); INSERT INTO Department VALUES (1, 'Sales');\"}", "Sales|Emp1|75000\nSales|Emp2|75000\nSales|Emp3|75000\nSales|Emp4|50000", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, DepartmentId INT); CREATE TABLE Department (Id INT, Name VARCHAR(255));\", \"data\": \"INSERT INTO Employee VALUES (1, 'A', 100000, 1), (2, 'B', 90000, 1), (3, 'C', 80000, 1), (4, 'D', 70000, 1), (5, 'E', 60000, 1), (6, 'F', 50000, 1); INSERT INTO Department VALUES (1, 'HR');\"}", "HR|A|100000\nHR|B|90000\nHR|C|80000", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, DepartmentId INT); CREATE TABLE Department (Id INT, Name VARCHAR(255));\", \"data\": \"INSERT INTO Employee VALUES (1, 'X', 50000, 1), (2, 'Y', 60000, 2), (3, 'Z', 70000, 1); INSERT INTO Department VALUES (1, 'Dept1'), (2, 'Dept2');\"}", "Dept1|Z|70000\nDept1|X|50000\nDept2|Y|60000", true),
                    // Case 8: Only 2 unique salaries (85000, 70000) - since there are fewer than 3 unique, return all employees
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, DepartmentId INT); CREATE TABLE Department (Id INT, Name VARCHAR(255));\", \"data\": \"INSERT INTO Employee VALUES (1, 'One', 85000, 1), (2, 'Two', 85000, 1), (3, 'Three', 85000, 1), (4, 'Four', 70000, 1); INSERT INTO Department VALUES (1, 'Tech');\"}", "Tech|One|85000\nTech|Two|85000\nTech|Three|85000\nTech|Four|70000", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, DepartmentId INT); CREATE TABLE Department (Id INT, Name VARCHAR(255));\", \"data\": \"INSERT INTO Employee VALUES (1, 'High1', 95000, 1), (2, 'High2', 90000, 1), (3, 'Mid1', 80000, 1), (4, 'Mid2', 80000, 1), (5, 'Low', 60000, 1); INSERT INTO Department VALUES (1, 'Ops');\"}", "Ops|High1|95000\nOps|High2|90000\nOps|Mid1|80000\nOps|Mid2|80000", true),
                    ("{\"schema\": \"CREATE TABLE Employee (Id INT, Name VARCHAR(255), Salary INT, DepartmentId INT); CREATE TABLE Department (Id INT, Name VARCHAR(255));\", \"data\": \"INSERT INTO Employee VALUES (1, 'A1', 100000, 1), (2, 'A2', 95000, 1), (3, 'A3', 90000, 1), (4, 'A4', 85000, 1), (5, 'A5', 80000, 1); INSERT INTO Department VALUES (1, 'Management');\"}", "Management|A1|100000\nManagement|A2|95000\nManagement|A3|90000", true)
                };
                // Add SQL solution
                questionSolutionsMap["Department Top Three Salaries"] = new Dictionary<string, string>
                {
                    ["SQL"] = "SELECT d.Name AS Department, e.Name AS Employee, e.Salary\nFROM Employee e\nJOIN Department d ON e.DepartmentId = d.Id\nWHERE (\n    SELECT COUNT(DISTINCT e2.Salary)\n    FROM Employee e2\n    WHERE e2.DepartmentId = e.DepartmentId AND e2.Salary > e.Salary\n) < 3\nORDER BY d.Name, e.Salary DESC;"
                };
            }

            // SQL Questions - Consecutive Numbers
            // SQLite outputs plain values, one per line
            var consecutiveNumbers = questions.FirstOrDefault(q => q.Title == "Consecutive Numbers");
            if (consecutiveNumbers != null)
            {
                questionTestCasesMap["Consecutive Numbers"] = new List<(string, string, bool)>
                {
                    // 3 visible test cases - SQLite outputs plain values
                    ("{\"schema\": \"CREATE TABLE Logs (Id INT, Num INT);\", \"data\": \"INSERT INTO Logs VALUES (1, 1), (2, 1), (3, 1), (4, 2), (5, 1), (6, 2), (7, 2);\"}", "1", false),
                    ("{\"schema\": \"CREATE TABLE Logs (Id INT, Num INT);\", \"data\": \"INSERT INTO Logs VALUES (1, 1), (2, 2), (3, 3);\"}", "", false),
                    ("{\"schema\": \"CREATE TABLE Logs (Id INT, Num INT);\", \"data\": \"INSERT INTO Logs VALUES (1, 5), (2, 5), (3, 5), (4, 5);\"}", "5", false),
                    // 7 hidden test cases
                    ("{\"schema\": \"CREATE TABLE Logs (Id INT, Num INT);\", \"data\": \"INSERT INTO Logs VALUES (1, 1), (2, 1), (3, 1), (4, 1), (5, 2);\"}", "1", true),
                    ("{\"schema\": \"CREATE TABLE Logs (Id INT, Num INT);\", \"data\": \"INSERT INTO Logs VALUES (1, 3), (2, 3), (3, 3), (4, 4), (5, 4), (6, 4);\"}", "3\n4", true),
                    ("{\"schema\": \"CREATE TABLE Logs (Id INT, Num INT);\", \"data\": \"INSERT INTO Logs VALUES (1, 10), (2, 10), (3, 10), (4, 20), (5, 20), (6, 20);\"}", "10\n20", true),
                    ("{\"schema\": \"CREATE TABLE Logs (Id INT, Num INT);\", \"data\": \"INSERT INTO Logs VALUES (1, 1), (2, 2), (3, 1), (4, 1), (5, 1);\"}", "1", true),
                    ("{\"schema\": \"CREATE TABLE Logs (Id INT, Num INT);\", \"data\": \"INSERT INTO Logs VALUES (1, 7), (2, 7), (3, 8), (4, 7), (5, 7), (6, 7);\"}", "7", true),
                    ("{\"schema\": \"CREATE TABLE Logs (Id INT, Num INT);\", \"data\": \"INSERT INTO Logs VALUES (1, 100), (2, 100), (3, 100), (4, 200), (5, 100), (6, 200), (7, 200), (8, 200);\"}", "100\n200", true),
                    ("{\"schema\": \"CREATE TABLE Logs (Id INT, Num INT);\", \"data\": \"INSERT INTO Logs VALUES (1, 1), (2, 1), (3, 1), (4, 1), (5, 1), (6, 2);\"}", "1", true)
                };
                // Add SQL solution
                questionSolutionsMap["Consecutive Numbers"] = new Dictionary<string, string>
                {
                    ["SQL"] = "SELECT DISTINCT l1.Num AS ConsecutiveNums\nFROM Logs l1\nJOIN Logs l2 ON l1.Id = l2.Id - 1\nJOIN Logs l3 ON l2.Id = l3.Id - 1\nWHERE l1.Num = l2.Num AND l2.Num = l3.Num;"
                };
            }

            // Get all existing questions that might need test cases added (including SQL questions)
            var allQuestionTitles = questionTestCasesMap.Keys.Union(questionSolutionsMap.Keys).ToList();
            var existingQuestionsNeedingTestCases = await context.InterviewQuestions
                .Where(q => allQuestionTitles.Contains(q.Title))
                .ToListAsync();
            
            // Combine new questions with existing questions that need test cases.
            // Skip "Two Sum" by title (not by position) since it's seeded separately above.
            var allQuestionsToProcess = questions
                .Where(q => q.Title != "Two Sum")
                .Union(existingQuestionsNeedingTestCases.Where(eq => !questions.Any(q => q.Id == eq.Id)))
                .ToList();
            
            // Add test cases and solutions for all questions
            foreach (var question in allQuestionsToProcess)
            {
                var questionTitle = question.Title;
                
                // For SQL questions and linked list questions, always delete and re-seed
                // to ensure correct format (e.g. JSON array format for linked list params)
                var alwaysRefreshTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    { "Add Two Numbers" };

                if (question.QuestionType == "SQL" || alwaysRefreshTitles.Contains(questionTitle))
                {
                    var existingCasesToRefresh = await context.QuestionTestCases
                        .Where(tc => tc.QuestionId == question.Id)
                        .ToListAsync();
                    
                    if (existingCasesToRefresh.Any())
                    {
                        context.QuestionTestCases.RemoveRange(existingCasesToRefresh);
                        await context.SaveChangesAsync();
                        logger.LogInformation("Deleted {Count} old test cases for refresh: {Title}", existingCasesToRefresh.Count, questionTitle);
                    }
                }
                
                // Check if test cases already exist for this question
                var existingTestCaseCount = await context.QuestionTestCases
                    .CountAsync(tc => tc.QuestionId == question.Id);
                
                // Add test cases if they don't exist yet
                if (questionTestCasesMap.ContainsKey(questionTitle) && existingTestCaseCount == 0)
                {
                    int testCaseNum = 1;
                    foreach (var (input, expectedOutput, isHidden) in questionTestCasesMap[questionTitle])
                    {
                        testCases.Add(new QuestionTestCase
                        {
                            Id = Guid.NewGuid(),
                            QuestionId = question.Id,
                            TestCaseNumber = testCaseNum++,
                            Input = input,
                            ExpectedOutput = expectedOutput,
                            IsHidden = isHidden,
                            CreatedAt = now
                        });
                    }
                }
                else
                {
                    // Fallback: add at least one test case
                    testCases.Add(new QuestionTestCase
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = question.Id,
                        TestCaseNumber = 1,
                        Input = "{}",
                        ExpectedOutput = "{}",
                        IsHidden = false,
                        CreatedAt = now
                    });
                }

                // Add solutions with detailed explanations if they don't exist yet
                if (questionSolutionsMap.ContainsKey(questionTitle))
                {
                    // Check if solutions already exist for this question
                    var existingSolutionCount = await context.QuestionSolutions
                        .CountAsync(s => s.QuestionId == question.Id);
                    
                    if (existingSolutionCount == 0)
                    {
                        int solutionNum = 1;
                        foreach (var (language, code) in questionSolutionsMap[questionTitle])
                        {
                            var explanation = GetSolutionExplanation(questionTitle, solutionNum, language, question.TimeComplexityHint, question.SpaceComplexityHint);
                            solutions.Add(new QuestionSolution
                            {
                                Id = Guid.NewGuid(),
                                QuestionId = question.Id,
                                Language = language,
                                Code = code,
                                Explanation = explanation,
                                TimeComplexity = question.TimeComplexityHint,
                                SpaceComplexity = question.SpaceComplexityHint,
                                IsOfficial = true,
                                CreatedAt = now,
                                UpdatedAt = now
                            });
                            solutionNum++;
                        }
                    }
                }
                
                // Fallback: add at least one solution if no solutions were added above
                if (!questionSolutionsMap.ContainsKey(questionTitle))
                {
                    // Fallback: add at least one solution
                    solutions.Add(new QuestionSolution
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = question.Id,
                        Language = "JavaScript",
                        Code = $"// Solution for {questionTitle}",
                        Explanation = "Official solution",
                        TimeComplexity = question.TimeComplexityHint,
                        SpaceComplexity = question.SpaceComplexityHint,
                        IsOfficial = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            context.QuestionTestCases.AddRange(testCases);
            context.QuestionSolutions.AddRange(solutions);
            await context.SaveChangesAsync();

            logger.LogInformation("✅ Seeded {TestCaseCount} test cases and {SolutionCount} solutions", 
                testCases.Count, solutions.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed test cases and solutions, but this is non-critical. Continuing...");
        }
    }

    /// <summary>
    /// Gets detailed solution explanation for a question
    /// </summary>
    private static string GetSolutionExplanation(string questionTitle, int solutionNum, string language, string timeComplexity, string spaceComplexity)
    {
        return questionTitle switch
        {
            "Add Two Numbers" => $"Solution {solutionNum}: Linked List Traversal approach\n" +
                "Our solution processes both linked lists simultaneously, digit by digit, similar to how we add numbers manually. We maintain a carry variable to handle cases where the sum of two digits exceeds 9.\n\n" +
                "The algorithm uses a dummy node to simplify edge cases and builds the result list as we traverse. For each pair of digits (or single digit if one list is exhausted), we calculate the sum including any carry from the previous addition. The units digit becomes the value of the new node, and the tens digit becomes the carry for the next iteration.\n\n" +
                "Pseudocode:\n" +
                "function addTwoNumbers(l1, l2):\n" +
                "    dummy = new ListNode(0)\n" +
                "    current = dummy\n" +
                "    carry = 0\n" +
                "    while l1 or l2 or carry:\n" +
                "        sum = (l1.val if l1 else 0) + (l2.val if l2 else 0) + carry\n" +
                "        carry = sum / 10\n" +
                "        current.next = new ListNode(sum % 10)\n" +
                "        current = current.next\n" +
                "        l1 = l1.next if l1 else null\n" +
                "        l2 = l2.next if l2 else null\n" +
                "    return dummy.next\n\n" +
                $"Time Complexity: {timeComplexity}. We traverse both lists once, and the time complexity is determined by the longer list.\n\n" +
                $"Space Complexity: {spaceComplexity}. We create a new list to store the result, which requires space proportional to the length of the longer input list.",

            "Longest Substring Without Repeating Characters" => $"Solution {solutionNum}: Sliding Window approach\n" +
                "Our solution uses a sliding window technique with two pointers (left and right) to maintain a window of characters without duplicates. We use a hash map to track the last seen index of each character.\n\n" +
                "As we expand the window by moving the right pointer, we check if the current character has been seen before within the current window. If it has, we shrink the window by moving the left pointer to just after the previous occurrence of that character. This ensures we always maintain a valid window without repeating characters.\n\n" +
                "Pseudocode:\n" +
                "function lengthOfLongestSubstring(s):\n" +
                "    charMap = {{}}\n" +
                "    maxLen = 0\n" +
                "    left = 0\n" +
                "    for right from 0 to len(s)-1:\n" +
                "        if s[right] in charMap:\n" +
                "            left = max(left, charMap[s[right]] + 1)\n" +
                "        charMap[s[right]] = right\n" +
                "        maxLen = max(maxLen, right - left + 1)\n" +
                "    return maxLen\n\n" +
                $"Time Complexity: {timeComplexity}. Each character is visited at most twice (once by right pointer, once by left pointer).\n\n" +
                $"Space Complexity: {spaceComplexity}. The hash map stores at most min(n, m) characters where m is the size of the character set.",

            "Median of Two Sorted Arrays" => $"Solution {solutionNum}: Binary Search approach\n" +
                "Our solution uses binary search on the smaller array to find the correct partition point. The key insight is that we can partition both arrays such that all elements on the left side are less than or equal to all elements on the right side.\n\n" +
                "We binary search on the partition point of the smaller array. For each partition, we calculate the corresponding partition in the larger array. We check if this partition is valid (max of left partitions <= min of right partitions). If valid, we can compute the median. If not, we adjust our binary search range.\n\n" +
                "Pseudocode:\n" +
                "function findMedianSortedArrays(nums1, nums2):\n" +
                "    if len(nums1) > len(nums2):\n" +
                "        swap(nums1, nums2)\n" +
                "    m, n = len(nums1), len(nums2)\n" +
                "    left, right = 0, m\n" +
                "    while left <= right:\n" +
                "        partitionX = (left + right) / 2\n" +
                "        partitionY = (m + n + 1) / 2 - partitionX\n" +
                "        maxX = nums1[partitionX-1] if partitionX > 0 else -inf\n" +
                "        minX = nums1[partitionX] if partitionX < m else inf\n" +
                "        maxY = nums2[partitionY-1] if partitionY > 0 else -inf\n" +
                "        minY = nums2[partitionY] if partitionY < n else inf\n" +
                "        if maxX <= minY and maxY <= minX:\n" +
                "            return median based on max(maxX, maxY) and min(minX, minY)\n" +
                "        elif maxX > minY:\n" +
                "            right = partitionX - 1\n" +
                "        else:\n" +
                "            left = partitionX + 1\n\n" +
                $"Time Complexity: {timeComplexity}. We perform binary search on the smaller array.\n\n" +
                $"Space Complexity: {spaceComplexity}. We only use a constant amount of extra space.",

            "Longest Palindromic Substring" => $"Solution {solutionNum}: Expand Around Centers approach\n" +
                "Our solution expands around each possible center of a palindrome. A palindrome can have either an odd length (center at a character) or an even length (center between two characters).\n\n" +
                "For each position in the string, we try expanding outward to find the longest palindrome centered at that position. We handle both odd and even length palindromes by expanding from (i, i) and (i, i+1) respectively. We keep track of the longest palindrome found so far.\n\n" +
                "Pseudocode:\n" +
                "function longestPalindrome(s):\n" +
                "    start = 0\n" +
                "    maxLen = 0\n" +
                "    function expandAroundCenter(left, right):\n" +
                "        while left >= 0 and right < len(s) and s[left] == s[right]:\n" +
                "            left--\n" +
                "            right++\n" +
                "        return right - left - 1\n" +
                "    for i from 0 to len(s)-1:\n" +
                "        len1 = expandAroundCenter(i, i)\n" +
                "        len2 = expandAroundCenter(i, i+1)\n" +
                "        length = max(len1, len2)\n" +
                "        if length > maxLen:\n" +
                "            maxLen = length\n" +
                "            start = i - (length - 1) / 2\n" +
                "    return s[start:start+maxLen]\n\n" +
                $"Time Complexity: {timeComplexity}. For each of the n positions, we may expand up to n/2 times in the worst case.\n\n" +
                $"Space Complexity: {spaceComplexity}. We only use a constant amount of extra space.",

            "Container With Most Water" => $"Solution {solutionNum}: Two Pointers approach\n" +
                "Our solution uses two pointers starting from both ends of the array. The key insight is that the area is limited by the shorter line, and moving the pointer at the shorter line inward might find a taller line that compensates for the reduced width.\n\n" +
                "At each step, we calculate the area formed by the two lines and update the maximum. Then, we move the pointer pointing to the shorter line inward. This is optimal because moving the taller line inward can only decrease the area (width decreases, height can't increase beyond the current shorter line).\n\n" +
                "Pseudocode:\n" +
                "function maxArea(height):\n" +
                "    left = 0\n" +
                "    right = len(height) - 1\n" +
                "    maxArea = 0\n" +
                "    while left < right:\n" +
                "        area = min(height[left], height[right]) * (right - left)\n" +
                "        maxArea = max(maxArea, area)\n" +
                "        if height[left] < height[right]:\n" +
                "            left++\n" +
                "        else:\n" +
                "            right--\n" +
                "    return maxArea\n\n" +
                $"Time Complexity: {timeComplexity}. We traverse the array once with two pointers.\n\n" +
                $"Space Complexity: {spaceComplexity}. We only use a constant amount of extra space.",

            "Reverse Linked List" => $"Solution {solutionNum}: Iterative approach\n" +
                "Our solution iteratively reverses the linked list by maintaining three pointers: previous, current, and next. We traverse the list once, reversing the next pointer of each node to point to the previous node instead of the next node.\n\n" +
                "The algorithm starts with previous pointing to null and current pointing to the head. For each node, we save the next node, reverse the current node's next pointer to point to previous, then move both previous and current forward. When we finish, previous points to the new head.\n\n" +
                "Pseudocode:\n" +
                "function reverseList(head):\n" +
                "    prev = null\n" +
                "    current = head\n" +
                "    while current != null:\n" +
                "        next = current.next\n" +
                "        current.next = prev\n" +
                "        prev = current\n" +
                "        current = next\n" +
                "    return prev\n\n" +
                $"Time Complexity: {timeComplexity}. We visit each node exactly once.\n\n" +
                $"Space Complexity: {spaceComplexity}. We only use a constant amount of extra space for the pointers.",

            "Valid Parentheses" => $"Solution {solutionNum}: Stack approach\n" +
                "Our solution uses a stack to keep track of opening brackets. When we encounter an opening bracket, we push it onto the stack. When we encounter a closing bracket, we check if it matches the most recent opening bracket (top of stack). If it matches, we pop from the stack. If the stack is empty at the end, all brackets are properly matched.\n\n" +
                "The key insight is that brackets must be closed in the reverse order of their opening. This LIFO (Last In First Out) behavior is exactly what a stack provides. We use a hash map to quickly check if a closing bracket matches its corresponding opening bracket.\n\n" +
                "Pseudocode:\n" +
                "function isValid(s):\n" +
                "    stack = []\n" +
                "    mapping = {{')': '(', '}}': '{{', ']': '['}}\n" +
                "    for char in s:\n" +
                "        if char not in mapping:\n" +
                "            stack.push(char)\n" +
                "        else if stack is empty or stack.pop() != mapping[char]:\n" +
                "            return false\n" +
                "    return stack is empty\n\n" +
                $"Time Complexity: {timeComplexity}. We process each character exactly once.\n\n" +
                $"Space Complexity: {spaceComplexity}. In the worst case, all characters are opening brackets, requiring O(n) stack space.",

            "Merge Two Sorted Lists" => $"Solution {solutionNum}: Two Pointers approach\n" +
                "Our solution uses two pointers, one for each list, to merge them in sorted order. We compare the values at the current positions of both lists and add the smaller value to the result. We continue until one list is exhausted, then append the remaining elements from the other list.\n\n" +
                "The algorithm uses a dummy node to simplify edge cases and avoid special handling for the first node. We build the merged list by connecting nodes in sorted order, reusing the existing nodes from both input lists to avoid creating new ones.\n\n" +
                "Pseudocode:\n" +
                "function mergeTwoLists(list1, list2):\n" +
                "    dummy = new ListNode(0)\n" +
                "    current = dummy\n" +
                "    while list1 and list2:\n" +
                "        if list1.val < list2.val:\n" +
                "            current.next = list1\n" +
                "            list1 = list1.next\n" +
                "        else:\n" +
                "            current.next = list2\n" +
                "            list2 = list2.next\n" +
                "        current = current.next\n" +
                "    current.next = list1 or list2\n" +
                "    return dummy.next\n\n" +
                $"Time Complexity: {timeComplexity}. We visit each node in both lists exactly once.\n\n" +
                $"Space Complexity: {spaceComplexity}. We only use a constant amount of extra space for the dummy node and pointers.",

            "Maximum Subarray" => $"Solution {solutionNum}: Kadane's algorithm (Dynamic Programming)\n" +
                "Our solution uses Kadane's algorithm, which is a dynamic programming approach. The key insight is that at each position, we decide whether to extend the previous subarray or start a new subarray from the current position.\n\n" +
                "We maintain two variables: currentSum (maximum sum of subarray ending at current position) and maxSum (overall maximum sum seen so far). At each position, we update currentSum to be the maximum of the current element alone or the current element plus the previous currentSum. This ensures we always have the optimal subarray ending at each position.\n\n" +
                "Pseudocode:\n" +
                "function maxSubArray(nums):\n" +
                "    maxSum = nums[0]\n" +
                "    currentSum = nums[0]\n" +
                "    for i from 1 to len(nums)-1:\n" +
                "        currentSum = max(nums[i], currentSum + nums[i])\n" +
                "        maxSum = max(maxSum, currentSum)\n" +
                "    return maxSum\n\n" +
                $"Time Complexity: {timeComplexity}. We traverse the array once.\n\n" +
                $"Space Complexity: {spaceComplexity}. We only use a constant amount of extra space.",

            "Second Highest Salary" => $"Solution {solutionNum}: Subquery approach\n" +
                "Our solution uses a subquery to find the maximum salary first, then selects the maximum salary that is less than that maximum. This effectively gives us the second highest salary.\n\n" +
                "The outer query selects the maximum salary from employees whose salary is less than the overall maximum salary (found by the subquery). If there is no second highest salary (i.e., all employees have the same salary or there's only one employee), the query returns NULL.\n\n" +
                "Alternative approaches include using LIMIT and OFFSET, or window functions like ROW_NUMBER() or DENSE_RANK(). However, the subquery approach is straightforward and works well when we need to handle edge cases like ties or missing values.\n\n" +
                $"Time Complexity: {timeComplexity}. The query scans the Employee table twice: once for the subquery and once for the outer query. Most databases optimize this, but worst-case complexity is O(n).\n\n" +
                $"Space Complexity: {spaceComplexity}. The query uses temporary storage for intermediate results, typically O(n) in worst case.",

            "Employees Earning More Than Their Managers" => $"Solution {solutionNum}: Self-join approach\n" +
                "Our solution uses a self-join on the Employee table to compare each employee's salary with their manager's salary. We join the table to itself where the employee's ManagerId matches the manager's Id.\n\n" +
                "The join condition connects employees to their managers: e1.ManagerId = e2.Id. We then filter for employees whose salary (e1.Salary) is greater than their manager's salary (e2.Salary). The result includes only employees who earn more than their managers.\n\n" +
                "This is a classic self-join problem where we need to compare rows within the same table based on a relationship defined within that table (manager-employee relationship).\n\n" +
                $"Time Complexity: {timeComplexity}. The join operation requires comparing each employee with their manager. With proper indexing on Id and ManagerId, this is typically O(n log n) due to the join operation.\n\n" +
                $"Space Complexity: {spaceComplexity}. The join creates an intermediate result set, requiring O(n) space in the worst case.",

            "Rank Scores" => $"Solution {solutionNum}: Window function approach (DENSE_RANK)\n" +
                "Our solution uses the DENSE_RANK() window function to assign ranks to scores. DENSE_RANK() assigns consecutive ranks without gaps, which is exactly what we need for this problem.\n\n" +
                "The window function DENSE_RANK() OVER (ORDER BY Score DESC) partitions and orders all rows by Score in descending order, then assigns ranks. Scores with the same value receive the same rank, and the next distinct score receives the next consecutive rank (no gaps).\n\n" +
                "We order by Score DESC to ensure the highest scores get rank 1. The final ORDER BY Score DESC ensures the output is sorted from highest to lowest score.\n\n" +
                "Alternative approaches include using subqueries to count distinct scores greater than the current score, but window functions are more efficient and readable.\n\n" +
                $"Time Complexity: {timeComplexity}. The DENSE_RANK() window function requires sorting the scores, which is typically O(n log n).\n\n" +
                $"Space Complexity: {spaceComplexity}. Window functions may require additional space for sorting and ranking, typically O(n).",

            "Department Top Three Salaries" => $"Solution {solutionNum}: Correlated subquery approach\n" +
                "Our solution uses a correlated subquery to count how many distinct salaries are higher than the current employee's salary within the same department. If this count is less than 3, the employee is in the top 3 earners of their department.\n\n" +
                "The correlated subquery (SELECT COUNT(DISTINCT e2.Salary) FROM Employee e2 WHERE e2.DepartmentId = e.DepartmentId AND e2.Salary > e.Salary) counts distinct salaries higher than the current employee's salary in the same department. If this count is 0, 1, or 2, the employee is ranked 1st, 2nd, or 3rd respectively.\n\n" +
                "We join with the Department table to get department names, and order by department name and salary in descending order for readable output.\n\n" +
                "Alternative approaches include using window functions like DENSE_RANK() partitioned by department, which might be more efficient for large datasets.\n\n" +
                $"Time Complexity: {timeComplexity}. For each employee, we execute a correlated subquery that scans employees in the same department. This results in O(n²) complexity in the worst case, though databases may optimize this.\n\n" +
                $"Space Complexity: {spaceComplexity}. The query requires space for joins and subquery results, typically O(n).",

            "Consecutive Numbers" => $"Solution {solutionNum}: Self-join approach\n" +
                "Our solution uses self-joins to check if three consecutive rows have the same number. We join the Logs table to itself twice to access the previous and next rows.\n\n" +
                "The first join connects l1 to l2 where l2.Id = l1.Id - 1 (next row). The second join connects l2 to l3 where l3.Id = l2.Id - 1 (row after next). We then filter for cases where all three rows have the same Num value: l1.Num = l2.Num AND l2.Num = l3.Num.\n\n" +
                "We use DISTINCT because multiple consecutive triplets might share the same number (e.g., four consecutive 1s would match the pattern twice).\n\n" +
                "Alternative approaches include using window functions like LAG() and LEAD() to access previous and next values, which might be more efficient and readable.\n\n" +
                $"Time Complexity: {timeComplexity}. The self-joins require comparing each row with its neighbors. With proper indexing on Id, this is typically O(n) since each row is joined a constant number of times.\n\n" +
                $"Space Complexity: {spaceComplexity}. The joins create intermediate result sets, requiring O(n) space.",

            _ => $"Solution {solutionNum}: {language} approach\n" +
                $"Official {language} solution for {questionTitle}.\n\n" +
                $"Time Complexity: {timeComplexity}\n\n" +
                $"Space Complexity: {spaceComplexity}"
        };
    }
}

