// Accessibility helper functions

window.focusElement = (selector) => {
    const element = document.querySelector(selector);
    if (element) {
        element.focus();
    }
};

window.focusElementById = (id) => {
    const element = document.getElementById(id);
    if (element) {
        element.focus();
    }
};

window.announceToScreenReader = (message) => {
    const announcement = document.createElement('div');
    announcement.setAttribute('aria-live', 'polite');
    announcement.setAttribute('aria-atomic', 'true');
    announcement.className = 'sr-only';
    announcement.textContent = message;
    
    document.body.appendChild(announcement);
    
    // Remove after announcement
    setTimeout(() => {
        document.body.removeChild(announcement);
    }, 1000);
};

// Skip link functionality
window.skipToContent = () => {
    const mainContent = document.querySelector('main');
    if (mainContent) {
        mainContent.focus();
        mainContent.scrollIntoView();
    }
};

// Keyboard navigation helpers
window.handleArrowNavigation = (currentElement, direction) => {
    const focusableElements = Array.from(document.querySelectorAll(
        'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
    ));
    
    const currentIndex = focusableElements.indexOf(currentElement);
    let nextIndex;
    
    switch (direction) {
        case 'up':
        case 'left':
            nextIndex = currentIndex > 0 ? currentIndex - 1 : focusableElements.length - 1;
            break;
        case 'down':
        case 'right':
            nextIndex = currentIndex < focusableElements.length - 1 ? currentIndex + 1 : 0;
            break;
        default:
            return;
    }
    
    focusableElements[nextIndex]?.focus();
};