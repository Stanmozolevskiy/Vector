// Question Design Page JavaScript

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
    
    // Whiteboard functionality
    const canvas = document.getElementById('whiteboard');
    if (canvas) {
        const ctx = canvas.getContext('2d');
        let isDrawing = false;
        let lastX = 0;
        let lastY = 0;
        let currentTool = 'pen';
        
        // Set canvas size
        function resizeCanvas() {
            const wrapper = canvas.parentElement;
            canvas.width = wrapper.offsetWidth;
            canvas.height = wrapper.offsetHeight;
        }
        resizeCanvas();
        window.addEventListener('resize', resizeCanvas);
        
        // Drawing settings
        ctx.strokeStyle = '#111827';
        ctx.lineWidth = 2;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
        
        // Tool buttons
        const toolButtons = document.querySelectorAll('.tool-btn');
        toolButtons.forEach(btn => {
            btn.addEventListener('click', function() {
                toolButtons.forEach(b => b.classList.remove('active'));
                this.classList.add('active');
                
                const icon = this.querySelector('i');
                if (icon.classList.contains('fa-pen')) {
                    currentTool = 'pen';
                    ctx.globalCompositeOperation = 'source-over';
                    ctx.strokeStyle = '#111827';
                } else if (icon.classList.contains('fa-eraser')) {
                    currentTool = 'eraser';
                    ctx.globalCompositeOperation = 'destination-out';
                    ctx.lineWidth = 20;
                } else if (icon.classList.contains('fa-trash')) {
                    ctx.clearRect(0, 0, canvas.width, canvas.height);
                    if (window.vectorApp) {
                        window.vectorApp.showToast('Whiteboard cleared', 'info');
                    }
                }
            });
        });
        
        // Drawing functions
        function startDrawing(e) {
            isDrawing = true;
            [lastX, lastY] = [e.offsetX, e.offsetY];
        }
        
        function draw(e) {
            if (!isDrawing) return;
            
            ctx.beginPath();
            ctx.moveTo(lastX, lastY);
            ctx.lineTo(e.offsetX, e.offsetY);
            ctx.stroke();
            
            [lastX, lastY] = [e.offsetX, e.offsetY];
        }
        
        function stopDrawing() {
            isDrawing = false;
        }
        
        // Mouse events
        canvas.addEventListener('mousedown', startDrawing);
        canvas.addEventListener('mousemove', draw);
        canvas.addEventListener('mouseup', stopDrawing);
        canvas.addEventListener('mouseout', stopDrawing);
        
        // Touch events for mobile
        canvas.addEventListener('touchstart', (e) => {
            e.preventDefault();
            const touch = e.touches[0];
            const rect = canvas.getBoundingClientRect();
            const offsetX = touch.clientX - rect.left;
            const offsetY = touch.clientY - rect.top;
            isDrawing = true;
            [lastX, lastY] = [offsetX, offsetY];
        });
        
        canvas.addEventListener('touchmove', (e) => {
            e.preventDefault();
            if (!isDrawing) return;
            
            const touch = e.touches[0];
            const rect = canvas.getBoundingClientRect();
            const offsetX = touch.clientX - rect.left;
            const offsetY = touch.clientY - rect.top;
            
            ctx.beginPath();
            ctx.moveTo(lastX, lastY);
            ctx.lineTo(offsetX, offsetY);
            ctx.stroke();
            
            [lastX, lastY] = [offsetX, offsetY];
        });
        
        canvas.addEventListener('touchend', stopDrawing);
    }
    
    // Notes textarea functionality
    const notesTextarea = document.getElementById('designNotes');
    if (notesTextarea) {
        // Auto-save notes to localStorage
        notesTextarea.addEventListener('input', function() {
            localStorage.setItem('designNotes', this.value);
        });
        
        // Load saved notes
        const savedNotes = localStorage.getItem('designNotes');
        if (savedNotes) {
            notesTextarea.value = savedNotes;
        }
    }
    
    // Checklist functionality
    const checklistItems = document.querySelectorAll('.checklist i');
    checklistItems.forEach(icon => {
        icon.addEventListener('click', function() {
            if (this.classList.contains('fa-square')) {
                this.classList.remove('fa-square');
                this.classList.add('fa-check-square');
                this.style.color = 'var(--success-color)';
            } else {
                this.classList.remove('fa-check-square');
                this.classList.add('fa-square');
                this.style.color = '#d1d5db';
            }
        });
    });
    
    // Action buttons
    const saveBtn = document.querySelector('.btn-outline');
    const submitBtn = document.querySelector('.btn-primary');
    
    if (saveBtn) {
        saveBtn.addEventListener('click', function() {
            // Save whiteboard and notes
            const canvas = document.getElementById('whiteboard');
            if (canvas) {
                const imageData = canvas.toDataURL();
                localStorage.setItem('whiteboardData', imageData);
            }
            
            if (window.vectorApp) {
                window.vectorApp.showToast('Progress saved successfully', 'success');
            }
        });
    }
    
    if (submitBtn) {
        submitBtn.addEventListener('click', function() {
            const notes = document.getElementById('designNotes').value;
            const checkedItems = document.querySelectorAll('.checklist .fa-check-square').length;
            
            if (notes.length < 50) {
                if (window.vectorApp) {
                    window.vectorApp.showToast('Please add more notes before submitting', 'error');
                }
                return;
            }
            
            if (checkedItems < 3) {
                if (window.vectorApp) {
                    window.vectorApp.showToast('Please complete at least 3 checklist items', 'error');
                }
                return;
            }
            
            // In a real app, would submit the solution
            if (window.vectorApp) {
                window.vectorApp.showToast('Solution submitted successfully!', 'success');
            }
            
            // Redirect to questions page after a delay
            setTimeout(() => {
                window.location.href = 'questions.html';
            }, 2000);
        });
    }
    
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
});
