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
    const resetBtn = document.querySelector('.filters-sidebar .btn-outline');
    if (resetBtn) {
        resetBtn.addEventListener('click', function() {
            filterCheckboxes.forEach(checkbox => {
                checkbox.checked = false;
            });
            window.vectorApp.showToast('Filters reset', 'info');
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
    const pageButtons = document.querySelectorAll('.page-btn:not(.disabled)');
    pageButtons.forEach(button => {
        button.addEventListener('click', function() {
            if (!this.classList.contains('active')) {
                document.querySelectorAll('.page-btn').forEach(btn => {
                    btn.classList.remove('active');
                });
                this.classList.add('active');
                window.scrollTo({ top: 0, behavior: 'smooth' });
            }
        });
    });
});