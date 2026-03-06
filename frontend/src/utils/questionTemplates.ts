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
  sql?: string;
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
    javascript: '/**\n * Definition for singly-linked list.\n */\nclass ListNode {\n    constructor(val, next = null) {\n        this.val = val;\n        this.next = next;\n    }\n}\n\n/**\n * Helper function to convert array to linked list (for testing)\n */\nfunction arrayToList(arr) {\n    if (!arr || arr.length === 0) return null;\n    let head = new ListNode(arr[0]);\n    let current = head;\n    for (let i = 1; i < arr.length; i++) {\n        current.next = new ListNode(arr[i]);\n        current = current.next;\n    }\n    return head;\n}\n\n/**\n * Helper function to convert linked list to array (for testing)\n */\nfunction listToArray(head) {\n    let arr = [];\n    while (head) {\n        arr.push(head.val);\n        head = head.next;\n    }\n    return arr;\n}\n\n/**\n * @param {ListNode} l1\n * @param {ListNode} l2\n * @return {ListNode}\n */\nvar addTwoNumbers = function(l1, l2) {\n    \n};',
    python: '# Definition for singly-linked list.\nclass ListNode:\n    def __init__(self, val=0, next=None):\n        self.val = val\n        self.next = next\n\ndef array_to_list(arr):\n    """Helper function to convert array to linked list (for testing)\"\"\"\n    if not arr:\n        return None\n    head = ListNode(arr[0])\n    current = head\n    for i in range(1, len(arr)):\n        current.next = ListNode(arr[i])\n        current = current.next\n    return head\n\ndef list_to_array(head):\n    """Helper function to convert linked list to array (for testing)\"\"\"\n    arr = []\n    while head:\n        arr.append(head.val)\n        head = head.next\n    return arr\n\ndef addTwoNumbers(l1, l2):\n    pass',
    java: '/**\n * Definition for singly-linked list.\n */\npublic class ListNode {\n    int val;\n    ListNode next;\n    ListNode() {}\n    ListNode(int val) { this.val = val; }\n    ListNode(int val, ListNode next) { this.val = val; this.next = next; }\n}\n\nclass Solution {\n    /**\n     * Helper function to convert array to linked list (for testing)\n     */\n    public static ListNode arrayToList(int[] arr) {\n        if (arr == null || arr.length == 0) return null;\n        ListNode head = new ListNode(arr[0]);\n        ListNode current = head;\n        for (int i = 1; i < arr.length; i++) {\n            current.next = new ListNode(arr[i]);\n            current = current.next;\n        }\n        return head;\n    }\n    \n    /**\n     * Helper function to convert linked list to array (for testing)\n     */\n    public static int[] listToArray(ListNode head) {\n        java.util.List<Integer> list = new java.util.ArrayList<>();\n        while (head != null) {\n            list.add(head.val);\n            head = head.next;\n        }\n        return list.stream().mapToInt(i -> i).toArray();\n    }\n    \n    public ListNode addTwoNumbers(ListNode l1, ListNode l2) {\n        \n    }\n}',
    cpp: '/**\n * Definition for singly-linked list.\n */\nstruct ListNode {\n    int val;\n    ListNode *next;\n    ListNode() : val(0), next(nullptr) {}\n    ListNode(int x) : val(x), next(nullptr) {}\n    ListNode(int x, ListNode *next) : val(x), next(next) {}\n};\n\n/**\n * Helper function to convert vector to linked list (for testing)\n */\nListNode* arrayToList(const std::vector<int>& arr) {\n    if (arr.empty()) return nullptr;\n    ListNode* head = new ListNode(arr[0]);\n    ListNode* current = head;\n    for (size_t i = 1; i < arr.size(); i++) {\n        current->next = new ListNode(arr[i]);\n        current = current->next;\n    }\n    return head;\n}\n\n/**\n * Helper function to convert linked list to vector (for testing)\n */\nstd::vector<int> listToArray(ListNode* head) {\n    std::vector<int> arr;\n    while (head) {\n        arr.push_back(head->val);\n        head = head->next;\n    }\n    return arr;\n}\n\nclass Solution {\npublic:\n    ListNode* addTwoNumbers(ListNode* l1, ListNode* l2) {\n        \n    }\n};',
    csharp: '/**\n * Definition for singly-linked list.\n */\npublic class ListNode {\n    public int val;\n    public ListNode next;\n    public ListNode(int val=0, ListNode next=null) {\n        this.val = val;\n        this.next = next;\n    }\n}\n\npublic class Solution {\n    /**\n     * Helper function to convert array to linked list (for testing)\n     */\n    public static ListNode ArrayToList(int[] arr) {\n        if (arr == null || arr.Length == 0) return null;\n        ListNode head = new ListNode(arr[0]);\n        ListNode current = head;\n        for (int i = 1; i < arr.Length; i++) {\n            current.next = new ListNode(arr[i]);\n            current = current.next;\n        }\n        return head;\n    }\n    \n    /**\n     * Helper function to convert linked list to array (for testing)\n     */\n    public static int[] ListToArray(ListNode head) {\n        var list = new System.Collections.Generic.List<int>();\n        while (head != null) {\n            list.Add(head.val);\n            head = head.next;\n        }\n        return list.ToArray();\n    }\n    \n    public ListNode AddTwoNumbers(ListNode l1, ListNode l2) {\n        \n    }\n}',
    go: '/**\n * Definition for singly-linked list.\n */\ntype ListNode struct {\n    Val int\n    Next *ListNode\n}\n\n/**\n * Helper function to convert slice to linked list (for testing)\n */\nfunc arrayToList(arr []int) *ListNode {\n    if len(arr) == 0 {\n        return nil\n    }\n    head := &ListNode{Val: arr[0]}\n    current := head\n    for i := 1; i < len(arr); i++ {\n        current.Next = &ListNode{Val: arr[i]}\n        current = current.Next\n    }\n    return head\n}\n\n/**\n * Helper function to convert linked list to slice (for testing)\n */\nfunc listToArray(head *ListNode) []int {\n    arr := []int{}\n    for head != nil {\n        arr = append(arr, head.Val)\n        head = head.Next\n    }\n    return arr\n}\n\nfunc addTwoNumbers(l1 *ListNode, l2 *ListNode) *ListNode {\n    \n}',
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
    javascript: '/**\n * Definition for singly-linked list.\n */\nclass ListNode {\n    constructor(val, next = null) {\n        this.val = val;\n        this.next = next;\n    }\n}\n\n/**\n * @param {ListNode} head\n * @return {ListNode}\n */\nvar reverseList = function(head) {\n    \n};',
    python: '# Definition for singly-linked list.\nclass ListNode:\n    def __init__(self, val=0, next=None):\n        self.val = val\n        self.next = next\n\ndef reverseList(head):\n    pass',
    java: '/**\n * Definition for singly-linked list.\n */\npublic class ListNode {\n    int val;\n    ListNode next;\n    ListNode() {}\n    ListNode(int val) { this.val = val; }\n    ListNode(int val, ListNode next) { this.val = val; this.next = next; }\n}\n\nclass Solution {\n    public ListNode reverseList(ListNode head) {\n        \n    }\n}',
    cpp: '/**\n * Definition for singly-linked list.\n */\nstruct ListNode {\n    int val;\n    ListNode *next;\n    ListNode() : val(0), next(nullptr) {}\n    ListNode(int x) : val(x), next(nullptr) {}\n    ListNode(int x, ListNode *next) : val(x), next(next) {}\n};\n\nclass Solution {\npublic:\n    ListNode* reverseList(ListNode* head) {\n        \n    }\n};',
    csharp: '/**\n * Definition for singly-linked list.\n */\npublic class ListNode {\n    public int val;\n    public ListNode next;\n    public ListNode(int val=0, ListNode next=null) {\n        this.val = val;\n        this.next = next;\n    }\n}\n\npublic class Solution {\n    public ListNode ReverseList(ListNode head) {\n        \n    }\n}',
    go: '/**\n * Definition for singly-linked list.\n */\ntype ListNode struct {\n    Val int\n    Next *ListNode\n}\n\nfunc reverseList(head *ListNode) *ListNode {\n    \n}',
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
    javascript: '/**\n * Definition for singly-linked list.\n */\nclass ListNode {\n    constructor(val, next = null) {\n        this.val = val;\n        this.next = next;\n    }\n}\n\n/**\n * @param {ListNode} list1\n * @param {ListNode} list2\n * @return {ListNode}\n */\nvar mergeTwoLists = function(list1, list2) {\n    \n};',
    python: '# Definition for singly-linked list.\nclass ListNode:\n    def __init__(self, val=0, next=None):\n        self.val = val\n        self.next = next\n\ndef mergeTwoLists(list1, list2):\n    pass',
    java: '/**\n * Definition for singly-linked list.\n */\npublic class ListNode {\n    int val;\n    ListNode next;\n    ListNode() {}\n    ListNode(int val) { this.val = val; }\n    ListNode(int val, ListNode next) { this.val = val; this.next = next; }\n}\n\nclass Solution {\n    public ListNode mergeTwoLists(ListNode list1, ListNode list2) {\n        \n    }\n}',
    cpp: '/**\n * Definition for singly-linked list.\n */\nstruct ListNode {\n    int val;\n    ListNode *next;\n    ListNode() : val(0), next(nullptr) {}\n    ListNode(int x) : val(x), next(nullptr) {}\n    ListNode(int x, ListNode *next) : val(x), next(next) {}\n};\n\nclass Solution {\npublic:\n    ListNode* mergeTwoLists(ListNode* list1, ListNode* list2) {\n        \n    }\n};',
    csharp: '/**\n * Definition for singly-linked list.\n */\npublic class ListNode {\n    public int val;\n    public ListNode next;\n    public ListNode(int val=0, ListNode next=null) {\n        this.val = val;\n        this.next = next;\n    }\n}\n\npublic class Solution {\n    public ListNode MergeTwoLists(ListNode list1, ListNode list2) {\n        \n    }\n}',
    go: '/**\n * Definition for singly-linked list.\n */\ntype ListNode struct {\n    Val int\n    Next *ListNode\n}\n\nfunc mergeTwoLists(list1 *ListNode, list2 *ListNode) *ListNode {\n    \n}',
  },
  "Maximum Subarray": {
    javascript: '/**\n * @param {number[]} nums\n * @return {number}\n */\nvar maxSubArray = function(nums) {\n    \n};',
    python: 'def maxSubArray(nums):\n    pass',
    java: 'class Solution {\n    public int maxSubArray(int[] nums) {\n        \n    }\n}',
    cpp: 'class Solution {\npublic:\n    int maxSubArray(vector<int>& nums) {\n        \n    }\n};',
    csharp: 'public class Solution {\n    public int MaxSubArray(int[] nums) {\n        \n    }\n}',
    go: 'func maxSubArray(nums []int) int {\n    \n}',
  },
  // SQL Questions - Start with empty editor, no pre-filled solutions
  "Second Highest Salary": {
    javascript: '', // Not used for SQL questions
    python: '',
    java: '',
    cpp: '',
    csharp: '',
    go: '',
    sql: '' // Empty template - users write their own solution
  },
  "Employees Earning More Than Their Managers": {
    javascript: '',
    python: '',
    java: '',
    cpp: '',
    csharp: '',
    go: '',
    sql: '' // Empty template - users write their own solution
  },
  "Rank Scores": {
    javascript: '',
    python: '',
    java: '',
    cpp: '',
    csharp: '',
    go: '',
    sql: '' // Empty template - users write their own solution
  },
  "Department Top Three Salaries": {
    javascript: '',
    python: '',
    java: '',
    cpp: '',
    csharp: '',
    go: '',
    sql: '' // Empty template - users write their own solution
  },
  "Consecutive Numbers": {
    javascript: '',
    python: '',
    java: '',
    cpp: '',
    csharp: '',
    go: '',
    sql: '' // Empty template - users write their own solution
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
  
  // For SQL questions, return SQL template or empty string
  if (language.toLowerCase() === 'sql') {
    return template.sql || '';
  }
  
  // For other languages, return the template or default to javascript
  return template[language as keyof QuestionTemplate] || template.javascript || '';
}

/**
 * Get all templates for a question
 */
export function getAllTemplatesForQuestion(questionTitle: string): QuestionTemplate {
  return QUESTION_TEMPLATES[questionTitle] || DEFAULT_TEMPLATE;
}

