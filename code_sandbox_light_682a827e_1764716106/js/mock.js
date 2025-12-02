// Mock Interviews Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Interview type selection
    const typeCards = document.querySelectorAll('.type-card');
    typeCards.forEach(card => {
        card.addEventListener('click', function() {
            typeCards.forEach(c => c.classList.remove('active'));
            this.classList.add('active');
            
            const interviewType = this.querySelector('h3').textContent;
            window.vectorApp.showToast(`Selected: ${interviewType}`, 'success');
            
            // Update booking summary
            const summaryType = document.querySelector('.summary-item strong');
            if (summaryType) {
                summaryType.textContent = interviewType;
            }
        });
    });
    
    // Interviewer selection
    const interviewerCards = document.querySelectorAll('.interviewer-card .btn-primary');
    interviewerCards.forEach(button => {
        button.addEventListener('click', function() {
            const card = this.closest('.interviewer-card');
            const interviewerName = card.querySelector('h3').textContent;
            
            // Remove selection from all cards
            document.querySelectorAll('.interviewer-card').forEach(c => {
                c.style.border = '1px solid var(--border-color)';
            });
            
            // Highlight selected card
            card.style.border = '2px solid var(--primary-color)';
            
            window.vectorApp.showToast(`Selected: ${interviewerName}`, 'success');
            
            // Update booking summary
            const summaryInterviewer = document.querySelectorAll('.summary-item strong')[1];
            if (summaryInterviewer) {
                summaryInterviewer.textContent = interviewerName;
            }
        });
    });
    
    // Calendar navigation
    const calendarNavButtons = document.querySelectorAll('.calendar-nav');
    calendarNavButtons.forEach(button => {
        button.addEventListener('click', function() {
            // In a real app, would navigate months
            window.vectorApp.showToast('Calendar navigation', 'info');
        });
    });
    
    // Calendar day selection
    const calendarDays = document.querySelectorAll('.calendar-day:not(.disabled)');
    calendarDays.forEach(day => {
        day.addEventListener('click', function() {
            calendarDays.forEach(d => d.classList.remove('active'));
            this.classList.add('active');
            
            const dayNumber = this.textContent;
            window.vectorApp.showToast(`Selected: Jan ${dayNumber}, 2025`, 'success');
            
            // Update time slots header
            const timeSlotsHeader = document.querySelector('.time-slots h3');
            if (timeSlotsHeader) {
                timeSlotsHeader.textContent = `Available Time Slots (Jan ${dayNumber})`;
            }
        });
    });
    
    // Time slot selection
    const timeSlots = document.querySelectorAll('.time-slot:not(.disabled)');
    timeSlots.forEach(slot => {
        slot.addEventListener('click', function() {
            timeSlots.forEach(s => s.classList.remove('active'));
            this.classList.add('active');
            
            const timeSlot = this.textContent;
            window.vectorApp.showToast(`Selected time: ${timeSlot}`, 'success');
            
            // Update booking summary
            const summaryDateTime = document.querySelectorAll('.summary-item strong')[2];
            if (summaryDateTime) {
                const selectedDay = document.querySelector('.calendar-day.active');
                if (selectedDay) {
                    summaryDateTime.textContent = `Jan ${selectedDay.textContent}, 2025 at ${timeSlot}`;
                }
            }
        });
    });
    
    // Confirm booking button
    const confirmBtn = document.querySelector('.booking-summary .btn-primary');
    if (confirmBtn) {
        confirmBtn.addEventListener('click', function() {
            // Validate that all selections are made
            const hasType = document.querySelector('.type-card.active');
            const hasInterviewer = document.querySelector('.interviewer-card[style*="border: 2px"]');
            const hasDate = document.querySelector('.calendar-day.active');
            const hasTime = document.querySelector('.time-slot.active');
            
            if (!hasType || !hasInterviewer || !hasDate || !hasTime) {
                window.vectorApp.showToast('Please complete all selections', 'error');
                return;
            }
            
            window.vectorApp.showToast('Booking confirmed! Check your email for details.', 'success');
            
            setTimeout(() => {
                window.location.href = 'dashboard.html';
            }, 2000);
        });
    }
});