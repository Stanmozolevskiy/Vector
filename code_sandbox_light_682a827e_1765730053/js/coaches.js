// Coaches Page JavaScript

// Sample coaches data
const coachesData = [
    {
        id: 1,
        name: "Sarah Chen",
        title: "Senior Software Engineer",
        company: "Google",
        avatar: "https://ui-avatars.com/api/?name=Sarah+Chen&background=667eea&color=fff&size=128",
        rating: 4.9,
        reviewCount: 127,
        specializations: ["Algorithms", "System Design", "Frontend"],
        hourlyRate: 150,
        sessionsCompleted: 250,
        yearsExperience: 8,
        verified: true
    },
    {
        id: 2,
        name: "Michael Rodriguez",
        title: "Tech Lead",
        company: "Meta",
        avatar: "https://ui-avatars.com/api/?name=Michael+Rodriguez&background=764ba2&color=fff&size=128",
        rating: 4.8,
        reviewCount: 94,
        specializations: ["System Design", "Backend", "Behavioral"],
        hourlyRate: 180,
        sessionsCompleted: 180,
        yearsExperience: 10,
        verified: true
    },
    {
        id: 3,
        name: "Emily Johnson",
        title: "Principal Engineer",
        company: "Amazon",
        avatar: "https://ui-avatars.com/api/?name=Emily+Johnson&background=ec4899&color=fff&size=128",
        rating: 5.0,
        reviewCount: 156,
        specializations: ["Algorithms", "Backend", "System Design"],
        hourlyRate: 200,
        sessionsCompleted: 320,
        yearsExperience: 12,
        verified: true
    },
    {
        id: 4,
        name: "David Kim",
        title: "Staff Software Engineer",
        company: "Microsoft",
        avatar: "https://ui-avatars.com/api/?name=David+Kim&background=10b981&color=fff&size=128",
        rating: 4.7,
        reviewCount: 78,
        specializations: ["Frontend", "Mobile", "Behavioral"],
        hourlyRate: 140,
        sessionsCompleted: 145,
        yearsExperience: 7,
        verified: true
    },
    {
        id: 5,
        name: "Priya Sharma",
        title: "Engineering Manager",
        company: "Apple",
        avatar: "https://ui-avatars.com/api/?name=Priya+Sharma&background=f59e0b&color=fff&size=128",
        rating: 4.9,
        reviewCount: 112,
        specializations: ["System Design", "Behavioral", "Leadership"],
        hourlyRate: 175,
        sessionsCompleted: 200,
        yearsExperience: 11,
        verified: true
    },
    {
        id: 6,
        name: "Alex Thompson",
        title: "Senior SDE",
        company: "Amazon",
        avatar: "https://ui-avatars.com/api/?name=Alex+Thompson&background=6366f1&color=fff&size=128",
        rating: 4.6,
        reviewCount: 65,
        specializations: ["Algorithms", "Backend"],
        hourlyRate: 130,
        sessionsCompleted: 120,
        yearsExperience: 6,
        verified: true
    },
    {
        id: 7,
        name: "Lisa Wang",
        title: "Senior Engineer",
        company: "Google",
        avatar: "https://ui-avatars.com/api/?name=Lisa+Wang&background=8b5cf6&color=fff&size=128",
        rating: 4.8,
        reviewCount: 89,
        specializations: ["Frontend", "Mobile", "System Design"],
        hourlyRate: 160,
        sessionsCompleted: 175,
        yearsExperience: 9,
        verified: true
    },
    {
        id: 8,
        name: "James Wilson",
        title: "Tech Lead",
        company: "Meta",
        avatar: "https://ui-avatars.com/api/?name=James+Wilson&background=ef4444&color=fff&size=128",
        rating: 4.9,
        reviewCount: 103,
        specializations: ["System Design", "Backend", "Algorithms"],
        hourlyRate: 190,
        sessionsCompleted: 220,
        yearsExperience: 10,
        verified: true
    }
];

// Generate more sample coaches
for (let i = 9; i <= 50; i++) {
    const companies = ["Google", "Meta", "Amazon", "Microsoft", "Apple"];
    const titles = ["Senior Software Engineer", "Tech Lead", "Staff Engineer", "Principal Engineer"];
    const specs = [
        ["Algorithms", "System Design"],
        ["Frontend", "Mobile"],
        ["Backend", "System Design"],
        ["Algorithms", "Behavioral"],
        ["System Design", "Frontend", "Backend"]
    ];
    
    coachesData.push({
        id: i,
        name: `Coach ${i}`,
        title: titles[Math.floor(Math.random() * titles.length)],
        company: companies[Math.floor(Math.random() * companies.length)],
        avatar: `https://ui-avatars.com/api/?name=Coach+${i}&background=random&size=128`,
        rating: (4 + Math.random()).toFixed(1),
        reviewCount: Math.floor(Math.random() * 150) + 30,
        specializations: specs[Math.floor(Math.random() * specs.length)],
        hourlyRate: Math.floor(Math.random() * 150) + 100,
        sessionsCompleted: Math.floor(Math.random() * 200) + 50,
        yearsExperience: Math.floor(Math.random() * 10) + 5,
        verified: Math.random() > 0.2
    });
}

// State
let currentPage = 1;
const itemsPerPage = 12;
let filteredCoaches = [...coachesData];
let currentView = 'grid';

// DOM Elements
const coachesGrid = document.getElementById('coachesGrid');
const searchInput = document.getElementById('searchInput');
const sortBy = document.getElementById('sortBy');
const viewButtons = document.querySelectorAll('.view-btn');
const priceRange = document.getElementById('priceRange');
const maxPriceDisplay = document.getElementById('maxPrice');
const resultsCount = document.getElementById('resultsCount');
const resetFiltersBtn = document.getElementById('resetFiltersBtn');

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    renderCoaches();
    attachEventListeners();
});

// Render coaches
function renderCoaches() {
    const start = (currentPage - 1) * itemsPerPage;
    const end = start + itemsPerPage;
    const paginatedCoaches = filteredCoaches.slice(start, end);
    
    coachesGrid.innerHTML = '';
    
    if (paginatedCoaches.length === 0) {
        coachesGrid.innerHTML = `
            <div style="grid-column: 1/-1; text-align: center; padding: 4rem 2rem; color: var(--text-light);">
                <i class="fas fa-search" style="font-size: 3rem; margin-bottom: 1rem; display: block; opacity: 0.5;"></i>
                <h3 style="margin-bottom: 0.5rem; color: var(--text-dark);">No coaches found</h3>
                <p>Try adjusting your filters to see more results.</p>
            </div>
        `;
        return;
    }
    
    paginatedCoaches.forEach(coach => {
        const card = createCoachCard(coach);
        coachesGrid.appendChild(card);
    });
    
    resultsCount.textContent = filteredCoaches.length;
}

// Create coach card
function createCoachCard(coach) {
    const card = document.createElement('div');
    card.className = 'coach-card';
    card.onclick = () => window.location.href = `coach-detail.html?id=${coach.id}`;
    
    const stars = generateStars(coach.rating);
    const specs = coach.specializations.map(spec => 
        `<span class="specialization-tag">${spec}</span>`
    ).join('');
    
    card.innerHTML = `
        <div class="coach-card-header">
            <img src="${coach.avatar}" alt="${coach.name}" class="coach-avatar">
            ${coach.verified ? '<div class="coach-badge"><i class="fas fa-check-circle"></i> Verified</div>' : ''}
        </div>
        <div class="coach-card-body">
            <h3 class="coach-name">${coach.name}</h3>
            <p class="coach-title">${coach.title}</p>
            <div class="coach-company">
                <i class="fas fa-building"></i>
                ${coach.company}
            </div>
            <div class="coach-rating">
                <span class="rating-stars">${stars}</span>
                <span class="rating-value">${coach.rating}</span>
                <span class="rating-count">(${coach.reviewCount})</span>
            </div>
            <div class="coach-specializations">
                ${specs}
            </div>
            <div class="coach-stats">
                <div class="stat-item">
                    <span class="stat-value">${coach.sessionsCompleted}</span>
                    <span class="stat-label">Sessions</span>
                </div>
                <div class="stat-item">
                    <span class="stat-value">${coach.yearsExperience}</span>
                    <span class="stat-label">Years Exp</span>
                </div>
            </div>
            <div class="coach-card-footer">
                <div class="coach-rate">
                    <span class="rate-amount">$${coach.hourlyRate}</span>
                    <span class="rate-period">per hour</span>
                </div>
                <button class="btn-book" onclick="event.stopPropagation(); bookCoach(${coach.id})">
                    Book Session
                </button>
            </div>
        </div>
    `;
    
    return card;
}

// Generate star rating HTML
function generateStars(rating) {
    const fullStars = Math.floor(rating);
    const hasHalfStar = rating % 1 >= 0.5;
    let stars = '';
    
    for (let i = 0; i < fullStars; i++) {
        stars += '<i class="fas fa-star"></i>';
    }
    
    if (hasHalfStar) {
        stars += '<i class="fas fa-star-half-alt"></i>';
    }
    
    const emptyStars = 5 - Math.ceil(rating);
    for (let i = 0; i < emptyStars; i++) {
        stars += '<i class="far fa-star"></i>';
    }
    
    return stars;
}

// Book coach
function bookCoach(coachId) {
    const coach = coachesData.find(c => c.id === coachId);
    window.vectorApp.showToast(`Booking session with ${coach.name}...`, 'info');
    setTimeout(() => {
        window.location.href = `coach-detail.html?id=${coachId}#book`;
    }, 500);
}

// Attach event listeners
function attachEventListeners() {
    // Search
    searchInput.addEventListener('input', applyFilters);
    
    // Sort
    sortBy.addEventListener('change', applySorting);
    
    // View toggle
    viewButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            viewButtons.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            currentView = btn.dataset.view;
            coachesGrid.className = currentView === 'list' ? 'coaches-grid list-view' : 'coaches-grid';
        });
    });
    
    // Price range
    priceRange.addEventListener('input', (e) => {
        const value = e.target.value;
        maxPriceDisplay.textContent = value >= 500 ? '$500+' : `$${value}`;
        applyFilters();
    });
    
    // Filter checkboxes and radios
    document.querySelectorAll('.filters-sidebar input[type="checkbox"], .filters-sidebar input[type="radio"]').forEach(input => {
        input.addEventListener('change', applyFilters);
    });
    
    // Reset filters
    resetFiltersBtn.addEventListener('click', resetFilters);
}

// Apply filters
function applyFilters() {
    const searchTerm = searchInput.value.toLowerCase();
    const maxPrice = parseInt(priceRange.value);
    const selectedSpecs = Array.from(document.querySelectorAll('input[name="specialization"]:checked')).map(cb => cb.value);
    const minRating = document.querySelector('input[name="rating"]:checked')?.value;
    const selectedCompanies = Array.from(document.querySelectorAll('input[name="company"]:checked')).map(cb => cb.value);
    
    filteredCoaches = coachesData.filter(coach => {
        // Search filter
        const matchesSearch = !searchTerm || 
            coach.name.toLowerCase().includes(searchTerm) ||
            coach.company.toLowerCase().includes(searchTerm) ||
            coach.specializations.some(spec => spec.toLowerCase().includes(searchTerm));
        
        // Price filter
        const matchesPrice = maxPrice >= 500 || coach.hourlyRate <= maxPrice;
        
        // Specialization filter
        const matchesSpec = selectedSpecs.length === 0 || 
            coach.specializations.some(spec => selectedSpecs.includes(spec.toLowerCase().replace(/ /g, '-').replace(/&/g, '').trim()));
        
        // Rating filter
        const matchesRating = !minRating || minRating === 'all' || coach.rating >= parseFloat(minRating);
        
        // Company filter
        const matchesCompany = selectedCompanies.length === 0 || 
            selectedCompanies.includes(coach.company.toLowerCase());
        
        return matchesSearch && matchesPrice && matchesSpec && matchesRating && matchesCompany;
    });
    
    applySorting();
    currentPage = 1;
    renderCoaches();
}

// Apply sorting
function applySorting() {
    const sortValue = sortBy.value;
    
    switch(sortValue) {
        case 'rating':
            filteredCoaches.sort((a, b) => b.rating - a.rating);
            break;
        case 'price-low':
            filteredCoaches.sort((a, b) => a.hourlyRate - b.hourlyRate);
            break;
        case 'price-high':
            filteredCoaches.sort((a, b) => b.hourlyRate - a.hourlyRate);
            break;
        case 'experience':
            filteredCoaches.sort((a, b) => b.yearsExperience - a.yearsExperience);
            break;
        default: // recommended
            filteredCoaches.sort((a, b) => (b.rating * b.reviewCount) - (a.rating * a.reviewCount));
    }
    
    renderCoaches();
}

// Reset filters
function resetFilters() {
    // Reset search
    searchInput.value = '';
    
    // Reset price
    priceRange.value = 500;
    maxPriceDisplay.textContent = '$500+';
    
    // Check all specializations
    document.querySelectorAll('input[name="specialization"]').forEach(cb => cb.checked = true);
    
    // Reset rating to all
    document.querySelector('input[name="rating"][value="all"]').checked = true;
    
    // Uncheck all companies
    document.querySelectorAll('input[name="company"]').forEach(cb => cb.checked = false);
    
    // Reset sort
    sortBy.value = 'recommended';
    
    window.vectorApp.showToast('Filters reset', 'info');
    applyFilters();
}
