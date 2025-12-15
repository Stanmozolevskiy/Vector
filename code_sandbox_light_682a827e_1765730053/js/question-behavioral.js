// Question Behavioral Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Tab switching
    const tabs = document.querySelectorAll('.tab');
    const tabPanes = document.querySelectorAll('.tab-pane');
    
    tabs.forEach(tab => {
        tab.addEventListener('click', function() {
            const targetTab = this.getAttribute('data-tab');
            
            // Remove active class from all tabs and panes
            tabs.forEach(t => t.classList.remove('active'));
            tabPanes.forEach(pane => pane.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding pane
            this.classList.add('active');
            document.getElementById(targetTab).classList.add('active');
        });
    });
    
    // Character counter for textareas
    const textareas = {
        situation: document.getElementById('situationInput'),
        task: document.getElementById('taskInput'),
        action: document.getElementById('actionInput'),
        result: document.getElementById('resultInput')
    };
    
    Object.keys(textareas).forEach(key => {
        const textarea = textareas[key];
        const counter = document.getElementById(`${key}Count`);
        
        if (textarea && counter) {
            textarea.addEventListener('input', function() {
                counter.textContent = this.value.length;
                
                // Auto-save to localStorage
                localStorage.setItem(`behavioral_${key}`, this.value);
                
                // Change color based on length
                const maxLength = parseInt(this.getAttribute('maxlength'));
                const percentage = (this.value.length / maxLength) * 100;
                
                if (percentage > 90) {
                    counter.style.color = '#ef4444';
                } else if (percentage > 75) {
                    counter.style.color = '#f59e0b';
                } else {
                    counter.style.color = '#9ca3af';
                }
            });
            
            // Load saved content
            const saved = localStorage.getItem(`behavioral_${key}`);
            if (saved) {
                textarea.value = saved;
                counter.textContent = saved.length;
            }
        }
    });
    
    // Timer functionality
    let timerInterval;
    let seconds = 0;
    const timerBtn = document.getElementById('timerBtn');
    const timerDisplay = document.querySelector('.timer-display');
    
    if (timerBtn && timerDisplay) {
        timerBtn.addEventListener('click', function() {
            if (timerInterval) {
                // Stop timer
                clearInterval(timerInterval);
                timerInterval = null;
                this.querySelector('i').classList.remove('fa-pause');
                this.querySelector('i').classList.add('fa-stopwatch');
            } else {
                // Start timer
                timerInterval = setInterval(() => {
                    seconds++;
                    const mins = Math.floor(seconds / 60);
                    const secs = seconds % 60;
                    timerDisplay.textContent = `${mins}:${secs.toString().padStart(2, '0')}`;
                }, 1000);
                this.querySelector('i').classList.remove('fa-stopwatch');
                this.querySelector('i').classList.add('fa-pause');
            }
        });
    }
    
    // Practice Aloud button
    const practiceBtn = document.getElementById('practiceBtn');
    if (practiceBtn) {
        practiceBtn.addEventListener('click', function() {
            // Check if all sections have content
            const allFilled = Object.values(textareas).every(textarea => textarea.value.trim().length > 0);
            
            if (!allFilled) {
                if (window.vectorApp) {
                    window.vectorApp.showToast('Please fill all STAR sections before practicing', 'error');
                }
                return;
            }
            
            // In a real app, would start voice recording or open practice mode
            if (window.vectorApp) {
                window.vectorApp.showToast('Practice mode activated. Start speaking your answer!', 'success');
            }
            
            // Start timer if not already running
            if (!timerInterval) {
                timerBtn.click();
            }
        });
    }
    
    // Get Feedback button
    const feedbackBtn = document.getElementById('feedbackBtn');
    if (feedbackBtn) {
        feedbackBtn.addEventListener('click', function() {
            const allFilled = Object.values(textareas).every(textarea => textarea.value.trim().length > 0);
            
            if (!allFilled) {
                if (window.vectorApp) {
                    window.vectorApp.showToast('Please fill all STAR sections to get feedback', 'error');
                }
                return;
            }
            
            // Calculate total length
            const totalLength = Object.values(textareas).reduce((sum, textarea) => {
                return sum + textarea.value.length;
            }, 0);
            
            let feedback = [];
            
            // Provide basic feedback
            if (totalLength < 500) {
                feedback.push('Your answer seems a bit short. Add more details and examples.');
            } else if (totalLength > 1200) {
                feedback.push('Your answer might be too long. Try to be more concise.');
            } else {
                feedback.push('Good length! Your answer is appropriately detailed.');
            }
            
            // Check Result section
            if (textareas.result.value.length < 150) {
                feedback.push('Spend more time on the Result section - lessons learned and growth are crucial.');
            }
            
            // Check for key words
            const actionText = textareas.action.value.toLowerCase();
            const resultText = textareas.result.value.toLowerCase();
            
            if (!resultText.includes('learn')) {
                feedback.push('Make sure to explicitly mention what you learned from this experience.');
            }
            
            if (feedback.length > 0) {
                if (window.vectorApp) {
                    window.vectorApp.showToast(feedback.join(' '), 'info');
                }
            } else {
                if (window.vectorApp) {
                    window.vectorApp.showToast('Great job! Your answer looks solid.', 'success');
                }
            }
        });
    }
    
    // Save Answer button
    const saveAnswerBtn = document.getElementById('saveAnswerBtn');
    if (saveAnswerBtn) {
        saveAnswerBtn.addEventListener('click', function() {
            // Content is auto-saved, just show confirmation
            if (window.vectorApp) {
                window.vectorApp.showToast('Answer saved successfully', 'success');
            }
        });
    }
    
    // Submit Answer button
    const submitAnswerBtn = document.getElementById('submitAnswerBtn');
    if (submitAnswerBtn) {
        submitAnswerBtn.addEventListener('click', function() {
            const allFilled = Object.values(textareas).every(textarea => textarea.value.trim().length > 0);
            
            if (!allFilled) {
                if (window.vectorApp) {
                    window.vectorApp.showToast('Please complete all STAR sections before submitting', 'error');
                }
                return;
            }
            
            // Check minimum lengths
            if (textareas.situation.value.length < 100) {
                if (window.vectorApp) {
                    window.vectorApp.showToast('Situation section needs more details (minimum 100 characters)', 'error');
                }
                return;
            }
            
            if (textareas.result.value.length < 150) {
                if (window.vectorApp) {
                    window.vectorApp.showToast('Result section needs more details (minimum 150 characters)', 'error');
                }
                return;
            }
            
            // In a real app, would submit the answer for review
            if (window.vectorApp) {
                window.vectorApp.showToast('Answer submitted successfully! Great work!', 'success');
            }
            
            // Clear localStorage
            Object.keys(textareas).forEach(key => {
                localStorage.removeItem(`behavioral_${key}`);
            });
            
            // Redirect after delay
            setTimeout(() => {
                window.location.href = 'questions.html';
            }, 2000);
        });
    }
    
    // Category card clicks
    const categoryCards = document.querySelectorAll('.category-card');
    categoryCards.forEach(card => {
        card.addEventListener('click', function() {
            const title = this.querySelector('.category-title').textContent;
            const example = this.querySelector('.category-example').textContent;
            
            if (window.vectorApp) {
                window.vectorApp.showToast(`Selected: ${title}`, 'info');
            }
            
            // Optionally pre-fill situation with this category
            if (textareas.situation.value.trim() === '') {
                textareas.situation.value = `I want to share an experience related to ${title.toLowerCase()}: ${example}.\n\n`;
                textareas.situation.dispatchEvent(new Event('input'));
            }
        });
    });
    
    // Bookmark functionality
    const bookmarkBtn = document.querySelector('.action-btn[title="Bookmark"]');
    if (bookmarkBtn) {
        bookmarkBtn.addEventListener('click', function() {
            const icon = this.querySelector('i');
            if (icon.classList.contains('far')) {
                icon.classList.remove('far');
                icon.classList.add('fas');
                if (window.vectorApp) {
                    window.vectorApp.showToast('Question bookmarked', 'success');
                }
            } else {
                icon.classList.remove('fas');
                icon.classList.add('far');
                if (window.vectorApp) {
                    window.vectorApp.showToast('Bookmark removed', 'info');
                }
            }
        });
    }
    
    // Share functionality
    const shareBtn = document.querySelector('.action-btn[title="Share"]');
    if (shareBtn) {
        shareBtn.addEventListener('click', function() {
            if (navigator.share) {
                navigator.share({
                    title: document.querySelector('.question-title-main').textContent,
                    text: 'Check out this behavioral interview question',
                    url: window.location.href
                }).catch(err => console.log('Error sharing:', err));
            } else {
                // Fallback: copy to clipboard
                navigator.clipboard.writeText(window.location.href).then(() => {
                    if (window.vectorApp) {
                        window.vectorApp.showToast('Link copied to clipboard', 'success');
                    }
                });
            }
        });
    }
});
