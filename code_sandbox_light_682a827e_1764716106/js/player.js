// Video Player JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Tab switching
    const tabButtons = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');
    
    tabButtons.forEach(button => {
        button.addEventListener('click', function() {
            const tabName = this.dataset.tab;
            
            // Remove active class from all tabs and contents
            tabButtons.forEach(btn => btn.classList.remove('active'));
            tabContents.forEach(content => content.classList.remove('active'));
            
            // Add active class to clicked tab and corresponding content
            this.classList.add('active');
            document.getElementById(tabName).classList.add('active');
        });
    });
    
    // Curriculum section toggle
    const sectionTitles = document.querySelectorAll('.section-title');
    sectionTitles.forEach(title => {
        title.addEventListener('click', function() {
            const section = this.parentElement;
            const lessonslist = section.querySelector('.lessons-list');
            const icon = this.querySelector('i');
            
            if (lessonslist) {
                section.classList.toggle('collapsed');
                if (section.classList.contains('collapsed')) {
                    icon.className = 'fas fa-chevron-right';
                } else {
                    icon.className = 'fas fa-chevron-down';
                }
            }
        });
    });
    
    // Video control buttons
    const playBtn = document.querySelector('.control-btn .fa-play');
    if (playBtn) {
        playBtn.parentElement.addEventListener('click', function() {
            if (playBtn.classList.contains('fa-play')) {
                playBtn.classList.remove('fa-play');
                playBtn.classList.add('fa-pause');
                window.vectorApp.showToast('Video playing', 'info');
            } else {
                playBtn.classList.remove('fa-pause');
                playBtn.classList.add('fa-play');
                window.vectorApp.showToast('Video paused', 'info');
            }
        });
    }
    
    // Progress bar interaction
    const progressBar = document.querySelector('.progress-bar');
    if (progressBar) {
        progressBar.addEventListener('click', function(e) {
            const rect = this.getBoundingClientRect();
            const percent = ((e.clientX - rect.left) / rect.width) * 100;
            const progressFilled = this.querySelector('.progress-filled');
            progressFilled.style.width = percent + '%';
            window.vectorApp.showToast(`Seeked to ${Math.round(percent)}%`, 'info');
        });
    }
    
    // Save note functionality
    const saveNoteBtn = document.querySelector('.note-editor .btn-primary');
    if (saveNoteBtn) {
        saveNoteBtn.addEventListener('click', function() {
            const textarea = document.querySelector('.note-editor textarea');
            const noteText = textarea.value.trim();
            
            if (noteText) {
                window.vectorApp.showToast('Note saved successfully!', 'success');
                textarea.value = '';
                
                // In a real app, would save to backend
            } else {
                window.vectorApp.showToast('Please enter a note', 'error');
            }
        });
    }
    
    // Bookmark button
    const bookmarkBtn = document.querySelector('.action-btn .fa-bookmark');
    if (bookmarkBtn) {
        bookmarkBtn.parentElement.addEventListener('click', function() {
            if (bookmarkBtn.classList.contains('far')) {
                bookmarkBtn.classList.remove('far');
                bookmarkBtn.classList.add('fas');
                window.vectorApp.showToast('Lesson bookmarked', 'success');
            } else {
                bookmarkBtn.classList.remove('fas');
                bookmarkBtn.classList.add('far');
                window.vectorApp.showToast('Bookmark removed', 'info');
            }
        });
    }
});