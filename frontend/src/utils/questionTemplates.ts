/**
 * Question-specific code templates
 * Each question has its own function signature based on the problem
 */

export interface QuestionTemplate {
  javascript: string;
  python: string;
  java: string;
  cpp: string;
  csharp: string;
  go: string;
}

const QUESTION_TEMPLATES: Record<string, QuestionTemplate> = {
  "Two Sum": {
    javascript: '/**\n * @param {number[]} nums\n * @param {number} target\n * @return {number[]}\n */\nvar twoSum = function(nums, target) {\n    \n};',
    python: 'def twoSum(nums, target):\n    pass',
    java: 'class Solution {\n    public int[] twoSum(int[] nums, int target) {\n        \n    }\n}',
    cpp: 'class Solution {\npublic:\n    vector<int> twoSum(vector<int>& nums, int target) {\n        \n    }\n};',
    csharp: 'public class Solution {\n    public int[] TwoSum(int[] nums, int target) {\n        \n    }\n}',
    go: 'func twoSum(nums []int, target int) []int {\n    \n}',
  },
  "Add Two Numbers": {
    javascript: '/**\n * @param {ListNode} l1\n * @param {ListNode} l2\n * @return {ListNode}\n */\nvar addTwoNumbers = function(l1, l2) {\n    \n};',
    python: 'def addTwoNumbers(l1, l2):\n    pass',
    java: 'class Solution {\n    public ListNode addTwoNumbers(ListNode l1, ListNode l2) {\n        \n    }\n}',
    cpp: 'class Solution {\npublic:\n    ListNode* addTwoNumbers(ListNode* l1, ListNode* l2) {\n        \n    }\n};',
    csharp: 'public class Solution {\n    public ListNode AddTwoNumbers(ListNode l1, ListNode l2) {\n        \n    }\n}',
    go: 'func addTwoNumbers(l1 *ListNode, l2 *ListNode) *ListNode {\n    \n}',
  },
  "Longest Substring Without Repeating Characters": {
    javascript: '/**\n * @param {string} s\n * @return {number}\n */\nvar lengthOfLongestSubstring = function(s) {\n    \n};',
    python: 'def lengthOfLongestSubstring(s):\n    pass',
    java: 'class Solution {\n    public int lengthOfLongestSubstring(String s) {\n        \n    }\n}',
    cpp: 'class Solution {\npublic:\n    int lengthOfLongestSubstring(string s) {\n        \n    }\n};',
    csharp: 'public class Solution {\n    public int LengthOfLongestSubstring(string s) {\n        \n    }\n}',
    go: 'func lengthOfLongestSubstring(s string) int {\n    \n}',
  },
  "Median of Two Sorted Arrays": {
    javascript: '/**\n * @param {number[]} nums1\n * @param {number[]} nums2\n * @return {number}\n */\nvar findMedianSortedArrays = function(nums1, nums2) {\n    \n};',
    python: 'def findMedianSortedArrays(nums1, nums2):\n    pass',
    java: 'class Solution {\n    public double findMedianSortedArrays(int[] nums1, int[] nums2) {\n        \n    }\n}',
    cpp: 'class Solution {\npublic:\n    double findMedianSortedArrays(vector<int>& nums1, vector<int>& nums2) {\n        \n    }\n};',
    csharp: 'public class Solution {\n    public double FindMedianSortedArrays(int[] nums1, int[] nums2) {\n        \n    }\n}',
    go: 'func findMedianSortedArrays(nums1 []int, nums2 []int) float64 {\n    \n}',
  },
  "Longest Palindromic Substring": {
    javascript: '/**\n * @param {string} s\n * @return {string}\n */\nvar longestPalindrome = function(s) {\n    \n};',
    python: 'def longestPalindrome(s):\n    pass',
    java: 'class Solution {\n    public String longestPalindrome(String s) {\n        \n    }\n}',
    cpp: 'class Solution {\npublic:\n    string longestPalindrome(string s) {\n        \n    }\n};',
    csharp: 'public class Solution {\n    public string LongestPalindrome(string s) {\n        \n    }\n}',
    go: 'func longestPalindrome(s string) string {\n    \n}',
  },
  "Container With Most Water": {
    javascript: '/**\n * @param {number[]} height\n * @return {number}\n */\nvar maxArea = function(height) {\n    \n};',
    python: 'def maxArea(height):\n    pass',
    java: 'class Solution {\n    public int maxArea(int[] height) {\n        \n    }\n}',
    cpp: 'class Solution {\npublic:\n    int maxArea(vector<int>& height) {\n        \n    }\n};',
    csharp: 'public class Solution {\n    public int MaxArea(int[] height) {\n        \n    }\n}',
    go: 'func maxArea(height []int) int {\n    \n}',
  },
  "Reverse Linked List": {
    javascript: '/**\n * @param {ListNode} head\n * @return {ListNode}\n */\nvar reverseList = function(head) {\n    \n};',
    python: 'def reverseList(head):\n    pass',
    java: 'class Solution {\n    public ListNode reverseList(ListNode head) {\n        \n    }\n}',
    cpp: 'class Solution {\npublic:\n    ListNode* reverseList(ListNode* head) {\n        \n    }\n};',
    csharp: 'public class Solution {\n    public ListNode ReverseList(ListNode head) {\n        \n    }\n}',
    go: 'func reverseList(head *ListNode) *ListNode {\n    \n}',
  },
  "Valid Parentheses": {
    javascript: '/**\n * @param {string} s\n * @return {boolean}\n */\nvar isValid = function(s) {\n    \n};',
    python: 'def isValid(s):\n    pass',
    java: 'class Solution {\n    public boolean isValid(String s) {\n        \n    }\n}',
    cpp: 'class Solution {\npublic:\n    bool isValid(string s) {\n        \n    }\n};',
    csharp: 'public class Solution {\n    public bool IsValid(string s) {\n        \n    }\n}',
    go: 'func isValid(s string) bool {\n    \n}',
  },
  "Merge Two Sorted Lists": {
    javascript: '/**\n * @param {ListNode} list1\n * @param {ListNode} list2\n * @return {ListNode}\n */\nvar mergeTwoLists = function(list1, list2) {\n    \n};',
    python: 'def mergeTwoLists(list1, list2):\n    pass',
    java: 'class Solution {\n    public ListNode mergeTwoLists(ListNode list1, ListNode list2) {\n        \n    }\n}',
    cpp: 'class Solution {\npublic:\n    ListNode* mergeTwoLists(ListNode* list1, ListNode* list2) {\n        \n    }\n};',
    csharp: 'public class Solution {\n    public ListNode MergeTwoLists(ListNode list1, ListNode list2) {\n        \n    }\n}',
    go: 'func mergeTwoLists(list1 *ListNode, list2 *ListNode) *ListNode {\n    \n}',
  },
  "Maximum Subarray": {
    javascript: '/**\n * @param {number[]} nums\n * @return {number}\n */\nvar maxSubArray = function(nums) {\n    \n};',
    python: 'def maxSubArray(nums):\n    pass',
    java: 'class Solution {\n    public int maxSubArray(int[] nums) {\n        \n    }\n}',
    cpp: 'class Solution {\npublic:\n    int maxSubArray(vector<int>& nums) {\n        \n    }\n};',
    csharp: 'public class Solution {\n    public int MaxSubArray(int[] nums) {\n        \n    }\n}',
    go: 'func maxSubArray(nums []int) int {\n    \n}',
  },
};

// Default template (fallback)
const DEFAULT_TEMPLATE: QuestionTemplate = {
  javascript: '/**\n * @param {number[]} nums\n * @param {number} target\n * @return {number[]}\n */\nvar twoSum = function(nums, target) {\n    \n};',
  python: 'def twoSum(nums, target):\n    pass',
  java: 'class Solution {\n    public int[] twoSum(int[] nums, int target) {\n        \n    }\n}',
  cpp: 'class Solution {\npublic:\n    vector<int> twoSum(vector<int>& nums, int target) {\n        \n    }\n};',
  csharp: 'public class Solution {\n    public int[] TwoSum(int[] nums, int target) {\n        \n    }\n}',
  go: 'func twoSum(nums []int, target int) []int {\n    \n}',
};

/**
 * Get code template for a specific question and language
 */
export function getQuestionTemplate(questionTitle: string, language: string): string {
  const template = QUESTION_TEMPLATES[questionTitle] || DEFAULT_TEMPLATE;
  return template[language as keyof QuestionTemplate] || template.javascript;
}

/**
 * Get all templates for a question
 */
export function getAllTemplatesForQuestion(questionTitle: string): QuestionTemplate {
  return QUESTION_TEMPLATES[questionTitle] || DEFAULT_TEMPLATE;
}

