// Questions Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Search functionality
    const searchInput = document.querySelector('.search-box input');
    if (searchInput) {
        searchInput.addEventListener('input', function(e) {
            const searchTerm = e.target.value.toLowerCase();
            // In a real app, would filter questions based on search term
            if (searchTerm.length > 2) {
                console.log('Searching for:', searchTerm);
            }
        });
    }
    
    // Filter tags
    const filterTags = document.querySelectorAll('.filter-tag');
    filterTags.forEach(tag => {
        tag.addEventListener('click', function() {
            // Remove active class from all tags
            filterTags.forEach(t => t.classList.remove('active'));
            // Add active class to clicked tag
            this.classList.add('active');
            
            const category = this.textContent;
            window.vectorApp.showToast(`Showing ${category} questions`, 'info');
        });
    });
    
    // Filter checkboxes
    const filterCheckboxes = document.querySelectorAll('.filters-sidebar input[type="checkbox"]');
    filterCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            applyFilters();
        });
    });
    
    function applyFilters() {
        // Collect selected filters
        const selectedDifficulties = Array.from(document.querySelectorAll('input[name="difficulty"]:checked'))
            .map(cb => cb.value);
        const selectedStatuses = Array.from(document.querySelectorAll('input[name="status"]:checked'))
            .map(cb => cb.value);
        const selectedCompanies = Array.from(document.querySelectorAll('input[name="company"]:checked'))
            .map(cb => cb.value);
        
        console.log('Filters:', { selectedDifficulties, selectedStatuses, selectedCompanies });
        // In a real app, would filter questions table
    }
    
    // Reset filters
    const resetBtn = document.getElementById('resetFiltersBtn');
    if (resetBtn) {
        resetBtn.addEventListener('click', function() {
            // Uncheck all status and company filters
            document.querySelectorAll('input[name="status"]').forEach(checkbox => {
                checkbox.checked = false;
            });
            document.querySelectorAll('input[name="company"]').forEach(checkbox => {
                checkbox.checked = false;
            });
            // Check all difficulty filters (default state)
            document.querySelectorAll('input[name="difficulty"]').forEach(checkbox => {
                checkbox.checked = true;
            });
            window.vectorApp.showToast('Filters reset', 'info');
            applyFilters();
        });
    }
    
    // Solve button
    const solveButtons = document.querySelectorAll('.btn-solve');
    solveButtons.forEach(button => {
        button.addEventListener('click', function() {
            const questionTitle = this.closest('tr').querySelector('.question-title').textContent;
            window.vectorApp.showToast(`Opening: ${questionTitle}`, 'success');
            // Would navigate to question detail page
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
                        // Go to next page
                        console.log('Next page:', pageNumber + 1);
                    } else {
                        // Go to previous page
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
                    window.vectorApp.showToast(`Page ${pageNumber} loaded`, 'info');
                }
            });
        });
    }
});