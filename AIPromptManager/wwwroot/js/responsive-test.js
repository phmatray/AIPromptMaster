/**
 * Responsive Design Testing Utility
 * Tests layout and functionality across different viewport sizes
 */

class ResponsiveDesignTester {
    constructor() {
        this.viewports = {
            mobile: { width: 375, height: 667, name: 'Mobile (iPhone SE)' },
            mobileLarge: { width: 414, height: 896, name: 'Mobile Large (iPhone 11)' },
            tablet: { width: 768, height: 1024, name: 'Tablet (iPad)' },
            tabletLarge: { width: 1024, height: 1366, name: 'Tablet Large (iPad Pro)' },
            desktop: { width: 1280, height: 720, name: 'Desktop (1280x720)' },
            desktopLarge: { width: 1920, height: 1080, name: 'Desktop Large (1920x1080)' }
        };
        
        this.testResults = [];
        this.currentViewport = null;
    }

    /**
     * Run all responsive design tests
     */
    async runAllTests() {
        console.log('🧪 Starting Responsive Design Tests...');
        this.testResults = [];
        
        for (const [key, viewport] of Object.entries(this.viewports)) {
            console.log(`\n📱 Testing ${viewport.name} (${viewport.width}x${viewport.height})`);
            await this.testViewport(key, viewport);
        }
        
        this.generateReport();
        return this.testResults;
    }

    /**
     * Test a specific viewport size
     */
    async testViewport(key, viewport) {
        this.currentViewport = { key, ...viewport };
        
        // Simulate viewport resize
        this.setViewportSize(viewport.width, viewport.height);
        
        // Wait for layout to settle
        await this.wait(500);
        
        const results = {
            viewport: viewport.name,
            width: viewport.width,
            height: viewport.height,
            tests: []
        };

        // Test navigation
        results.tests.push(await this.testNavigation());
        
        // Test main content layout
        results.tests.push(await this.testMainLayout());
        
        // Test prompt cards layout
        results.tests.push(await this.testPromptCards());
        
        // Test forms responsiveness
        results.tests.push(await this.testForms());
        
        // Test search functionality
        results.tests.push(await this.testSearch());
        
        // Test touch interactions (for mobile/tablet)
        if (viewport.width <= 1024) {
            results.tests.push(await this.testTouchInteractions());
        }
        
        this.testResults.push(results);
    }

    /**
     * Test navigation responsiveness
     */
    async testNavigation() {
        const test = { name: 'Navigation', passed: true, issues: [] };
        
        try {
            const nav = document.querySelector('nav[aria-label="Main navigation"]');
            const mobileToggle = document.querySelector('button[aria-label="Toggle navigation"]');
            
            if (this.currentViewport.width < 1024) {
                // Mobile: Navigation should be collapsible
                if (!mobileToggle) {
                    test.passed = false;
                    test.issues.push('Mobile navigation toggle button not found');
                }
                
                // Check if navigation is initially hidden on mobile
                const sidebar = document.querySelector('aside[role="navigation"]');
                if (sidebar) {
                    const isHidden = sidebar.classList.contains('-translate-x-full') || 
                                   getComputedStyle(sidebar).transform.includes('translateX(-100%');
                    if (!isHidden) {
                        test.issues.push('Navigation should be hidden by default on mobile');
                    }
                }
            } else {
                // Desktop: Navigation should be visible
                if (mobileToggle && getComputedStyle(mobileToggle).display !== 'none') {
                    test.issues.push('Mobile toggle should be hidden on desktop');
                }
            }
            
            // Test navigation links accessibility
            const navLinks = document.querySelectorAll('nav a[role="menuitem"]');
            navLinks.forEach((link, index) => {
                if (!link.getAttribute('aria-label') && !link.textContent.trim()) {
                    test.issues.push(`Navigation link ${index + 1} missing accessible label`);
                }
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Navigation test error: ${error.message}`);
        }
        
        if (test.issues.length > 0) {
            test.passed = false;
        }
        
        return test;
    }

    /**
     * Test main content layout
     */
    async testMainLayout() {
        const test = { name: 'Main Layout', passed: true, issues: [] };
        
        try {
            const main = document.querySelector('main[role="main"]');
            if (!main) {
                test.passed = false;
                test.issues.push('Main content area not found');
                return test;
            }
            
            const mainStyles = getComputedStyle(main);
            const padding = parseInt(mainStyles.paddingLeft) + parseInt(mainStyles.paddingRight);
            
            // Check appropriate padding for viewport
            if (this.currentViewport.width < 640) {
                // Mobile: Should have minimal padding
                if (padding > 32) {
                    test.issues.push(`Mobile padding too large: ${padding}px`);
                }
            } else if (this.currentViewport.width >= 1024) {
                // Desktop: Should have adequate padding
                if (padding < 48) {
                    test.issues.push(`Desktop padding too small: ${padding}px`);
                }
            }
            
            // Check for horizontal scrolling
            if (document.body.scrollWidth > window.innerWidth) {
                test.issues.push('Horizontal scrolling detected');
            }
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Main layout test error: ${error.message}`);
        }
        
        if (test.issues.length > 0) {
            test.passed = false;
        }
        
        return test;
    }

    /**
     * Test prompt cards layout
     */
    async testPromptCards() {
        const test = { name: 'Prompt Cards', passed: true, issues: [] };
        
        try {
            const promptGrid = document.querySelector('[class*="grid"]');
            if (!promptGrid) {
                test.issues.push('Prompt grid not found');
                return test;
            }
            
            const gridStyles = getComputedStyle(promptGrid);
            const gridColumns = gridStyles.gridTemplateColumns;
            
            // Check grid responsiveness
            if (this.currentViewport.width < 640) {
                // Mobile: Should be single column
                if (gridColumns !== 'none' && !gridColumns.includes('1fr') && gridColumns.split(' ').length > 1) {
                    test.issues.push(`Mobile should use single column grid, found: ${gridColumns}`);
                }
            } else if (this.currentViewport.width >= 1024) {
                // Desktop: Should be multi-column
                if (gridColumns === 'none' || gridColumns.split(' ').length < 2) {
                    test.issues.push(`Desktop should use multi-column grid, found: ${gridColumns}`);
                }
            }
            
            // Test individual prompt cards
            const promptCards = document.querySelectorAll('[class*="prompt-card"], .bg-white.shadow');
            promptCards.forEach((card, index) => {
                const cardRect = card.getBoundingClientRect();
                
                // Check if card fits within viewport
                if (cardRect.width > window.innerWidth) {
                    test.issues.push(`Prompt card ${index + 1} wider than viewport`);
                }
                
                // Check minimum touch target size for mobile
                if (this.currentViewport.width <= 768) {
                    const buttons = card.querySelectorAll('button, a');
                    buttons.forEach((button, btnIndex) => {
                        const btnRect = button.getBoundingClientRect();
                        if (btnRect.width < 44 || btnRect.height < 44) {
                            test.issues.push(`Touch target too small in card ${index + 1}, button ${btnIndex + 1}: ${btnRect.width}x${btnRect.height}`);
                        }
                    });
                }
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Prompt cards test error: ${error.message}`);
        }
        
        if (test.issues.length > 0) {
            test.passed = false;
        }
        
        return test;
    }

    /**
     * Test forms responsiveness
     */
    async testForms() {
        const test = { name: 'Forms', passed: true, issues: [] };
        
        try {
            const forms = document.querySelectorAll('form');
            
            forms.forEach((form, formIndex) => {
                const formRect = form.getBoundingClientRect();
                
                // Check form width
                if (formRect.width > window.innerWidth) {
                    test.issues.push(`Form ${formIndex + 1} wider than viewport`);
                }
                
                // Check input fields
                const inputs = form.querySelectorAll('input, textarea, select');
                inputs.forEach((input, inputIndex) => {
                    const inputRect = input.getBoundingClientRect();
                    
                    // Check input width
                    if (inputRect.width > window.innerWidth - 32) {
                        test.issues.push(`Input field too wide in form ${formIndex + 1}, input ${inputIndex + 1}`);
                    }
                    
                    // Check minimum height for touch
                    if (this.currentViewport.width <= 768 && inputRect.height < 44) {
                        test.issues.push(`Input field too short for touch in form ${formIndex + 1}, input ${inputIndex + 1}: ${inputRect.height}px`);
                    }
                });
                
                // Check form buttons
                const buttons = form.querySelectorAll('button[type="submit"], button[type="button"]');
                buttons.forEach((button, btnIndex) => {
                    const btnRect = button.getBoundingClientRect();
                    
                    if (this.currentViewport.width <= 768) {
                        // Mobile: Buttons should be large enough for touch
                        if (btnRect.height < 44) {
                            test.issues.push(`Button too short for touch in form ${formIndex + 1}, button ${btnIndex + 1}: ${btnRect.height}px`);
                        }
                    }
                });
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Forms test error: ${error.message}`);
        }
        
        if (test.issues.length > 0) {
            test.passed = false;
        }
        
        return test;
    }

    /**
     * Test search functionality
     */
    async testSearch() {
        const test = { name: 'Search', passed: true, issues: [] };
        
        try {
            const searchInput = document.querySelector('input[type="search"], input[placeholder*="search" i]');
            
            if (searchInput) {
                const searchRect = searchInput.getBoundingClientRect();
                
                // Check search input width
                if (this.currentViewport.width < 640) {
                    // Mobile: Search should be full width or nearly full width
                    const parentRect = searchInput.parentElement.getBoundingClientRect();
                    const widthRatio = searchRect.width / parentRect.width;
                    if (widthRatio < 0.8) {
                        test.issues.push(`Search input should be wider on mobile: ${Math.round(widthRatio * 100)}% of parent`);
                    }
                }
                
                // Check search input height for touch
                if (this.currentViewport.width <= 768 && searchRect.height < 44) {
                    test.issues.push(`Search input too short for touch: ${searchRect.height}px`);
                }
            }
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Search test error: ${error.message}`);
        }
        
        if (test.issues.length > 0) {
            test.passed = false;
        }
        
        return test;
    }

    /**
     * Test touch interactions for mobile/tablet
     */
    async testTouchInteractions() {
        const test = { name: 'Touch Interactions', passed: true, issues: [] };
        
        try {
            // Find all interactive elements
            const interactiveElements = document.querySelectorAll('button, a, input, select, textarea, [role="button"], [tabindex="0"]');
            
            interactiveElements.forEach((element, index) => {
                const rect = element.getBoundingClientRect();
                
                // Check minimum touch target size (44x44px recommended)
                if (rect.width < 44 || rect.height < 44) {
                    // Allow exceptions for certain elements
                    const isException = element.matches('input[type="text"], input[type="email"], textarea') ||
                                      element.closest('.tag') || // Small tag elements
                                      element.matches('.text-xs, .text-sm'); // Small text elements
                    
                    if (!isException) {
                        test.issues.push(`Touch target too small (${Math.round(rect.width)}x${Math.round(rect.height)}px): ${element.tagName.toLowerCase()}${element.className ? '.' + element.className.split(' ')[0] : ''}`);
                    }
                }
                
                // Check spacing between touch targets
                const nextElement = interactiveElements[index + 1];
                if (nextElement) {
                    const nextRect = nextElement.getBoundingClientRect();
                    const distance = Math.sqrt(
                        Math.pow(nextRect.left - rect.right, 2) + 
                        Math.pow(nextRect.top - rect.bottom, 2)
                    );
                    
                    if (distance < 8 && distance > 0) {
                        test.issues.push(`Touch targets too close together: ${distance.toFixed(1)}px apart`);
                    }
                }
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Touch interactions test error: ${error.message}`);
        }
        
        if (test.issues.length > 0) {
            test.passed = false;
        }
        
        return test;
    }

    /**
     * Set viewport size for testing
     */
    setViewportSize(width, height) {
        // This would typically be done by the test runner
        // For browser testing, we simulate by setting CSS
        document.documentElement.style.setProperty('--test-viewport-width', `${width}px`);
        document.documentElement.style.setProperty('--test-viewport-height', `${height}px`);
        
        // Trigger resize event
        window.dispatchEvent(new Event('resize'));
    }

    /**
     * Wait for specified milliseconds
     */
    wait(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    /**
     * Generate test report
     */
    generateReport() {
        console.log('\n📊 Responsive Design Test Report');
        console.log('================================');
        
        let totalTests = 0;
        let passedTests = 0;
        let totalIssues = 0;
        
        this.testResults.forEach(viewport => {
            console.log(`\n${viewport.viewport} (${viewport.width}x${viewport.height})`);
            console.log('-'.repeat(viewport.viewport.length + 20));
            
            viewport.tests.forEach(test => {
                totalTests++;
                const status = test.passed ? '✅' : '❌';
                console.log(`${status} ${test.name}`);
                
                if (test.passed) {
                    passedTests++;
                } else {
                    test.issues.forEach(issue => {
                        console.log(`   ⚠️  ${issue}`);
                        totalIssues++;
                    });
                }
            });
        });
        
        console.log('\n📈 Summary');
        console.log('==========');
        console.log(`Total Tests: ${totalTests}`);
        console.log(`Passed: ${passedTests}`);
        console.log(`Failed: ${totalTests - passedTests}`);
        console.log(`Issues Found: ${totalIssues}`);
        console.log(`Success Rate: ${Math.round((passedTests / totalTests) * 100)}%`);
        
        if (totalIssues === 0) {
            console.log('\n🎉 All responsive design tests passed!');
        } else {
            console.log(`\n⚠️  Found ${totalIssues} responsive design issues that need attention.`);
        }
    }
}

// Export for use in other scripts
window.ResponsiveDesignTester = ResponsiveDesignTester;

// Auto-run tests if requested
if (window.location.search.includes('run-responsive-tests')) {
    document.addEventListener('DOMContentLoaded', async () => {
        const tester = new ResponsiveDesignTester();
        await tester.runAllTests();
    });
}