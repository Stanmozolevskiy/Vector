// Profile Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Load user data
    const user = window.vectorApp.storage.get('user') || {
        firstName: 'John',
        lastName: 'Smith',
        email: 'john.smith@example.com',
        username: 'johnsmith',
        bio: 'Software engineer passionate about learning and preparing for interviews at top tech companies.',
        phone: '+1 (555) 123-4567',
        location: 'San Francisco, CA'
    };
    
    // Update profile data in form
    if (document.getElementById('firstName')) {
        document.getElementById('firstName').value = user.firstName || 'John';
        document.getElementById('lastName').value = user.lastName || 'Smith';
        document.getElementById('email').value = user.email || '';
        document.getElementById('username').value = user.username || '';
        document.getElementById('bio').value = user.bio || '';
        document.getElementById('phone').value = user.phone || '';
        document.getElementById('location').value = user.location || '';
    }
    
    // Section Navigation
    const navItems = document.querySelectorAll('.profile-nav-item');
    const sections = document.querySelectorAll('.profile-section-content');
    
    navItems.forEach(item => {
        item.addEventListener('click', function(e) {
            e.preventDefault();
            const sectionId = this.dataset.section;
            
            // Update active nav item
            navItems.forEach(nav => nav.classList.remove('active'));
            this.classList.add('active');
            
            // Show corresponding section
            sections.forEach(section => section.classList.remove('active'));
            document.getElementById(sectionId).classList.add('active');
            
            // Scroll to top
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    });
    
    // Profile Picture Upload
    const uploadPictureBtn = document.getElementById('uploadPictureBtn');
    const pictureUpload = document.getElementById('pictureUpload');
    const removePicture = document.getElementById('removePicture');
    const profileAvatar = document.querySelector('.profile-avatar-large');
    
    if (uploadPictureBtn && pictureUpload) {
        uploadPictureBtn.addEventListener('click', function() {
            pictureUpload.click();
        });
        
        pictureUpload.addEventListener('change', function(e) {
            const file = e.target.files[0];
            if (file) {
                if (file.size > 5 * 1024 * 1024) {
                    window.vectorApp.showToast('File size must be less than 5MB', 'error');
                    return;
                }
                
                const reader = new FileReader();
                reader.onload = function(e) {
                    // In real app, would upload to server
                    window.vectorApp.showToast('Profile picture uploaded successfully!', 'success');
                    // Could set background-image of avatar here
                };
                reader.readAsDataURL(file);
            }
        });
    }
    
    if (removePicture) {
        removePicture.addEventListener('click', function(e) {
            e.stopPropagation();
            if (confirm('Remove profile picture?')) {
                window.vectorApp.showToast('Profile picture removed', 'info');
                // Reset to initials
            }
        });
    }
    
    // Personal Information Form
    const personalInfoForm = document.getElementById('personalInfoForm');
    if (personalInfoForm) {
        personalInfoForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const formData = {
                firstName: document.getElementById('firstName').value,
                lastName: document.getElementById('lastName').value,
                email: document.getElementById('email').value,
                username: document.getElementById('username').value,
                bio: document.getElementById('bio').value,
                phone: document.getElementById('phone').value,
                location: document.getElementById('location').value
            };
            
            // Validate
            if (!formData.firstName || !formData.lastName) {
                window.vectorApp.showToast('Please enter your full name', 'error');
                return;
            }
            
            if (!window.vectorApp.validateEmail(formData.email)) {
                window.vectorApp.showToast('Please enter a valid email', 'error');
                return;
            }
            
            if (formData.bio.length > 200) {
                window.vectorApp.showToast('Bio must be 200 characters or less', 'error');
                return;
            }
            
            // Save to storage
            window.vectorApp.storage.set('user', formData);
            
            // Update UI
            const userAvatar = document.querySelector('.user-avatar');
            const userName = document.querySelector('.user-menu span');
            if (userAvatar) {
                const initials = formData.firstName[0] + formData.lastName[0];
                userAvatar.textContent = initials;
            }
            if (userName) {
                userName.textContent = `${formData.firstName} ${formData.lastName}`;
            }
            
            window.vectorApp.showToast('Profile updated successfully!', 'success');
        });
    }
    
    // Cancel Personal Info
    const cancelPersonalInfo = document.getElementById('cancelPersonalInfo');
    if (cancelPersonalInfo) {
        cancelPersonalInfo.addEventListener('click', function() {
            // Reset form to stored values
            personalInfoForm.reset();
            window.vectorApp.showToast('Changes discarded', 'info');
        });
    }
    
    // Change Password Form
    const changePasswordForm = document.getElementById('changePasswordForm');
    if (changePasswordForm) {
        changePasswordForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const currentPassword = document.getElementById('currentPassword').value;
            const newPassword = document.getElementById('newPassword').value;
            const confirmNewPassword = document.getElementById('confirmNewPassword').value;
            
            // Validate
            if (!window.vectorApp.validatePassword(newPassword)) {
                window.vectorApp.showToast('New password must be at least 8 characters', 'error');
                return;
            }
            
            if (newPassword !== confirmNewPassword) {
                window.vectorApp.showToast('Passwords do not match', 'error');
                return;
            }
            
            if (currentPassword === newPassword) {
                window.vectorApp.showToast('New password must be different from current password', 'error');
                return;
            }
            
            // Simulate password change
            window.vectorApp.showToast('Password updated successfully!', 'success');
            changePasswordForm.reset();
        });
    }
    
    // Cancel Password
    const cancelPassword = document.getElementById('cancelPassword');
    if (cancelPassword) {
        cancelPassword.addEventListener('click', function() {
            changePasswordForm.reset();
            window.vectorApp.showToast('Changes discarded', 'info');
        });
    }
    
    // Enable 2FA
    const enable2FA = document.getElementById('enable2FA');
    if (enable2FA) {
        enable2FA.addEventListener('click', function() {
            window.vectorApp.showToast('Two-factor authentication setup coming soon!', 'info');
        });
    }
    
    // Revoke Session
    const revokeButtons = document.querySelectorAll('.session-item .btn-outline');
    revokeButtons.forEach(button => {
        if (button.textContent.includes('Revoke')) {
            button.addEventListener('click', function() {
                if (confirm('Are you sure you want to revoke this session?')) {
                    this.closest('.session-item').remove();
                    window.vectorApp.showToast('Session revoked', 'success');
                }
            });
        }
    });
    
    // Cancel Subscription Modal
    const cancelSubscriptionBtn = document.getElementById('cancelSubscription');
    const cancelSubscriptionModal = document.getElementById('cancelSubscriptionModal');
    const modalClose = document.querySelector('.modal-close');
    const modalCancel = document.querySelector('.modal-cancel');
    const confirmCancel = document.getElementById('confirmCancel');
    
    if (cancelSubscriptionBtn && cancelSubscriptionModal) {
        cancelSubscriptionBtn.addEventListener('click', function() {
            cancelSubscriptionModal.classList.add('active');
        });
        
        if (modalClose) {
            modalClose.addEventListener('click', function() {
                cancelSubscriptionModal.classList.remove('active');
            });
        }
        
        if (modalCancel) {
            modalCancel.addEventListener('click', function() {
                cancelSubscriptionModal.classList.remove('active');
            });
        }
        
        if (confirmCancel) {
            confirmCancel.addEventListener('click', function() {
                window.vectorApp.showToast('Subscription canceled. Access continues until Feb 15, 2025', 'success');
                cancelSubscriptionModal.classList.remove('active');
                
                // Update UI to show canceled state
                setTimeout(() => {
                    cancelSubscriptionBtn.textContent = 'Reactivate Subscription';
                    cancelSubscriptionBtn.classList.remove('btn-danger');
                    cancelSubscriptionBtn.classList.add('btn-primary');
                }, 500);
            });
        }
        
        // Close modal on background click
        cancelSubscriptionModal.addEventListener('click', function(e) {
            if (e.target === this) {
                this.classList.remove('active');
            }
        });
    }
    
    // Update Payment Method
    const updatePayment = document.getElementById('updatePayment');
    if (updatePayment) {
        updatePayment.addEventListener('click', function() {
            window.vectorApp.showToast('Redirecting to payment method update...', 'info');
            // Would redirect to Stripe or payment processor
        });
    }
    
    // Download Invoice
    const invoiceLinks = document.querySelectorAll('.invoice-link');
    invoiceLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            window.vectorApp.showToast('Downloading invoice...', 'success');
            // Would trigger PDF download
        });
    });
    
    // Notification Toggles
    const notificationToggles = document.querySelectorAll('.notification-item .toggle-switch input');
    notificationToggles.forEach(toggle => {
        toggle.addEventListener('change', function() {
            const notificationName = this.closest('.notification-item').querySelector('h4').textContent;
            const status = this.checked ? 'enabled' : 'disabled';
            window.vectorApp.showToast(`${notificationName} ${status}`, 'success');
        });
    });
    
    // Privacy Toggles
    const privacyToggles = document.querySelectorAll('.privacy-item .toggle-switch input');
    privacyToggles.forEach(toggle => {
        toggle.addEventListener('change', function() {
            const settingName = this.closest('.privacy-item').querySelector('h4').textContent;
            const status = this.checked ? 'enabled' : 'disabled';
            window.vectorApp.showToast(`${settingName} ${status}`, 'success');
        });
    });
    
    // Request Data Download
    const requestDataBtn = document.querySelector('.danger-actions .btn-outline');
    if (requestDataBtn) {
        requestDataBtn.addEventListener('click', function() {
            window.vectorApp.showToast('Data export request submitted. You will receive an email when ready.', 'success');
        });
    }
    
    // Delete Account
    const deleteAccountBtn = document.getElementById('deleteAccount');
    if (deleteAccountBtn) {
        deleteAccountBtn.addEventListener('click', function() {
            const confirmation = prompt('This action cannot be undone. Type "DELETE" to confirm:');
            if (confirmation === 'DELETE') {
                window.vectorApp.showToast('Account deletion initiated. You will receive a confirmation email.', 'error');
                setTimeout(() => {
                    window.vectorApp.storage.clear();
                    window.location.href = 'index.html';
                }, 3000);
            } else if (confirmation !== null) {
                window.vectorApp.showToast('Account deletion canceled', 'info');
            }
        });
    }
    
    // Character counter for bio
    const bioTextarea = document.getElementById('bio');
    if (bioTextarea) {
        const maxLength = 200;
        const createCounter = () => {
            const counter = document.createElement('small');
            counter.className = 'form-help char-counter';
            counter.style.float = 'right';
            return counter;
        };
        
        const counter = createCounter();
        bioTextarea.parentElement.appendChild(counter);
        
        const updateCounter = () => {
            const remaining = maxLength - bioTextarea.value.length;
            counter.textContent = `${remaining} characters remaining`;
            if (remaining < 0) {
                counter.style.color = 'var(--error-color)';
            } else if (remaining < 20) {
                counter.style.color = 'var(--warning-color)';
            } else {
                counter.style.color = 'var(--text-secondary)';
            }
        };
        
        bioTextarea.addEventListener('input', updateCounter);
        updateCounter();
    }
});