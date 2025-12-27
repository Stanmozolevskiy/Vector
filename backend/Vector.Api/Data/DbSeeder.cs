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
            // Always repopulate questions - clear existing and add fresh ones
            var existingQuestions = await context.InterviewQuestions.ToListAsync();
            if (existingQuestions.Any())
            {
                logger.LogInformation("Clearing existing {Count} interview questions for repopulation...", existingQuestions.Count);
                
                // Remove related test cases and solutions first
                var questionIds = existingQuestions.Select(q => q.Id).ToList();
                var relatedTestCases = await context.QuestionTestCases.Where(tc => questionIds.Contains(tc.QuestionId)).ToListAsync();
                var relatedSolutions = await context.QuestionSolutions.Where(s => questionIds.Contains(s.QuestionId)).ToListAsync();
                
                context.QuestionTestCases.RemoveRange(relatedTestCases);
                context.QuestionSolutions.RemoveRange(relatedSolutions);
                context.InterviewQuestions.RemoveRange(existingQuestions);
                await context.SaveChangesAsync();
                
                logger.LogInformation("Cleared existing questions, test cases, and solutions.");
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
                    Hints = new[] { 
                        "Make sure you understand what is required. You need to add two numbers represented as linked lists where digits are stored in reverse order. Try working through the example: l1 = [2,4,3] represents 342, and l2 = [5,6,4] represents 465.",
                        "If you're stuck, think about how you add numbers manually. You start from the rightmost digit, add them, and if the sum is 10 or more, you carry over to the next digit. Apply the same logic here, but remember the digits are stored in reverse.",
                        "Keep track of the carry using a variable. As you traverse both lists, add the current digits along with any carry from the previous addition. If the sum is 10 or more, update the carry for the next iteration."
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
                }
            };

            int questionIndex = 0;
            foreach (var qData in questionsData)
            {
                // Approve first 8 questions by default
                bool isApproved = questionIndex < 8;
                
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
            
            // Two Sum: Add exactly 10 test cases (3 visible + 7 hidden)
            // Generate large input test case (array with 1000 elements for performance testing)
            var largeNums = new List<int>();
            for (int i = 0; i < 1000; i++)
            {
                largeNums.Add(i);
            }
            var largeInput = $"{{\"nums\": [{string.Join(",", largeNums)}], \"target\": 1999}}";
            
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
                    ExpectedOutput = "[0,5]",
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
                    ExpectedOutput = "[999,1000]",
                    IsHidden = true,
                    CreatedAt = now
                }
            };
            
            testCases.AddRange(twoSumTestCases);

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

            // Add comprehensive test cases and solutions for all other questions
            var questionTestCasesMap = new Dictionary<string, List<(string Input, string ExpectedOutput, bool IsHidden)>>();
            var questionSolutionsMap = new Dictionary<string, Dictionary<string, string>>();

            // Add Two Numbers
            var addTwoNumbers = questions.FirstOrDefault(q => q.Title == "Add Two Numbers");
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
                    ["JavaScript"] = "function addTwoNumbers(l1, l2) {\n    let dummy = new ListNode(0);\n    let current = dummy;\n    let carry = 0;\n    \n    while (l1 || l2 || carry) {\n        let sum = (l1?.val || 0) + (l2?.val || 0) + carry;\n        carry = Math.floor(sum / 10);\n        current.next = new ListNode(sum % 10);\n        current = current.next;\n        l1 = l1?.next;\n        l2 = l2?.next;\n    }\n    \n    return dummy.next;\n}",
                    ["Python"] = "def addTwoNumbers(l1, l2):\n    dummy = ListNode(0)\n    current = dummy\n    carry = 0\n    \n    while l1 or l2 or carry:\n        sum_val = (l1.val if l1 else 0) + (l2.val if l2 else 0) + carry\n        carry = sum_val // 10\n        current.next = ListNode(sum_val % 10)\n        current = current.next\n        l1 = l1.next if l1 else None\n        l2 = l2.next if l2 else None\n    \n    return dummy.next"
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

            // Add test cases and solutions for all questions
            foreach (var question in questions.Skip(1))
            {
                var questionTitle = question.Title;
                
                // Add test cases
                if (questionTestCasesMap.ContainsKey(questionTitle))
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

                // Add solutions with detailed explanations
                if (questionSolutionsMap.ContainsKey(questionTitle))
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
                else
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

            _ => $"Solution {solutionNum}: {language} approach\n" +
                $"Official {language} solution for {questionTitle}.\n\n" +
                $"Time Complexity: {timeComplexity}\n\n" +
                $"Space Complexity: {spaceComplexity}"
        };
    }
}

