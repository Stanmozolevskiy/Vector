/**
 * Enhanced Dropdown Menu Behavior
 * 
 * Features:
 * - Click to toggle dropdown
 * - Hover support with smooth transitions
 * - Debounced closing to prevent accidental dismissals
 * - Click-outside detection
 * - Works on both desktop and mobile
 */

document.addEventListener('DOMContentLoaded', function() {
  const userDropdown = document.getElementById('userDropdown');
  
  if (!userDropdown) return;
  
  const dropdownMenu = userDropdown.querySelector('.dropdown-menu');
  let closeTimeout;

  /**
   * Show the dropdown menu
   */
  function showDropdown() {
    if (closeTimeout) {
      clearTimeout(closeTimeout);
      closeTimeout = null;
    }
    dropdownMenu.classList.add('show');
  }

  /**
   * Hide the dropdown menu with a delay
   */
  function hideDropdown() {
    closeTimeout = setTimeout(() => {
      dropdownMenu.classList.remove('show');
    }, 100); // 100ms delay before closing
  }

  /**
   * Hide dropdown immediately (for click outside)
   */
  function hideDropdownImmediately() {
    if (closeTimeout) {
      clearTimeout(closeTimeout);
      closeTimeout = null;
    }
    dropdownMenu.classList.remove('show');
  }

  /**
   * Toggle dropdown visibility
   */
  function toggleDropdown() {
    if (dropdownMenu.classList.contains('show')) {
      hideDropdownImmediately();
    } else {
      showDropdown();
    }
  }

  // Click event on user menu trigger
  userDropdown.addEventListener('click', function(e) {
    e.stopPropagation();
    toggleDropdown();
  });

  // Hover events for smooth interaction
  userDropdown.addEventListener('mouseenter', function() {
    showDropdown();
  });

  userDropdown.addEventListener('mouseleave', function() {
    hideDropdown();
  });

  // Keep dropdown open when hovering over it
  dropdownMenu.addEventListener('mouseenter', function() {
    if (closeTimeout) {
      clearTimeout(closeTimeout);
      closeTimeout = null;
    }
  });

  dropdownMenu.addEventListener('mouseleave', function() {
    hideDropdown();
  });

  // Close dropdown when clicking outside
  document.addEventListener('click', function(e) {
    if (!userDropdown.contains(e.target)) {
      hideDropdownImmediately();
    }
  });

  // Close dropdown when clicking on a dropdown item (except the trigger)
  const dropdownItems = dropdownMenu.querySelectorAll('.dropdown-item');
  dropdownItems.forEach(item => {
    item.addEventListener('click', function() {
      hideDropdownImmediately();
    });
  });

  // Handle ESC key to close dropdown
  document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape' && dropdownMenu.classList.contains('show')) {
      hideDropdownImmediately();
    }
  });
});

