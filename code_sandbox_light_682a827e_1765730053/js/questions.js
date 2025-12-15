// Questions Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Dropdown functionality
    const filterBtns = document.querySelectorAll('.filter-btn');
    const filterDropdowns = document.querySelectorAll('.filter-dropdown');
    
    // Toggle dropdown
    filterBtns.forEach(btn => {
        btn.addEventListener('click', function(e) {
            e.stopPropagation();
            const dropdownId = this.id.replace('Btn', '') + 'Dropdown';
            const dropdown = document.getElementById(dropdownId);
            
            // Close other dropdowns
            filterDropdowns.forEach(d => {
                if (d.id !== dropdownId) {
                    d.classList.remove('show');
                }
            });
            
            filterBtns.forEach(b => {
                if (b !== this) {
                    b.classList.remove('active');
                }
            });
            
            // Toggle current dropdown
            dropdown.classList.toggle('show');
            this.classList.toggle('active');
        });
    });
    
    // Close dropdowns when clicking outside
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.dropdown-filter')) {
            filterDropdowns.forEach(dropdown => {
                dropdown.classList.remove('show');
            });
            filterBtns.forEach(btn => {
                btn.classList.remove('active');
            });
        }
    });
    
    // Prevent dropdown from closing when clicking inside
    filterDropdowns.forEach(dropdown => {
        dropdown.addEventListener('click', function(e) {
            e.stopPropagation();
        });
    });
    
    // Handle "All" checkbox logic for each dropdown
    function setupAllCheckbox(dropdownId, filterName) {
        const dropdown = document.getElementById(dropdownId);
        if (!dropdown) return;
        
        const allCheckbox = dropdown.querySelector(`input[value="all"]`);
        const otherCheckboxes = dropdown.querySelectorAll(`input[name="${filterName}"]:not([value="all"])`);
        
        if (allCheckbox) {
            // When "All" is checked, uncheck others
            allCheckbox.addEventListener('change', function() {
                if (this.checked) {
                    otherCheckboxes.forEach(cb => cb.checked = false);
                }
                applyFilters();
            });
        }
        
        // When any other checkbox is checked, uncheck "All"
        otherCheckboxes.forEach(cb => {
            cb.addEventListener('change', function() {
                if (this.checked && allCheckbox) {
                    allCheckbox.checked = false;
                }
                // If no checkboxes are checked, check "All"
                const anyChecked = Array.from(otherCheckboxes).some(c => c.checked);
                if (!anyChecked && allCheckbox) {
                    allCheckbox.checked = true;
                }
                applyFilters();
            });
        });
    }
    
    // Setup all dropdowns
    setupAllCheckbox('typeDropdown', 'type');
    setupAllCheckbox('companyDropdown', 'company');
    setupAllCheckbox('categoryDropdown', 'category');
    setupAllCheckbox('difficultyDropdown', 'difficulty');
    
    // Apply filters function
    function applyFilters() {
        // Collect selected filters
        const getSelectedValues = (name) => {
            const checkboxes = document.querySelectorAll(`input[name="${name}"]:checked:not([value="all"])`);
            return Array.from(checkboxes).map(cb => cb.value);
        };
        
        const selectedTypes = getSelectedValues('type');
        const selectedCompanies = getSelectedValues('company');
        const selectedCategories = getSelectedValues('category');
        const selectedDifficulties = getSelectedValues('difficulty');
        
        console.log('Filters:', { 
            selectedTypes,
            selectedCompanies,
            selectedCategories,
            selectedDifficulties
        });
        
        // In a real app, would filter questions based on selected filters
    }
    
    // Sidebar search
    const sidebarSearch = document.querySelector('.sidebar-search input');
    if (sidebarSearch) {
        sidebarSearch.addEventListener('input', function(e) {
            const searchTerm = e.target.value.toLowerCase();
            if (searchTerm.length > 2) {
                console.log('Searching for:', searchTerm);
            }
        });
    }
    
    // Expand button functionality
    const expandBtns = document.querySelectorAll('.expand-btn');
    expandBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const previewText = this.previousElementSibling;
            if (previewText.style.whiteSpace === 'normal') {
                previewText.style.whiteSpace = 'nowrap';
                this.querySelector('i').style.transform = 'rotate(0deg)';
            } else {
                previewText.style.whiteSpace = 'normal';
                this.querySelector('i').style.transform = 'rotate(180deg)';
            }
        });
    });
    
    // Save button functionality
    const saveBtns = document.querySelectorAll('.action-btn');
    saveBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const icon = this.querySelector('i');
            if (icon.classList.contains('far')) {
                icon.classList.remove('far');
                icon.classList.add('fas');
                if (window.vectorApp) {
                    window.vectorApp.showToast('Question saved', 'success');
                }
            } else {
                icon.classList.remove('fas');
                icon.classList.add('far');
                if (window.vectorApp) {
                    window.vectorApp.showToast('Question removed', 'info');
                }
            }
        });
    });
    
    // Topic card clicks
    const topicCards = document.querySelectorAll('.topic-card');
    topicCards.forEach(card => {
        card.addEventListener('click', function() {
            const title = this.querySelector('h3').textContent;
            if (window.vectorApp) {
                window.vectorApp.showToast(`Filtering: ${title}`, 'info');
            }
        });
    });
    
    // Pagination
    const pagination = document.querySelector('.pagination');
    if (pagination) {
        const pageButtons = pagination.querySelectorAll('.page-btn');
        
        pageButtons.forEach(button => {
            button.addEventListener('click', function() {
                // Skip if disabled or already active
                if (this.classList.contains('disabled') || this.classList.contains('active')) {
                    return;
                }
                
                // Handle prev/next buttons
                if (this.querySelector('i')) {
                    const isNext = this.querySelector('.fa-chevron-right');
                    const activePage = pagination.querySelector('.page-btn.active');
                    const pageNumber = parseInt(activePage.textContent);
                    
                    if (isNext) {
                        console.log('Next page:', pageNumber + 1);
                    } else {
                        console.log('Previous page:', pageNumber - 1);
                    }
                } else if (this.textContent !== '...') {
                    // Handle numbered page buttons
                    const pageNumber = parseInt(this.textContent);
                    console.log('Go to page:', pageNumber);
                    
                    // Remove active class from all buttons
                    pageButtons.forEach(btn => btn.classList.remove('active'));
                    
                    // Add active class to clicked button
                    this.classList.add('active');
                    
                    // Scroll to top
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                    
                    // Show toast
                    if (window.vectorApp) {
                        window.vectorApp.showToast(`Page ${pageNumber} loaded`, 'info');
                    }
                }
            });
        });
    }
    
    // Role tags
    const roleTags = document.querySelectorAll('.role-tag');
    roleTags.forEach(tag => {
        tag.addEventListener('click', function(e) {
            e.preventDefault();
            const role = this.textContent;
            if (window.vectorApp) {
                window.vectorApp.showToast(`Filtering by: ${role}`, 'info');
            }
        });
    });
    
    // Company items
    const companyItems = document.querySelectorAll('.company-item');
    companyItems.forEach(item => {
        item.addEventListener('click', function(e) {
            e.preventDefault();
            const company = this.querySelector('span').textContent;
            if (window.vectorApp) {
                window.vectorApp.showToast(`Filtering by: ${company}`, 'info');
            }
        });
    });
    
    // Share interview experience button
    const shareBtn = document.querySelector('.btn-share');
    if (shareBtn) {
        shareBtn.addEventListener('click', function() {
            if (window.vectorApp) {
                window.vectorApp.showToast('Opening interview experience form...', 'info');
            }
            // In a real app, would open a modal or navigate to experience form
        });
    }
});
