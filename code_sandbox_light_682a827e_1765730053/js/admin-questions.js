// Admin Questions Management JavaScript

// Sample questions data (in production, this would come from an API)
let questionsData = [
    {
        id: 1,
        title: "Two Sum",
        difficulty: "Easy",
        category: "Array",
        tags: ["Hash Table", "Two Pointers"],
        companies: ["Google", "Amazon", "Facebook"],
        acceptance: 48.5,
        status: "Published",
        description: "Given an array of integers nums and an integer target, return indices of the two numbers such that they add up to target.",
        constraints: "2 <= nums.length <= 10^4\n-10^9 <= nums[i] <= 10^9\n-10^9 <= target <= 10^9\nOnly one valid answer exists.",
        examples: [
            {
                input: "nums = [2,7,11,15], target = 9",
                output: "[0,1]",
                explanation: "Because nums[0] + nums[1] == 9, we return [0, 1]."
            }
        ],
        solution: "function twoSum(nums, target) {\n    const map = new Map();\n    for (let i = 0; i < nums.length; i++) {\n        const complement = target - nums[i];\n        if (map.has(complement)) {\n            return [map.get(complement), i];\n        }\n        map.set(nums[i], i);\n    }\n    return [];\n}",
        hints: ["Use a hash map to store values", "Check for complement in each iteration"],
        timeComplexity: "O(n)",
        spaceComplexity: "O(n)",
        order: 1
    },
    {
        id: 2,
        title: "Add Two Numbers",
        difficulty: "Medium",
        category: "Linked List",
        tags: ["Math", "Recursion"],
        companies: ["Amazon", "Microsoft"],
        acceptance: 38.2,
        status: "Published",
        description: "You are given two non-empty linked lists representing two non-negative integers. The digits are stored in reverse order, and each of their nodes contains a single digit.",
        constraints: "The number of nodes in each linked list is in the range [1, 100].\n0 <= Node.val <= 9",
        examples: [
            {
                input: "l1 = [2,4,3], l2 = [5,6,4]",
                output: "[7,0,8]",
                explanation: "342 + 465 = 807"
            }
        ],
        solution: "",
        hints: ["Handle carry", "Consider different list lengths"],
        timeComplexity: "O(max(m,n))",
        spaceComplexity: "O(max(m,n))",
        order: 2
    },
    {
        id: 3,
        title: "Longest Substring Without Repeating Characters",
        difficulty: "Medium",
        category: "String",
        tags: ["Hash Table", "Sliding Window"],
        companies: ["Amazon", "Google", "Bloomberg"],
        acceptance: 33.8,
        status: "Published",
        description: "Given a string s, find the length of the longest substring without repeating characters.",
        constraints: "0 <= s.length <= 5 * 10^4\ns consists of English letters, digits, symbols and spaces.",
        examples: [
            {
                input: 's = "abcabcbb"',
                output: "3",
                explanation: 'The answer is "abc", with the length of 3.'
            }
        ],
        solution: "",
        hints: ["Use sliding window technique", "Track character positions with a hash map"],
        timeComplexity: "O(n)",
        spaceComplexity: "O(min(m,n))",
        order: 3
    },
    {
        id: 4,
        title: "Median of Two Sorted Arrays",
        difficulty: "Hard",
        category: "Binary Search",
        tags: ["Array", "Divide and Conquer"],
        companies: ["Google", "Amazon", "Microsoft"],
        acceptance: 34.5,
        status: "Draft",
        description: "Given two sorted arrays nums1 and nums2 of size m and n respectively, return the median of the two sorted arrays.",
        constraints: "nums1.length == m\nnums2.length == n\n0 <= m <= 1000\n0 <= n <= 1000",
        examples: [
            {
                input: "nums1 = [1,3], nums2 = [2]",
                output: "2.00000",
                explanation: "merged array = [1,2,3] and median is 2."
            }
        ],
        solution: "",
        hints: ["Use binary search", "Partition arrays correctly"],
        timeComplexity: "O(log(min(m,n)))",
        spaceComplexity: "O(1)",
        order: 4
    }
];

// Generate more sample data
for (let i = 5; i <= 50; i++) {
    const difficulties = ["Easy", "Medium", "Hard"];
    const categories = ["Array", "String", "Hash Table", "Dynamic Programming", "Tree", "Graph"];
    const statuses = ["Published", "Draft"];
    
    questionsData.push({
        id: i,
        title: `Sample Question ${i}`,
        difficulty: difficulties[Math.floor(Math.random() * difficulties.length)],
        category: categories[Math.floor(Math.random() * categories.length)],
        tags: ["Tag1", "Tag2"],
        companies: ["Company A", "Company B"],
        acceptance: Math.floor(Math.random() * 50 + 20),
        status: statuses[Math.floor(Math.random() * statuses.length)],
        description: `This is a sample question description for question ${i}.`,
        constraints: "Sample constraints",
        examples: [{
            input: "Sample input",
            output: "Sample output",
            explanation: "Sample explanation"
        }],
        solution: "",
        hints: ["Sample hint"],
        timeComplexity: "O(n)",
        spaceComplexity: "O(1)",
        order: i
    });
}

// State
let currentPage = 1;
const itemsPerPage = 20;
let filteredQuestions = [...questionsData];
let selectedQuestions = new Set();
let editingQuestionId = null;

// DOM Elements
const questionsTableBody = document.getElementById('questionsTableBody');
const searchInput = document.getElementById('searchInput');
const difficultyFilter = document.getElementById('difficultyFilter');
const categoryFilter = document.getElementById('categoryFilter');
const statusFilter = document.getElementById('statusFilter');
const selectAllCheckbox = document.getElementById('selectAll');
const addQuestionBtn = document.getElementById('addQuestionBtn');
const questionModal = document.getElementById('questionModal');
const questionForm = document.getElementById('questionForm');
const closeModalBtn = document.getElementById('closeModalBtn');
const cancelBtn = document.getElementById('cancelBtn');
const deleteModal = document.getElementById('deleteModal');
const closeDeleteModalBtn = document.getElementById('closeDeleteModalBtn');
const cancelDeleteBtn = document.getElementById('cancelDeleteBtn');
const confirmDeleteBtn = document.getElementById('confirmDeleteBtn');
const bulkDeleteBtn = document.getElementById('bulkDeleteBtn');
const exportBtn = document.getElementById('exportBtn');
const addExampleBtn = document.getElementById('addExampleBtn');
const prevPageBtn = document.getElementById('prevPageBtn');
const nextPageBtn = document.getElementById('nextPageBtn');

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    updateStats();
    renderQuestions();
    attachEventListeners();
});

// Update statistics
function updateStats() {
    const total = questionsData.length;
    const easy = questionsData.filter(q => q.difficulty === 'Easy').length;
    const medium = questionsData.filter(q => q.difficulty === 'Medium').length;
    const hard = questionsData.filter(q => q.difficulty === 'Hard').length;
    
    document.getElementById('totalQuestions').textContent = total;
    document.getElementById('easyQuestions').textContent = easy;
    document.getElementById('mediumQuestions').textContent = medium;
    document.getElementById('hardQuestions').textContent = hard;
}

// Render questions table
function renderQuestions() {
    const start = (currentPage - 1) * itemsPerPage;
    const end = start + itemsPerPage;
    const paginatedQuestions = filteredQuestions.slice(start, end);
    
    questionsTableBody.innerHTML = '';
    
    if (paginatedQuestions.length === 0) {
        questionsTableBody.innerHTML = `
            <tr>
                <td colspan="8" style="text-align: center; padding: 3rem; color: var(--text-light);">
                    <i class="fas fa-inbox" style="font-size: 3rem; margin-bottom: 1rem; display: block;"></i>
                    No questions found matching your criteria.
                </td>
            </tr>
        `;
        return;
    }
    
    paginatedQuestions.forEach(question => {
        const row = document.createElement('tr');
        const isSelected = selectedQuestions.has(question.id);
        
        row.innerHTML = `
            <td>
                <input type="checkbox" class="question-checkbox" data-id="${question.id}" ${isSelected ? 'checked' : ''}>
            </td>
            <td>${question.id}</td>
            <td>
                <a href="#" class="question-title" data-id="${question.id}">${question.title}</a>
            </td>
            <td>
                <span class="difficulty-badge difficulty-${question.difficulty.toLowerCase()}">${question.difficulty}</span>
            </td>
            <td>${question.category}</td>
            <td>${question.acceptance}%</td>
            <td>
                <span class="status-badge status-${question.status.toLowerCase()}">${question.status}</span>
            </td>
            <td>
                <div class="action-buttons">
                    <button class="btn-icon edit" data-id="${question.id}" title="Edit">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn-icon delete" data-id="${question.id}" title="Delete">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </td>
        `;
        
        questionsTableBody.appendChild(row);
    });
    
    updatePagination();
    attachTableEventListeners();
}

// Update pagination
function updatePagination() {
    const totalPages = Math.ceil(filteredQuestions.length / itemsPerPage);
    const start = (currentPage - 1) * itemsPerPage + 1;
    const end = Math.min(currentPage * itemsPerPage, filteredQuestions.length);
    
    document.getElementById('paginationStart').textContent = filteredQuestions.length > 0 ? start : 0;
    document.getElementById('paginationEnd').textContent = end;
    document.getElementById('paginationTotal').textContent = filteredQuestions.length;
    
    prevPageBtn.disabled = currentPage === 1;
    nextPageBtn.disabled = currentPage === totalPages || filteredQuestions.length === 0;
    
    // Render page numbers
    const pageNumbers = document.getElementById('pageNumbers');
    pageNumbers.innerHTML = '';
    
    const maxVisiblePages = 5;
    let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2));
    let endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);
    
    if (endPage - startPage < maxVisiblePages - 1) {
        startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }
    
    for (let i = startPage; i <= endPage; i++) {
        const pageBtn = document.createElement('button');
        pageBtn.className = `page-number ${i === currentPage ? 'active' : ''}`;
        pageBtn.textContent = i;
        pageBtn.addEventListener('click', () => {
            currentPage = i;
            renderQuestions();
        });
        pageNumbers.appendChild(pageBtn);
    }
}

// Attach event listeners
function attachEventListeners() {
    // Search and filters
    searchInput.addEventListener('input', applyFilters);
    difficultyFilter.addEventListener('change', applyFilters);
    categoryFilter.addEventListener('change', applyFilters);
    statusFilter.addEventListener('change', applyFilters);
    
    // Add question button
    addQuestionBtn.addEventListener('click', openAddModal);
    
    // Modal close buttons
    closeModalBtn.addEventListener('click', closeModal);
    cancelBtn.addEventListener('click', closeModal);
    closeDeleteModalBtn.addEventListener('click', closeDeleteModal);
    cancelDeleteBtn.addEventListener('click', closeDeleteModal);
    
    // Form submission
    questionForm.addEventListener('submit', handleFormSubmit);
    
    // Select all checkbox
    selectAllCheckbox.addEventListener('change', handleSelectAll);
    
    // Bulk delete
    bulkDeleteBtn.addEventListener('click', handleBulkDelete);
    
    // Export
    exportBtn.addEventListener('click', handleExport);
    
    // Add example button
    addExampleBtn.addEventListener('click', addExample);
    
    // Pagination
    prevPageBtn.addEventListener('click', () => {
        if (currentPage > 1) {
            currentPage--;
            renderQuestions();
        }
    });
    
    nextPageBtn.addEventListener('click', () => {
        const totalPages = Math.ceil(filteredQuestions.length / itemsPerPage);
        if (currentPage < totalPages) {
            currentPage++;
            renderQuestions();
        }
    });
    
    // Close modal on outside click
    questionModal.addEventListener('click', (e) => {
        if (e.target === questionModal) {
            closeModal();
        }
    });
    
    deleteModal.addEventListener('click', (e) => {
        if (e.target === deleteModal) {
            closeDeleteModal();
        }
    });
}

// Attach table event listeners
function attachTableEventListeners() {
    // Question checkboxes
    document.querySelectorAll('.question-checkbox').forEach(checkbox => {
        checkbox.addEventListener('change', handleCheckboxChange);
    });
    
    // Edit buttons
    document.querySelectorAll('.btn-icon.edit').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const id = parseInt(e.currentTarget.getAttribute('data-id'));
            openEditModal(id);
        });
    });
    
    // Delete buttons
    document.querySelectorAll('.btn-icon.delete').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const id = parseInt(e.currentTarget.getAttribute('data-id'));
            openDeleteModal(id);
        });
    });
    
    // Question titles (view detail)
    document.querySelectorAll('.question-title').forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const id = parseInt(e.currentTarget.getAttribute('data-id'));
            openEditModal(id);
        });
    });
}

// Apply filters
function applyFilters() {
    const searchTerm = searchInput.value.toLowerCase();
    const difficulty = difficultyFilter.value;
    const category = categoryFilter.value;
    const status = statusFilter.value;
    
    filteredQuestions = questionsData.filter(question => {
        const matchesSearch = question.title.toLowerCase().includes(searchTerm) ||
                            question.tags.some(tag => tag.toLowerCase().includes(searchTerm));
        const matchesDifficulty = !difficulty || question.difficulty === difficulty;
        const matchesCategory = !category || question.category === category;
        const matchesStatus = !status || question.status === status;
        
        return matchesSearch && matchesDifficulty && matchesCategory && matchesStatus;
    });
    
    currentPage = 1;
    selectedQuestions.clear();
    selectAllCheckbox.checked = false;
    renderQuestions();
}

// Handle select all checkbox
function handleSelectAll(e) {
    const isChecked = e.target.checked;
    const start = (currentPage - 1) * itemsPerPage;
    const end = start + itemsPerPage;
    const paginatedQuestions = filteredQuestions.slice(start, end);
    
    paginatedQuestions.forEach(question => {
        if (isChecked) {
            selectedQuestions.add(question.id);
        } else {
            selectedQuestions.delete(question.id);
        }
    });
    
    document.querySelectorAll('.question-checkbox').forEach(checkbox => {
        checkbox.checked = isChecked;
    });
    
    updateBulkDeleteButton();
}

// Handle individual checkbox change
function handleCheckboxChange(e) {
    const id = parseInt(e.target.getAttribute('data-id'));
    
    if (e.target.checked) {
        selectedQuestions.add(id);
    } else {
        selectedQuestions.delete(id);
        selectAllCheckbox.checked = false;
    }
    
    updateBulkDeleteButton();
}

// Update bulk delete button state
function updateBulkDeleteButton() {
    bulkDeleteBtn.disabled = selectedQuestions.size === 0;
    bulkDeleteBtn.innerHTML = selectedQuestions.size > 0 
        ? `<i class="fas fa-trash"></i> Delete Selected (${selectedQuestions.size})`
        : `<i class="fas fa-trash"></i> Delete Selected`;
}

// Open add modal
function openAddModal() {
    editingQuestionId = null;
    document.getElementById('modalTitle').textContent = 'Add New Question';
    document.getElementById('submitBtnText').textContent = 'Save Question';
    questionForm.reset();
    
    // Reset examples to 1
    const examplesContainer = document.getElementById('examplesContainer');
    examplesContainer.innerHTML = createExampleHTML(1);
    attachExampleListeners();
    
    questionModal.classList.add('active');
    document.body.style.overflow = 'hidden';
}

// Open edit modal
function openEditModal(id) {
    const question = questionsData.find(q => q.id === id);
    if (!question) return;
    
    editingQuestionId = id;
    document.getElementById('modalTitle').textContent = 'Edit Question';
    document.getElementById('submitBtnText').textContent = 'Update Question';
    
    // Populate form
    document.getElementById('questionTitle').value = question.title;
    document.getElementById('questionDifficulty').value = question.difficulty;
    document.getElementById('questionCategory').value = question.category;
    document.getElementById('questionTags').value = question.tags.join(', ');
    document.getElementById('questionCompanies').value = question.companies.join(', ');
    document.getElementById('questionAcceptance').value = question.acceptance;
    document.getElementById('questionDescription').value = question.description;
    document.getElementById('questionConstraints').value = question.constraints;
    document.getElementById('questionSolution').value = question.solution;
    document.getElementById('questionHints').value = question.hints.join('\n');
    document.getElementById('questionTimeComplexity').value = question.timeComplexity;
    document.getElementById('questionSpaceComplexity').value = question.spaceComplexity;
    document.getElementById('questionStatus').value = question.status;
    document.getElementById('questionOrder').value = question.order;
    
    // Populate examples
    const examplesContainer = document.getElementById('examplesContainer');
    examplesContainer.innerHTML = '';
    question.examples.forEach((example, index) => {
        const exampleHTML = createExampleHTML(index + 1);
        examplesContainer.insertAdjacentHTML('beforeend', exampleHTML);
        
        const exampleItem = examplesContainer.lastElementChild;
        exampleItem.querySelector('.example-input').value = example.input;
        exampleItem.querySelector('.example-output').value = example.output;
        exampleItem.querySelector('.example-explanation').value = example.explanation || '';
    });
    
    attachExampleListeners();
    
    questionModal.classList.add('active');
    document.body.style.overflow = 'hidden';
}

// Create example HTML
function createExampleHTML(number) {
    return `
        <div class="example-item" data-example="${number}">
            <div class="example-header">
                <h4>Example ${number}</h4>
                <button type="button" class="btn btn-outline btn-sm remove-example-btn" data-example="${number}">
                    <i class="fas fa-times"></i>
                </button>
            </div>
            <div class="form-group">
                <label>Input</label>
                <input type="text" class="example-input" placeholder="e.g., nums = [2,7,11,15], target = 9">
            </div>
            <div class="form-group">
                <label>Output</label>
                <input type="text" class="example-output" placeholder="e.g., [0,1]">
            </div>
            <div class="form-group">
                <label>Explanation (optional)</label>
                <textarea class="example-explanation" rows="2" placeholder="Explain the example..."></textarea>
            </div>
        </div>
    `;
}

// Add example
function addExample() {
    const examplesContainer = document.getElementById('examplesContainer');
    const currentExamples = examplesContainer.querySelectorAll('.example-item').length;
    const newNumber = currentExamples + 1;
    
    examplesContainer.insertAdjacentHTML('beforeend', createExampleHTML(newNumber));
    attachExampleListeners();
}

// Attach example listeners
function attachExampleListeners() {
    document.querySelectorAll('.remove-example-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            const exampleItem = e.target.closest('.example-item');
            const examplesContainer = document.getElementById('examplesContainer');
            
            // Don't allow removing if it's the only example
            if (examplesContainer.querySelectorAll('.example-item').length > 1) {
                exampleItem.remove();
                
                // Renumber remaining examples
                examplesContainer.querySelectorAll('.example-item').forEach((item, index) => {
                    item.setAttribute('data-example', index + 1);
                    item.querySelector('h4').textContent = `Example ${index + 1}`;
                    item.querySelector('.remove-example-btn').setAttribute('data-example', index + 1);
                });
            } else {
                showToast('At least one example is required', 'error');
            }
        });
    });
}

// Close modal
function closeModal() {
    questionModal.classList.remove('active');
    document.body.style.overflow = '';
    questionForm.reset();
    editingQuestionId = null;
}

// Handle form submission
function handleFormSubmit(e) {
    e.preventDefault();
    
    // Collect form data
    const formData = {
        title: document.getElementById('questionTitle').value,
        difficulty: document.getElementById('questionDifficulty').value,
        category: document.getElementById('questionCategory').value,
        tags: document.getElementById('questionTags').value.split(',').map(t => t.trim()).filter(t => t),
        companies: document.getElementById('questionCompanies').value.split(',').map(c => c.trim()).filter(c => c),
        acceptance: parseFloat(document.getElementById('questionAcceptance').value) || 0,
        description: document.getElementById('questionDescription').value,
        constraints: document.getElementById('questionConstraints').value,
        solution: document.getElementById('questionSolution').value,
        hints: document.getElementById('questionHints').value.split('\n').filter(h => h.trim()),
        timeComplexity: document.getElementById('questionTimeComplexity').value,
        spaceComplexity: document.getElementById('questionSpaceComplexity').value,
        status: document.getElementById('questionStatus').value,
        order: parseInt(document.getElementById('questionOrder').value) || questionsData.length + 1,
        examples: []
    };
    
    // Collect examples
    document.querySelectorAll('.example-item').forEach(item => {
        const input = item.querySelector('.example-input').value;
        const output = item.querySelector('.example-output').value;
        const explanation = item.querySelector('.example-explanation').value;
        
        if (input && output) {
            formData.examples.push({ input, output, explanation });
        }
    });
    
    if (editingQuestionId) {
        // Update existing question
        const index = questionsData.findIndex(q => q.id === editingQuestionId);
        questionsData[index] = { ...questionsData[index], ...formData };
        showToast('Question updated successfully!');
    } else {
        // Add new question
        const newQuestion = {
            id: questionsData.length + 1,
            ...formData
        };
        questionsData.push(newQuestion);
        showToast('Question added successfully!');
    }
    
    updateStats();
    applyFilters();
    closeModal();
}

// Open delete modal
let questionToDelete = null;

function openDeleteModal(id) {
    questionToDelete = id;
    deleteModal.classList.add('active');
    document.body.style.overflow = 'hidden';
}

// Close delete modal
function closeDeleteModal() {
    deleteModal.classList.remove('active');
    document.body.style.overflow = '';
    questionToDelete = null;
}

// Confirm delete
confirmDeleteBtn.addEventListener('click', () => {
    if (questionToDelete) {
        questionsData = questionsData.filter(q => q.id !== questionToDelete);
        showToast('Question deleted successfully!');
        updateStats();
        applyFilters();
        closeDeleteModal();
    }
});

// Handle bulk delete
function handleBulkDelete() {
    if (selectedQuestions.size === 0) return;
    
    if (confirm(`Are you sure you want to delete ${selectedQuestions.size} question(s)?`)) {
        questionsData = questionsData.filter(q => !selectedQuestions.has(q.id));
        selectedQuestions.clear();
        selectAllCheckbox.checked = false;
        showToast(`${selectedQuestions.size} question(s) deleted successfully!`);
        updateStats();
        applyFilters();
        updateBulkDeleteButton();
    }
}

// Handle export
function handleExport() {
    // Create CSV content
    let csv = 'ID,Title,Difficulty,Category,Tags,Companies,Acceptance,Status\n';
    
    questionsData.forEach(question => {
        csv += `${question.id},"${question.title}",${question.difficulty},${question.category},"${question.tags.join('; ')}","${question.companies.join('; ')}",${question.acceptance}%,${question.status}\n`;
    });
    
    // Download CSV
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'questions_export.csv';
    a.click();
    window.URL.revokeObjectURL(url);
    
    showToast('Questions exported successfully!');
}

// Show toast notification
function showToast(message, type = 'success') {
    const toast = document.getElementById('toast');
    const toastMessage = document.getElementById('toastMessage');
    const icon = toast.querySelector('i');
    
    toastMessage.textContent = message;
    
    if (type === 'error') {
        icon.className = 'fas fa-exclamation-circle';
        toast.style.background = 'linear-gradient(135deg, #dc2626 0%, #b91c1c 100%)';
    } else {
        icon.className = 'fas fa-check-circle';
        toast.style.background = 'linear-gradient(135deg, #10b981 0%, #059669 100%)';
    }
    
    toast.classList.add('show');
    
    setTimeout(() => {
        toast.classList.remove('show');
    }, 3000);
}
