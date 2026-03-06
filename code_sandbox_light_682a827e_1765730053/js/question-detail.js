// Question Detail Page JavaScript

document.addEventListener('DOMContentLoaded', () => {
    initializeResizer();
    initializeTabs();
    initializeButtons();
    initializeCodeEditor();
});

// Panel Resizer
function initializeResizer() {
    const resizer = document.getElementById('resizer');
    const descriptionPanel = document.querySelector('.description-panel');
    const editorPanel = document.querySelector('.editor-panel');
    
    if (!resizer) return;
    
    let isResizing = false;
    let startX = 0;
    let startWidth = 0;
    
    resizer.addEventListener('mousedown', (e) => {
        isResizing = true;
        startX = e.clientX;
        startWidth = descriptionPanel.offsetWidth;
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
    });
    
    document.addEventListener('mousemove', (e) => {
        if (!isResizing) return;
        
        const deltaX = e.clientX - startX;
        const newWidth = startWidth + deltaX;
        const containerWidth = document.querySelector('.question-container').offsetWidth;
        const minWidth = 300;
        const maxWidth = containerWidth - 300;
        
        if (newWidth >= minWidth && newWidth <= maxWidth) {
            const percentage = (newWidth / containerWidth) * 100;
            descriptionPanel.style.width = `${percentage}%`;
        }
    });
    
    document.addEventListener('mouseup', () => {
        if (isResizing) {
            isResizing = false;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
        }
    });
}

// Tab Switching
function initializeTabs() {
    // Panel tabs (Description, Editorial, Solutions)
    const panelTabs = document.querySelectorAll('.panel-tab');
    panelTabs.forEach(tab => {
        tab.addEventListener('click', () => {
            panelTabs.forEach(t => t.classList.remove('active'));
            tab.classList.add('active');
            
            const tabType = tab.getAttribute('data-tab');
            // In a real app, would load different content
            console.log('Switched to tab:', tabType);
        });
    });
    
    // Testcase tabs
    const testcaseTabs = document.querySelectorAll('.testcase-tab');
    testcaseTabs.forEach(tab => {
        tab.addEventListener('click', () => {
            testcaseTabs.forEach(t => t.classList.remove('active'));
            tab.classList.add('active');
        });
    });
}

// Button Actions
function initializeButtons() {
    // Run Code
    const runBtns = document.querySelectorAll('.nav-btn:not(.nav-btn-primary)');
    runBtns.forEach(btn => {
        if (btn.textContent.includes('Run')) {
            btn.addEventListener('click', runCode);
        }
    });
    
    // Submit Code
    const submitBtns = document.querySelectorAll('.nav-btn-primary');
    submitBtns.forEach(btn => {
        if (btn.textContent.includes('Submit')) {
            btn.addEventListener('click', submitCode);
        }
    });
    
    // Back Button
    const backBtn = document.querySelector('.back-btn');
    if (backBtn) {
        backBtn.addEventListener('click', (e) => {
            e.preventDefault();
            window.location.href = 'questions.html';
        });
    }
}

// Run Code Function
function runCode() {
    console.log('Running code...');
    
    // Show testcase result tab
    const testcaseTabs = document.querySelectorAll('.testcase-tab');
    testcaseTabs.forEach(t => t.classList.remove('active'));
    if (testcaseTabs[1]) {
        testcaseTabs[1].classList.add('active');
    }
    
    // Simulate code execution
    setTimeout(() => {
        const consoleOutput = document.getElementById('consoleOutput');
        if (consoleOutput) {
            consoleOutput.style.display = 'flex';
        }
        
        if (window.vectorApp) {
            window.vectorApp.showToast('Code executed successfully!', 'success');
        }
    }, 500);
}

// Submit Code Function
function submitCode() {
    console.log('Submitting code...');
    
    // Simulate submission
    setTimeout(() => {
        const consoleOutput = document.getElementById('consoleOutput');
        if (consoleOutput) {
            consoleOutput.style.display = 'flex';
        }
        
        if (window.vectorApp) {
            window.vectorApp.showToast('Code submitted! Result: Accepted', 'success');
        }
    }, 1000);
}

// Code Editor
function initializeCodeEditor() {
    const codeEditor = document.getElementById('codeEditor');
    
    if (!codeEditor) return;
    
    // Handle tab key
    codeEditor.addEventListener('keydown', (e) => {
        if (e.key === 'Tab') {
            e.preventDefault();
            document.execCommand('insertHTML', false, '    ');
        }
    });
    
    // Language selector
    const languageSelect = document.querySelector('.language-select');
    if (languageSelect) {
        languageSelect.addEventListener('change', (e) => {
            const language = e.target.value;
            updateCodeTemplate(language);
        });
    }
}

// Update code template based on language
function updateCodeTemplate(language) {
    const codeEditor = document.getElementById('codeEditor');
    if (!codeEditor) return;
    
    const templates = {
        javascript: `<span class="code-comment">/**
 * @param {number[]} nums
 * @param {number} target
 * @return {number[]}
 */</span>
<span class="code-keyword">var</span> <span class="code-function">twoSum</span> = <span class="code-keyword">function</span>(<span class="code-param">nums</span>, <span class="code-param">target</span>) {
    
};`,
        python: `<span class="code-keyword">class</span> <span class="code-function">Solution</span>:
    <span class="code-keyword">def</span> <span class="code-function">twoSum</span>(<span class="code-param">self</span>, <span class="code-param">nums</span>: List[int], <span class="code-param">target</span>: int) -> List[int]:
        `,
        java: `<span class="code-keyword">class</span> <span class="code-function">Solution</span> {
    <span class="code-keyword">public</span> int[] <span class="code-function">twoSum</span>(int[] <span class="code-param">nums</span>, int <span class="code-param">target</span>) {
        
    }
}`,
        cpp: `<span class="code-keyword">class</span> <span class="code-function">Solution</span> {
<span class="code-keyword">public</span>:
    vector&lt;int&gt; <span class="code-function">twoSum</span>(vector&lt;int&gt;& <span class="code-param">nums</span>, int <span class="code-param">target</span>) {
        
    }
};`
    };
    
    codeEditor.innerHTML = templates[language] || templates.javascript;
}

// Add testcase functionality
const addTestcaseBtn = document.querySelector('.testcase-btn');
if (addTestcaseBtn) {
    addTestcaseBtn.addEventListener('click', () => {
        if (window.vectorApp) {
            window.vectorApp.showToast('Testcase added', 'info');
        }
    });
}

// Meta button interactions
document.querySelectorAll('.meta-btn').forEach(btn => {
    btn.addEventListener('click', () => {
        const btnText = btn.textContent.trim();
        console.log('Meta button clicked:', btnText);
        
        if (window.vectorApp) {
            if (btnText.includes('Topics')) {
                window.vectorApp.showToast('Topics: Array, Hash Table', 'info');
            } else if (btnText.includes('Companies')) {
                window.vectorApp.showToast('Companies: Google, Amazon, Microsoft', 'info');
            } else if (btnText.includes('Hint')) {
                window.vectorApp.showToast('Hint: Use a hash map to store values you\'ve seen', 'info');
            }
        }
    });
});

// Similar questions navigation
document.querySelectorAll('.similar-q').forEach(link => {
    link.addEventListener('click', (e) => {
        e.preventDefault();
        const questionTitle = link.querySelector('.similar-q-title').textContent;
        if (window.vectorApp) {
            window.vectorApp.showToast(`Loading: ${questionTitle}`, 'info');
        }
    });
});
