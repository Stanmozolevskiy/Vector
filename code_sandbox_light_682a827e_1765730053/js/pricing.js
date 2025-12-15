// Pricing Page JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Billing toggle (monthly/annual)
    const billingToggle = document.getElementById('billingToggle');
    const monthlyPrices = document.querySelectorAll('.monthly-price');
    const annualPrices = document.querySelectorAll('.annual-price');
    
    if (billingToggle) {
        billingToggle.addEventListener('change', function() {
            if (this.checked) {
                // Show annual prices
                monthlyPrices.forEach(price => price.style.display = 'none');
                annualPrices.forEach(price => price.style.display = 'inline');
            } else {
                // Show monthly prices
                monthlyPrices.forEach(price => price.style.display = 'inline');
                annualPrices.forEach(price => price.style.display = 'none');
            }
        });
    }
    
    // FAQ accordion
    const faqQuestions = document.querySelectorAll('.faq-question');
    faqQuestions.forEach(question => {
        question.addEventListener('click', function() {
            const faqItem = this.parentElement;
            const isActive = faqItem.classList.contains('active');
            
            // Close all FAQ items
            document.querySelectorAll('.faq-item').forEach(item => {
                item.classList.remove('active');
            });
            
            // Open clicked item if it wasn't active
            if (!isActive) {
                faqItem.classList.add('active');
            }
        });
    });
    
    // CTA buttons
    const ctaButtons = document.querySelectorAll('.pricing-card .btn-primary, .pricing-card .btn-outline');
    ctaButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            const planName = this.closest('.pricing-card').querySelector('.plan-header h3').textContent;
            
            if (this.textContent.includes('Contact')) {
                window.vectorApp.showToast('Enterprise sales team will contact you soon!', 'info');
                e.preventDefault();
            } else {
                window.vectorApp.showToast(`Starting ${planName} plan...`, 'success');
                // Would redirect to checkout or signup
            }
        });
    });
});