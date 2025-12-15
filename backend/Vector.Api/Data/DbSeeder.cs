using Microsoft.EntityFrameworkCore;
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
    /// Seeds mock interview videos
    /// </summary>
    public static async Task SeedMockInterviews(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Check if mock interviews exist
            var interviewsExist = await context.MockInterviews.AnyAsync();

            if (interviewsExist)
            {
                logger.LogInformation("Mock interviews already exist. Skipping seed.");
                return;
            }

            // Add initial mock interview video
            var mockInterview = new MockInterview
            {
                Id = Guid.NewGuid(),
                Title = "What Is Exponent? - Introduction to Mock Interviews",
                Description = "An introduction to Exponent's mock interview platform and how to prepare for technical interviews effectively.",
                VideoUrl = "https://dev-vector-user-uploads.s3.us-east-1.amazonaws.com/videos/mock-interviews/what-is-exponent.mp4",
                ThumbnailUrl = "",
                DurationSeconds = 180, // 3 minutes (approximate)
                Category = "Introduction",
                Difficulty = "Easy",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.MockInterviews.Add(mockInterview);
            await context.SaveChangesAsync();

            logger.LogInformation("Mock interview video seeded successfully: {Title}", mockInterview.Title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed mock interviews");
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

        // Seed mock interviews (non-critical)
        try
        {
            await SeedMockInterviews(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed mock interviews, but this is non-critical. Continuing...");
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
            var questionsExist = await context.InterviewQuestions.AnyAsync();
            if (questionsExist)
            {
                logger.LogInformation("Interview questions already exist. Skipping seed.");
                return;
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
                    Hints = new[] { "A really brute force way would be to search for all possible pairs of numbers", "So, if we fix one of the numbers, say x, we have to scan the entire array to find the next number y which is value - x", "Can we change our array somehow so that this search becomes faster?" },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(n)",
                    AcceptanceRate = 48.5
                },
                new {
                    Title = "Add Two Numbers",
                    Description = "You are given two non-empty linked lists representing two non-negative integers. The digits are stored in reverse order, and each of their nodes contains a single digit. Add the two numbers and return the sum as a linked list.\n\nYou may assume the two numbers do not contain any leading zero, except the number 0 itself.",
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
                    Hints = new[] { "Keep track of the carry using a variable", "Simulate digits-by-digits sum starting from the head of list" },
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
                    Hints = new[] { "Use a hash map to track characters", "Use two pointers for sliding window" },
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
                    Hints = new[] { "The problem can be solved using binary search", "Think about partitioning both arrays" },
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
                    Hints = new[] { "How can we reuse a previously computed palindrome?", "Use dynamic programming" },
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
                    Hints = new[] { "Use two pointers approach", "Start from both ends and move inward" },
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
                    Hints = new[] { "You can solve this iteratively or recursively", "Keep track of previous node" },
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
                    Hints = new[] { "Use a stack to keep track of opening brackets", "When you see a closing bracket, check if it matches the top of the stack" },
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
                    Hints = new[] { "Use two pointers, one for each list", "Compare values and merge" },
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
                    Hints = new[] { "Use Kadane's algorithm", "Keep track of maximum sum ending at current position" },
                    TimeComplexityHint = "O(n)",
                    SpaceComplexityHint = "O(1)",
                    AcceptanceRate = 49.8
                }
            };

            foreach (var qData in questionsData)
            {
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
                    CreatedBy = createdBy,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                questions.Add(question);
            }

            context.InterviewQuestions.AddRange(questions);
            await context.SaveChangesAsync();

            // Now add test cases and solutions for each question
            await SeedQuestionTestCasesAndSolutions(context, logger, questions, createdBy);

            logger.LogInformation("✅ Seeded {Count} interview questions", questions.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to seed interview questions, but this is non-critical. Continuing...");
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

            // Two Sum test cases and solution
            var twoSum = questions.First(q => q.Title == "Two Sum");
            
            testCases.AddRange(new[]
            {
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
                    IsHidden = true,
                    CreatedAt = now
                }
            });

            solutions.Add(new QuestionSolution
            {
                Id = Guid.NewGuid(),
                QuestionId = twoSum.Id,
                Language = "JavaScript",
                Code = "function twoSum(nums, target) {\n    const map = new Map();\n    \n    for (let i = 0; i < nums.length; i++) {\n        const complement = target - nums[i];\n        \n        if (map.has(complement)) {\n            return [map.get(complement), i];\n        }\n        \n        map.set(nums[i], i);\n    }\n    \n    return [];\n}",
                Explanation = "Use a hash map to store seen numbers and their indices. For each number, check if its complement (target - current number) exists in the map.",
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

            // Add test cases and solutions for other questions (simplified)
            foreach (var question in questions.Skip(1))
            {
                // Add at least one test case per question
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

                // Add at least one solution per question
                solutions.Add(new QuestionSolution
                {
                    Id = Guid.NewGuid(),
                    QuestionId = question.Id,
                    Language = "JavaScript",
                    Code = "// Solution for " + question.Title,
                    Explanation = "Official solution",
                    TimeComplexity = question.TimeComplexityHint,
                    SpaceComplexity = question.SpaceComplexityHint,
                    IsOfficial = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
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
}

