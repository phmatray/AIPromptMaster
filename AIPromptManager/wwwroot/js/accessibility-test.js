// Accessibility testing utilities

window.accessibilityTest = {
    // Test color contrast ratios
    testColorContrast: function() {
        const results = [];
        
        // Get all text elements
        const textElements = document.querySelectorAll('p, span, div, h1, h2, h3, h4, h5, h6, a, button, label');
        
        textElements.forEach(element => {
            const styles = window.getComputedStyle(element);
            const color = styles.color;
            const backgroundColor = styles.backgroundColor;
            
            // Skip if no background color or transparent
            if (backgroundColor === 'rgba(0, 0, 0, 0)' || backgroundColor === 'transparent') {
                return;
            }
            
            const contrast = this.calculateContrast(color, backgroundColor);
            const fontSize = parseFloat(styles.fontSize);
            const fontWeight = styles.fontWeight;
            
            // WCAG AA standards
            const isLargeText = fontSize >= 18 || (fontSize >= 14 && (fontWeight === 'bold' || fontWeight >= 700));
            const minContrast = isLargeText ? 3 : 4.5;
            
            if (contrast < minContrast) {
                results.push({
                    element: element,
                    contrast: contrast.toFixed(2),
                    required: minContrast,
                    text: element.textContent.substring(0, 50),
                    color: color,
                    backgroundColor: backgroundColor
                });
            }
        });
        
        return results;
    },
    
    // Calculate contrast ratio between two colors
    calculateContrast: function(color1, color2) {
        const rgb1 = this.parseColor(color1);
        const rgb2 = this.parseColor(color2);
        
        const l1 = this.getLuminance(rgb1);
        const l2 = this.getLuminance(rgb2);
        
        const lighter = Math.max(l1, l2);
        const darker = Math.min(l1, l2);
        
        return (lighter + 0.05) / (darker + 0.05);
    },
    
    // Parse color string to RGB values
    parseColor: function(color) {
        const div = document.createElement('div');
        div.style.color = color;
        document.body.appendChild(div);
        const computedColor = window.getComputedStyle(div).color;
        document.body.removeChild(div);
        
        const match = computedColor.match(/rgb\((\d+),\s*(\d+),\s*(\d+)\)/);
        if (match) {
            return {
                r: parseInt(match[1]),
                g: parseInt(match[2]),
                b: parseInt(match[3])
            };
        }
        return { r: 0, g: 0, b: 0 };
    },
    
    // Calculate relative luminance
    getLuminance: function(rgb) {
        const { r, g, b } = rgb;
        
        const [rs, gs, bs] = [r, g, b].map(c => {
            c = c / 255;
            return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
        });
        
        return 0.2126 * rs + 0.7152 * gs + 0.0722 * bs;
    },
    
    // Test keyboard navigation
    testKeyboardNavigation: function() {
        const focusableElements = document.querySelectorAll(
            'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
        );
        
        const results = {
            totalFocusable: focusableElements.length,
            elementsWithoutVisibleFocus: [],
            elementsWithoutAriaLabels: []
        };
        
        focusableElements.forEach(element => {
            // Test focus visibility
            element.focus();
            const styles = window.getComputedStyle(element);
            const hasVisibleFocus = styles.outline !== 'none' && styles.outline !== '0px' && styles.outline !== '';
            
            if (!hasVisibleFocus) {
                results.elementsWithoutVisibleFocus.push({
                    element: element,
                    tagName: element.tagName,
                    text: element.textContent?.substring(0, 30) || element.getAttribute('aria-label') || 'No text'
                });
            }
            
            // Test ARIA labels for buttons without text
            if (element.tagName === 'BUTTON' && !element.textContent.trim() && !element.getAttribute('aria-label')) {
                results.elementsWithoutAriaLabels.push({
                    element: element,
                    tagName: element.tagName
                });
            }
        });
        
        return results;
    },
    
    // Test screen reader compatibility
    testScreenReaderCompatibility: function() {
        const results = {
            imagesWithoutAlt: [],
            formsWithoutLabels: [],
            headingStructure: [],
            landmarkElements: []
        };
        
        // Test images without alt text
        const images = document.querySelectorAll('img');
        images.forEach(img => {
            if (!img.getAttribute('alt') && !img.getAttribute('aria-label')) {
                results.imagesWithoutAlt.push({
                    element: img,
                    src: img.src
                });
            }
        });
        
        // Test form inputs without labels
        const inputs = document.querySelectorAll('input, textarea, select');
        inputs.forEach(input => {
            const hasLabel = document.querySelector(`label[for="${input.id}"]`) || 
                           input.getAttribute('aria-label') || 
                           input.getAttribute('aria-labelledby');
            
            if (!hasLabel) {
                results.formsWithoutLabels.push({
                    element: input,
                    type: input.type || input.tagName,
                    id: input.id
                });
            }
        });
        
        // Test heading structure
        const headings = document.querySelectorAll('h1, h2, h3, h4, h5, h6');
        headings.forEach(heading => {
            results.headingStructure.push({
                level: parseInt(heading.tagName.charAt(1)),
                text: heading.textContent.substring(0, 50),
                element: heading
            });
        });
        
        // Test landmark elements
        const landmarks = document.querySelectorAll('main, nav, header, footer, aside, section[aria-label], section[aria-labelledby]');
        landmarks.forEach(landmark => {
            results.landmarkElements.push({
                tagName: landmark.tagName,
                role: landmark.getAttribute('role'),
                ariaLabel: landmark.getAttribute('aria-label'),
                element: landmark
            });
        });
        
        return results;
    },
    
    // Run all accessibility tests
    runAllTests: function() {
        console.group('🔍 Accessibility Test Results');
        
        // Color contrast test
        console.group('🎨 Color Contrast Test');
        const contrastResults = this.testColorContrast();
        if (contrastResults.length === 0) {
            console.log('✅ All text elements meet WCAG AA contrast requirements');
        } else {
            console.warn(`❌ Found ${contrastResults.length} elements with insufficient contrast:`);
            contrastResults.forEach(result => {
                console.warn(`- "${result.text}" has contrast ${result.contrast} (required: ${result.required})`);
            });
        }
        console.groupEnd();
        
        // Keyboard navigation test
        console.group('⌨️ Keyboard Navigation Test');
        const keyboardResults = this.testKeyboardNavigation();
        console.log(`📊 Total focusable elements: ${keyboardResults.totalFocusable}`);
        
        if (keyboardResults.elementsWithoutVisibleFocus.length === 0) {
            console.log('✅ All focusable elements have visible focus indicators');
        } else {
            console.warn(`❌ Found ${keyboardResults.elementsWithoutVisibleFocus.length} elements without visible focus`);
        }
        
        if (keyboardResults.elementsWithoutAriaLabels.length === 0) {
            console.log('✅ All buttons have appropriate labels');
        } else {
            console.warn(`❌ Found ${keyboardResults.elementsWithoutAriaLabels.length} buttons without ARIA labels`);
        }
        console.groupEnd();
        
        // Screen reader compatibility test
        console.group('🔊 Screen Reader Compatibility Test');
        const screenReaderResults = this.testScreenReaderCompatibility();
        
        if (screenReaderResults.imagesWithoutAlt.length === 0) {
            console.log('✅ All images have alt text');
        } else {
            console.warn(`❌ Found ${screenReaderResults.imagesWithoutAlt.length} images without alt text`);
        }
        
        if (screenReaderResults.formsWithoutLabels.length === 0) {
            console.log('✅ All form inputs have labels');
        } else {
            console.warn(`❌ Found ${screenReaderResults.formsWithoutLabels.length} form inputs without labels`);
        }
        
        console.log(`📊 Heading structure: ${screenReaderResults.headingStructure.length} headings found`);
        console.log(`📊 Landmark elements: ${screenReaderResults.landmarkElements.length} landmarks found`);
        console.groupEnd();
        
        console.groupEnd();
        
        return {
            colorContrast: contrastResults,
            keyboardNavigation: keyboardResults,
            screenReader: screenReaderResults
        };
    }
};

// Auto-run tests in development mode
if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    // Run tests after page load
    window.addEventListener('load', () => {
        setTimeout(() => {
            window.accessibilityTest.runAllTests();
        }, 1000);
    });
}