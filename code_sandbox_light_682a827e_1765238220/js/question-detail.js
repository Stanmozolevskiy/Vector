// Question Detail Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Solution tabs
    const solutionTabs = document.querySelectorAll('.solution-tab');
    solutionTabs.forEach(tab => {
        tab.addEventListener('click', function() {
            solutionTabs.forEach(t => t.classList.remove('active'));
            this.classList.add('active');
            
            const tabName = this.textContent;
            window.vectorApp.showToast(`Viewing: ${tabName}`, 'info');
        });
    });
    
    // Language selector
    const languageSelect = document.querySelector('.language-select');
    if (languageSelect) {
        languageSelect.addEventListener('change', function() {
            const language = this.value;
            window.vectorApp.showToast(`Switched to ${language}`, 'info');
            
            // In real app, would load code template for selected language
            const codeEditor = document.getElementById('codeEditor');
            const templates = {
                'JavaScript': 'function twoSum(nums, target) {\n    // Write your code here\n    \n}',
                'Python': 'def twoSum(nums, target):\n    # Write your code here\n    pass',
                'Java': 'public int[] twoSum(int[] nums, int target) {\n    // Write your code here\n    \n}',
                'C++': 'vector<int> twoSum(vector<int>& nums, int target) {\n    // Write your code here\n    \n}'
            };
            
            if (codeEditor && templates[language]) {
                codeEditor.value = templates[language];
            }
        });
    }
    
    // Reset button
    const resetBtn = document.querySelector('.editor-header .btn-outline');
    if (resetBtn) {
        resetBtn.addEventListener('click', function() {
            const codeEditor = document.getElementById('codeEditor');
            const language = document.querySelector('.language-select').value;
            
            if (confirm('Are you sure you want to reset your code?')) {
                // Trigger language change to reset to template
                languageSelect.dispatchEvent(new Event('change'));
                window.vectorApp.showToast('Code reset', 'info');
            }
        });
    }
    
    // Run code button
    const runBtn = document.querySelector('.editor-footer .btn-secondary');
    if (runBtn) {
        runBtn.addEventListener('click', function() {
            window.vectorApp.showToast('Running test cases...', 'info');
            
            // Simulate test execution
            setTimeout(() => {
                const testCases = document.querySelectorAll('.test-case');
                testCases.forEach(testCase => {
                    const status = testCase.querySelector('.test-status');
                    status.className = 'test-status passed';
                    status.innerHTML = '<i class="fas fa-check-circle"></i> Passed';
                });
                
                window.vectorApp.showToast('All test cases passed!', 'success');
            }, 2000);
        });
    }
    
    // Submit button
    const submitBtn = document.querySelector('.editor-footer .btn-primary');
    if (submitBtn) {
        submitBtn.addEventListener('click', function() {
            window.vectorApp.showToast('Submitting solution...', 'info');
            
            // Simulate submission
            setTimeout(() => {
                window.vectorApp.showToast('Solution accepted! ✓', 'success');
                
                // In real app, would save to backend and update user progress
                setTimeout(() => {
                    if (confirm('Solution accepted! Would you like to view the next problem?')) {
                        window.location.href = 'questions.html';
                    }
                }, 1500);
            }, 2000);
        });
    }
    
    // Code editor auto-expand
    const codeEditor = document.getElementById('codeEditor');
    if (codeEditor) {
        codeEditor.addEventListener('input', function() {
            this.style.height = 'auto';
            this.style.height = Math.max(300, this.scrollHeight) + 'px';
        });
    }
});