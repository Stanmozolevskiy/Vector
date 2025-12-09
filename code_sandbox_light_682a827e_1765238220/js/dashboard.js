// Dashboard JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Initialize Chart.js for activity chart
    const ctx = document.getElementById('activityChart');
    if (ctx) {
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
                datasets: [{
                    label: 'Problems Solved',
                    data: [12, 19, 15, 25, 22, 18, 24],
                    borderColor: '#6366f1',
                    backgroundColor: 'rgba(99, 102, 241, 0.1)',
                    tension: 0.4,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: '#f1f5f9'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
    }
    
    // Load user data
    const user = window.vectorApp.storage.get('user');
    if (user) {
        // Update user name in dashboard header
        const headerName = document.querySelector('.dashboard-header h1');
        if (headerName) {
            headerName.textContent = `Welcome back, ${user.name.split(' ')[0]}!`;
        }
        
        // Update user avatar
        const userAvatar = document.querySelector('.user-avatar');
        if (userAvatar) {
            const initials = user.name.split(' ').map(n => n[0]).join('');
            userAvatar.textContent = initials;
        }
        
        const userName = document.querySelector('.user-menu span');
        if (userName) {
            userName.textContent = user.name;
        }
    }
    
    // Logout functionality
    const logoutLinks = document.querySelectorAll('a[href*="index.html"]');
    logoutLinks.forEach(link => {
        if (link.querySelector('.fa-sign-out-alt')) {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                window.vectorApp.storage.remove('isAuthenticated');
                window.vectorApp.storage.remove('user');
                window.vectorApp.showToast('Logged out successfully', 'success');
                setTimeout(() => {
                    window.location.href = 'index.html';
                }, 1000);
            });
        }
    });
    
    // Goal checkboxes
    const goalCheckboxes = document.querySelectorAll('.goal-checkbox');
    goalCheckboxes.forEach((checkbox, index) => {
        checkbox.addEventListener('click', function() {
            const goalText = this.nextElementSibling;
            const isCompleted = goalText.classList.contains('completed');
            
            if (isCompleted) {
                goalText.classList.remove('completed');
                this.innerHTML = '';
            } else {
                goalText.classList.add('completed');
                this.innerHTML = '<i class="fas fa-check"></i>';
                window.vectorApp.showToast('Goal completed!', 'success');
            }
        });
    });
});