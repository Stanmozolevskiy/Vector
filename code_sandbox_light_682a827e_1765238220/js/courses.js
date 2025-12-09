// Courses Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Filter functionality
    const filterOptions = document.querySelectorAll('.filter-option input[type="checkbox"]');
    filterOptions.forEach(checkbox => {
        checkbox.addEventListener('change', function() {
            applyFilters();
        });
    });
    
    function applyFilters() {
        // In a real app, this would filter courses based on selected options
        window.vectorApp.showToast('Filters applied', 'info');
    }
    
    // Clear filters
    const clearBtn = document.querySelector('.filters-sidebar .btn-outline');
    if (clearBtn) {
        clearBtn.addEventListener('click', function() {
            filterOptions.forEach(checkbox => {
                checkbox.checked = false;
            });
            window.vectorApp.showToast('Filters cleared', 'info');
        });
    }
    
    // Sort dropdown
    const sortSelect = document.getElementById('sort');
    if (sortSelect) {
        sortSelect.addEventListener('change', function() {
            window.vectorApp.showToast(`Sorted by: ${this.options[this.selectedIndex].text}`, 'info');
            // In a real app, would sort courses
        });
    }
    
    // Enroll button
    const enrollBtn = document.querySelector('.course-card-sticky .btn-primary');
    if (enrollBtn) {
        enrollBtn.addEventListener('click', function() {
            window.vectorApp.showToast('Enrolling in course...', 'success');
            setTimeout(() => {
                window.location.href = 'lesson-player.html';
            }, 1500);
        });
    }
    
    // Add to wishlist
    const wishlistBtn = document.querySelector('.course-card-sticky .btn-outline');
    if (wishlistBtn) {
        wishlistBtn.addEventListener('click', function() {
            if (this.textContent.includes('Add')) {
                this.innerHTML = '<i class="fas fa-heart"></i> Remove from Wishlist';
                window.vectorApp.showToast('Added to wishlist', 'success');
            } else {
                this.innerHTML = '<i class="far fa-heart"></i> Add to Wishlist';
                window.vectorApp.showToast('Removed from wishlist', 'info');
            }
        });
    }
});