// Authentication JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Login Form
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;
            
            // Validate inputs
            if (!window.vectorApp.validateEmail(email)) {
                window.vectorApp.showToast('Please enter a valid email address', 'error');
                return;
            }
            
            if (!window.vectorApp.validatePassword(password)) {
                window.vectorApp.showToast('Password must be at least 8 characters', 'error');
                return;
            }
            
            // Simulate login (in real app, would call API)
            window.vectorApp.showToast('Logging in...', 'info');
            
            setTimeout(() => {
                // Set authentication flag
                window.vectorApp.storage.set('isAuthenticated', true);
                window.vectorApp.storage.set('user', {
                    email: email,
                    name: 'John Smith'
                });
                
                window.vectorApp.showToast('Login successful!', 'success');
                
                // Redirect to dashboard
                setTimeout(() => {
                    window.location.href = 'dashboard.html';
                }, 1000);
            }, 1500);
        });
    }
    
    // Register Form
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        registerForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const firstName = document.getElementById('firstName').value;
            const lastName = document.getElementById('lastName').value;
            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;
            const confirmPassword = document.getElementById('confirmPassword').value;
            const termsAccepted = document.querySelector('input[name="terms"]').checked;
            
            // Validate inputs
            if (!firstName || !lastName) {
                window.vectorApp.showToast('Please enter your full name', 'error');
                return;
            }
            
            if (!window.vectorApp.validateEmail(email)) {
                window.vectorApp.showToast('Please enter a valid email address', 'error');
                return;
            }
            
            if (!window.vectorApp.validatePassword(password)) {
                window.vectorApp.showToast('Password must be at least 8 characters', 'error');
                return;
            }
            
            if (password !== confirmPassword) {
                window.vectorApp.showToast('Passwords do not match', 'error');
                return;
            }
            
            if (!termsAccepted) {
                window.vectorApp.showToast('Please accept the terms of service', 'error');
                return;
            }
            
            // Simulate registration (in real app, would call API)
            window.vectorApp.showToast('Creating your account...', 'info');
            
            setTimeout(() => {
                // Set authentication flag
                window.vectorApp.storage.set('isAuthenticated', true);
                window.vectorApp.storage.set('user', {
                    email: email,
                    name: `${firstName} ${lastName}`
                });
                
                window.vectorApp.showToast('Account created successfully!', 'success');
                
                // Redirect to dashboard
                setTimeout(() => {
                    window.location.href = 'dashboard.html';
                }, 1000);
            }, 1500);
        });
    }
    
    // Forgot Password Form
    const forgotPasswordForm = document.getElementById('forgotPasswordForm');
    if (forgotPasswordForm) {
        forgotPasswordForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const email = document.getElementById('email').value;
            
            if (!window.vectorApp.validateEmail(email)) {
                window.vectorApp.showToast('Please enter a valid email address', 'error');
                return;
            }
            
            // Simulate password reset (in real app, would call API)
            window.vectorApp.showToast('Sending reset link...', 'info');
            
            setTimeout(() => {
                window.vectorApp.showToast('Password reset link sent to your email!', 'success');
                
                setTimeout(() => {
                    window.location.href = 'login.html';
                }, 2000);
            }, 1500);
        });
    }
    
    // Social login buttons
    const socialButtons = document.querySelectorAll('.social-btn');
    socialButtons.forEach(button => {
        button.addEventListener('click', function() {
            const provider = this.querySelector('span').textContent;
            window.vectorApp.showToast(`${provider} login coming soon!`, 'info');
        });
    });
});