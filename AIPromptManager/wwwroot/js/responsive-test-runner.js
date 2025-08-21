/**
 * Comprehensive Responsive Design Test Runner
 * Automatically tests responsive behavior and generates detailed reports
 */

class ResponsiveTestRunner {
    constructor() {
        this.testResults = [];
        this.currentTest = null;
        this.viewports = {
            mobile: { width: 375, height: 667, name: 'Mobile (iPhone SE)', breakpoint: 'mobile' },
            mobileLarge: { width: 414, height: 896, name: 'Mobile Large (iPhone 11)', breakpoint: 'mobile' },
            tablet: { width: 768, height: 1024, name: 'Tablet (iPad)', breakpoint: 'tablet' },
            tabletLarge: { width: 1024, height: 1366, name: 'Tablet Large (iPad Pro)', breakpoint: 'tablet' },
            desktop: { width: 1280, height: 720, name: 'Desktop (1280x720)', breakpoint: 'desktop' },
            desktopLarge: { width: 1920, height: 1080, name: 'Desktop Large (1920x1080)', breakpoint: 'desktop' }
        };
    }

    /**
     * Run comprehensive responsive tests
     */
    async runComprehensiveTests() {
        console.log('🚀 Starting Comprehensive Responsive Design Tests...');
        this.testResults = [];
        
        // Test each viewport
        for (const [key, viewport] of Object.entries(this.viewports)) {
            console.log(`\n📱 Testing ${viewport.name} (${viewport.width}x${viewport.height})`);
            await this.testViewportComprehensive(key, viewport);
        }
        
        // Generate comprehensive report
        this.generateComprehensiveReport();
        return this.testResults;
    }

    /**
     * Test a viewport comprehensively
     */
    async testViewportComprehensive(key, viewport) {
        this.currentTest = { key, ...viewport };
        
        // Simulate viewport
        this.simulateViewport(viewport.width, viewport.height);
        await this.wait(1000); // Allow layout to settle
        
        const results = {
            viewport: viewport.name,
            width: viewport.width,
            height: viewport.height,
            breakpoint: viewport.breakpoint,
            tests: []
        };

        // Core responsive tests
        results.tests.push(await this.testLayoutStructure());
        results.tests.push(await this.testNavigationResponsiveness());
        results.tests.push(await this.testContentGrid());
        results.tests.push(await this.testFormResponsiveness());
        results.tests.push(await this.testSearchBarResponsiveness());
        results.tests.push(await this.testButtonSizes());
        results.tests.push(await this.testTextReadability());
        results.tests.push(await this.testScrollBehavior());
        
        // Touch-specific tests for mobile/tablet
        if (viewport.width <= 1024) {
            results.tests.push(await this.testTouchTargets());
            results.tests.push(await this.testTouchSpacing());
        }
        
        // Desktop-specific tests
        if (viewport.width >= 1024) {
            results.tests.push(await this.testDesktopLayout());
            results.tests.push(await this.testHoverStates());
        }
        
        this.testResults.push(results);
    }

    /**
     * Test overall layout structure
     */
    async testLayoutStructure() {
        const test = { name: 'Layout Structure', passed: true, issues: [] };
        
        try {
            // Check for horizontal overflow
            const body = document.body;
            const html = document.documentElement;
            
            if (body.scrollWidth > window.innerWidth || html.scrollWidth > window.innerWidth) {
                test.issues.push('Horizontal scrolling detected - layout overflows viewport');
            }
            
            // Check main content area
            const main = document.querySelector('main[role="main"]');
            if (main) {
                const mainRect = main.getBoundingClientRect();
                if (mainRect.width > window.innerWidth) {
                    test.issues.push(`Main content area (${Math.round(mainRect.width)}px) wider than viewport (${window.innerWidth}px)`);
                }
                
                // Check padding appropriateness
                const mainStyles = getComputedStyle(main);
                const totalPadding = parseInt(mainStyles.paddingLeft) + parseInt(mainStyles.paddingRight);
                
                if (this.currentTest.breakpoint === 'mobile' && totalPadding > 32) {
                    test.issues.push(`Mobile padding too large: ${totalPadding}px (should be ≤32px)`);
                } else if (this.currentTest.breakpoint === 'desktop' && totalPadding < 48) {
                    test.issues.push(`Desktop padding too small: ${totalPadding}px (should be ≥48px)`);
                }
            }
            
            // Check for proper viewport meta tag
            const viewportMeta = document.querySelector('meta[name="viewport"]');
            if (!viewportMeta) {
                test.issues.push('Missing viewport meta tag');
            } else {
                const content = viewportMeta.getAttribute('content');
                if (!content.includes('width=device-width')) {
                    test.issues.push('Viewport meta tag missing width=device-width');
                }
            }
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Layout structure test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Test navigation responsiveness
     */
    async testNavigationResponsiveness() {
        const test = { name: 'Navigation Responsiveness', passed: true, issues: [] };
        
        try {
            const nav = document.querySelector('nav[aria-label="Main navigation"]');
            const mobileToggle = document.querySelector('button[aria-label="Toggle navigation"]');
            const sidebar = document.querySelector('aside[role="navigation"]');
            
            if (this.currentTest.breakpoint === 'mobile') {
                // Mobile: Should have toggle button
                if (!mobileToggle) {
                    test.issues.push('Mobile navigation toggle button not found');
                } else {
                    const toggleStyles = getComputedStyle(mobileToggle);
                    if (toggleStyles.display === 'none') {
                        test.issues.push('Mobile toggle button is hidden on mobile viewport');
                    }
                }
                
                // Mobile: Navigation should be collapsible
                if (sidebar) {
                    const sidebarStyles = getComputedStyle(sidebar);
                    const isHidden = sidebarStyles.transform.includes('translateX(-100%') || 
                                   sidebarStyles.transform.includes('translateX(-256px)') ||
                                   sidebar.classList.contains('-translate-x-full');
                    
                    if (!isHidden) {
                        test.issues.push('Navigation should be hidden by default on mobile');
                    }
                }
            } else {
                // Desktop: Toggle should be hidden
                if (mobileToggle) {
                    const toggleStyles = getComputedStyle(mobileToggle);
                    if (toggleStyles.display !== 'none') {
                        test.issues.push('Mobile toggle should be hidden on desktop');
                    }
                }
                
                // Desktop: Navigation should be visible
                if (sidebar) {
                    const sidebarStyles = getComputedStyle(sidebar);
                    const isHidden = sidebarStyles.transform.includes('translateX(-100%') || 
                                   sidebarStyles.transform.includes('translateX(-256px)');
                    
                    if (isHidden) {
                        test.issues.push('Navigation should be visible on desktop');
                    }
                }
            }
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Navigation test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Test content grid responsiveness
     */
    async testContentGrid() {
        const test = { name: 'Content Grid', passed: true, issues: [] };
        
        try {
            const grids = document.querySelectorAll('[class*="grid"]');
            
            grids.forEach((grid, index) => {
                const gridStyles = getComputedStyle(grid);
                const gridColumns = gridStyles.gridTemplateColumns;
                
                if (gridColumns && gridColumns !== 'none') {
                    const columnCount = gridColumns.split(' ').length;
                    
                    if (this.currentTest.breakpoint === 'mobile') {
                        if (columnCount > 1) {
                            test.issues.push(`Grid ${index + 1}: Mobile should use single column, found ${columnCount} columns`);
                        }
                    } else if (this.currentTest.breakpoint === 'desktop') {
                        if (columnCount < 2) {
                            test.issues.push(`Grid ${index + 1}: Desktop should use multiple columns, found ${columnCount} column(s)`);
                        }
                    }
                }
                
                // Check grid items don't overflow
                const gridItems = grid.children;
                Array.from(gridItems).forEach((item, itemIndex) => {
                    const itemRect = item.getBoundingClientRect();
                    if (itemRect.width > window.innerWidth) {
                        test.issues.push(`Grid ${index + 1}, item ${itemIndex + 1}: Item wider than viewport`);
                    }
                });
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Content grid test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Test form responsiveness
     */
    async testFormResponsiveness() {
        const test = { name: 'Form Responsiveness', passed: true, issues: [] };
        
        try {
            const forms = document.querySelectorAll('form');
            
            forms.forEach((form, formIndex) => {
                // Check form width
                const formRect = form.getBoundingClientRect();
                if (formRect.width > window.innerWidth) {
                    test.issues.push(`Form ${formIndex + 1}: Form wider than viewport`);
                }
                
                // Check input fields
                const inputs = form.querySelectorAll('input, textarea, select');
                inputs.forEach((input, inputIndex) => {
                    const inputRect = input.getBoundingClientRect();
                    
                    // Check input width
                    if (inputRect.width > window.innerWidth - 32) {
                        test.issues.push(`Form ${formIndex + 1}, input ${inputIndex + 1}: Input too wide for viewport`);
                    }
                    
                    // Check minimum height for touch
                    if (this.currentTest.breakpoint === 'mobile' && inputRect.height < 44) {
                        test.issues.push(`Form ${formIndex + 1}, input ${inputIndex + 1}: Input too short for touch (${Math.round(inputRect.height)}px < 44px)`);
                    }
                });
                
                // Check form layout on mobile
                if (this.currentTest.breakpoint === 'mobile') {
                    const formGroups = form.querySelectorAll('.flex');
                    formGroups.forEach((group, groupIndex) => {
                        const groupStyles = getComputedStyle(group);
                        if (groupStyles.flexDirection !== 'column') {
                            // Check if items are stacked properly
                            const children = Array.from(group.children);
                            if (children.length > 1) {
                                const firstChild = children[0].getBoundingClientRect();
                                const secondChild = children[1].getBoundingClientRect();
                                
                                if (firstChild.bottom > secondChild.top - 8) {
                                    // Items might be overlapping or too close
                                    test.issues.push(`Form ${formIndex + 1}: Form elements may not be properly stacked on mobile`);
                                }
                            }
                        }
                    });
                }
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Form responsiveness test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Test search bar responsiveness
     */
    async testSearchBarResponsiveness() {
        const test = { name: 'Search Bar Responsiveness', passed: true, issues: [] };
        
        try {
            const searchInputs = document.querySelectorAll('input[type="search"], input[placeholder*="search" i], input[role="searchbox"]');
            
            searchInputs.forEach((input, index) => {
                const inputRect = input.getBoundingClientRect();
                const parentRect = input.parentElement.getBoundingClientRect();
                
                // Check search input width
                if (this.currentTest.breakpoint === 'mobile') {
                    const widthRatio = inputRect.width / parentRect.width;
                    if (widthRatio < 0.8) {
                        test.issues.push(`Search input ${index + 1}: Should be wider on mobile (${Math.round(widthRatio * 100)}% of parent)`);
                    }
                }
                
                // Check search input height for touch
                if (this.currentTest.breakpoint === 'mobile' && inputRect.height < 44) {
                    test.issues.push(`Search input ${index + 1}: Too short for touch (${Math.round(inputRect.height)}px < 44px)`);
                }
                
                // Check if search input overflows
                if (inputRect.width > window.innerWidth - 32) {
                    test.issues.push(`Search input ${index + 1}: Too wide for viewport`);
                }
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Search bar test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Test button sizes and touch targets
     */
    async testButtonSizes() {
        const test = { name: 'Button Sizes', passed: true, issues: [] };
        
        try {
            const buttons = document.querySelectorAll('button, a[role="button"], [tabindex="0"]');
            
            buttons.forEach((button, index) => {
                const rect = button.getBoundingClientRect();
                
                // Skip hidden or very small decorative elements
                if (rect.width < 10 || rect.height < 10) return;
                
                if (this.currentTest.breakpoint === 'mobile') {
                    // Check minimum touch target size
                    if (rect.width < 44 || rect.height < 44) {
                        // Allow exceptions for certain elements
                        const isException = button.matches('input[type="text"], input[type="email"], textarea') ||
                                          button.closest('.tag') ||
                                          button.matches('.text-xs, .text-sm') ||
                                          button.getAttribute('aria-hidden') === 'true';
                        
                        if (!isException) {
                            test.issues.push(`Button ${index + 1}: Touch target too small (${Math.round(rect.width)}x${Math.round(rect.height)}px < 44x44px)`);
                        }
                    }
                }
                
                // Check if button text is readable
                const buttonText = button.textContent?.trim();
                if (buttonText && buttonText.length > 0) {
                    const styles = getComputedStyle(button);
                    const fontSize = parseInt(styles.fontSize);
                    
                    if (fontSize < 14) {
                        test.issues.push(`Button ${index + 1}: Text too small (${fontSize}px < 14px)`);
                    }
                }
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Button sizes test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Test text readability
     */
    async testTextReadability() {
        const test = { name: 'Text Readability', passed: true, issues: [] };
        
        try {
            const textElements = document.querySelectorAll('p, h1, h2, h3, h4, h5, h6, span, div');
            
            textElements.forEach((element, index) => {
                const text = element.textContent?.trim();
                if (!text || text.length === 0) return;
                
                const styles = getComputedStyle(element);
                const fontSize = parseInt(styles.fontSize);
                const lineHeight = parseFloat(styles.lineHeight);
                
                // Check minimum font size
                if (fontSize < 12) {
                    test.issues.push(`Text element ${index + 1}: Font too small (${fontSize}px < 12px)`);
                }
                
                // Check line height for readability
                if (lineHeight && lineHeight < fontSize * 1.2) {
                    test.issues.push(`Text element ${index + 1}: Line height too small for readability`);
                }
                
                // Check if text overflows horizontally
                const rect = element.getBoundingClientRect();
                if (rect.width > window.innerWidth) {
                    test.issues.push(`Text element ${index + 1}: Text overflows viewport width`);
                }
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Text readability test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Test scroll behavior
     */
    async testScrollBehavior() {
        const test = { name: 'Scroll Behavior', passed: true, issues: [] };
        
        try {
            // Check for horizontal scrolling
            if (document.body.scrollWidth > window.innerWidth) {
                test.issues.push('Horizontal scrolling detected');
            }
            
            // Check scrollable containers
            const scrollableElements = document.querySelectorAll('[style*="overflow"], .overflow-auto, .overflow-y-auto, .overflow-x-auto');
            
            scrollableElements.forEach((element, index) => {
                const styles = getComputedStyle(element);
                const rect = element.getBoundingClientRect();
                
                // Check if scrollable area is accessible
                if (styles.overflow === 'auto' || styles.overflowY === 'auto') {
                    if (rect.height < 100 && this.currentTest.breakpoint === 'mobile') {
                        test.issues.push(`Scrollable element ${index + 1}: May be too short for mobile interaction`);
                    }
                }
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Scroll behavior test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Test touch targets (mobile/tablet only)
     */
    async testTouchTargets() {
        const test = { name: 'Touch Targets', passed: true, issues: [] };
        
        try {
            const interactiveElements = document.querySelectorAll('button, a, input, select, textarea, [role="button"], [tabindex="0"]');
            
            interactiveElements.forEach((element, index) => {
                const rect = element.getBoundingClientRect();
                
                // Skip hidden elements
                if (rect.width === 0 || rect.height === 0) return;
                
                // Check minimum touch target size (44x44px recommended by Apple/Google)
                if (rect.width < 44 || rect.height < 44) {
                    const isException = element.matches('input[type="text"], input[type="email"], textarea') ||
                                      element.closest('.tag') ||
                                      element.matches('.text-xs, .text-sm') ||
                                      element.getAttribute('aria-hidden') === 'true';
                    
                    if (!isException) {
                        test.issues.push(`Touch target ${index + 1}: Too small (${Math.round(rect.width)}x${Math.round(rect.height)}px < 44x44px) - ${element.tagName.toLowerCase()}`);
                    }
                }
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Touch targets test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Test touch spacing (mobile/tablet only)
     */
    async testTouchSpacing() {
        const test = { name: 'Touch Spacing', passed: true, issues: [] };
        
        try {
            const interactiveElements = Array.from(document.querySelectorAll('button, a, input, select, textarea, [role="button"], [tabindex="0"]'));
            
            for (let i = 0; i < interactiveElements.length - 1; i++) {
                const current = interactiveElements[i];
                const next = interactiveElements[i + 1];
                
                const currentRect = current.getBoundingClientRect();
                const nextRect = next.getBoundingClientRect();
                
                // Skip hidden elements
                if (currentRect.width === 0 || nextRect.width === 0) continue;
                
                // Calculate distance between elements
                const horizontalDistance = Math.max(0, nextRect.left - currentRect.right);
                const verticalDistance = Math.max(0, nextRect.top - currentRect.bottom);
                const distance = Math.min(horizontalDistance, verticalDistance);
                
                // Check if elements are close enough to cause touch issues
                if (distance < 8 && distance > 0) {
                    // Check if they're in the same container (likely related)
                    const currentParent = current.closest('div, section, article, nav');
                    const nextParent = next.closest('div, section, article, nav');
                    
                    if (currentParent === nextParent) {
                        test.issues.push(`Touch elements too close: ${distance.toFixed(1)}px apart (should be ≥8px)`);
                    }
                }
            }
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Touch spacing test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Test desktop-specific layout (desktop only)
     */
    async testDesktopLayout() {
        const test = { name: 'Desktop Layout', passed: true, issues: [] };
        
        try {
            // Check if layout takes advantage of desktop space
            const main = document.querySelector('main[role="main"]');
            if (main) {
                const mainRect = main.getBoundingClientRect();
                const viewportWidth = window.innerWidth;
                
                // Check if content is too narrow on desktop
                if (mainRect.width < viewportWidth * 0.6) {
                    test.issues.push(`Desktop layout may be too narrow (${Math.round(mainRect.width)}px < ${Math.round(viewportWidth * 0.6)}px)`);
                }
            }
            
            // Check sidebar utilization
            const sidebar = document.querySelector('aside[role="navigation"]');
            if (sidebar) {
                const sidebarStyles = getComputedStyle(sidebar);
                if (sidebarStyles.display === 'none') {
                    test.issues.push('Navigation sidebar should be visible on desktop');
                }
            }
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Desktop layout test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Test hover states (desktop only)
     */
    async testHoverStates() {
        const test = { name: 'Hover States', passed: true, issues: [] };
        
        try {
            const hoverableElements = document.querySelectorAll('button, a, [role="button"]');
            
            hoverableElements.forEach((element, index) => {
                // Check if element has hover styles defined
                const styles = getComputedStyle(element);
                
                // This is a basic check - in a real test, you'd simulate hover
                // For now, we'll check if the element has cursor pointer
                if (styles.cursor !== 'pointer' && element.tagName.toLowerCase() !== 'input') {
                    test.issues.push(`Interactive element ${index + 1}: Missing pointer cursor for hover indication`);
                }
            });
            
        } catch (error) {
            test.passed = false;
            test.issues.push(`Hover states test error: ${error.message}`);
        }
        
        test.passed = test.issues.length === 0;
        return test;
    }

    /**
     * Simulate viewport size
     */
    simulateViewport(width, height) {
        // Set CSS custom properties for testing
        document.documentElement.style.setProperty('--test-viewport-width', `${width}px`);
        document.documentElement.style.setProperty('--test-viewport-height', `${height}px`);
        
        // Dispatch resize event
        window.dispatchEvent(new Event('resize'));
    }

    /**
     * Wait utility
     */
    wait(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    /**
     * Generate comprehensive report
     */
    generateComprehensiveReport() {
        console.log('\n📊 Comprehensive Responsive Design Test Report');
        console.log('='.repeat(50));
        
        let totalTests = 0;
        let passedTests = 0;
        let totalIssues = 0;
        const issuesByCategory = {};
        
        this.testResults.forEach(viewport => {
            console.log(`\n📱 ${viewport.viewport} (${viewport.width}x${viewport.height})`);
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
                        
                        // Categorize issues
                        const category = test.name;
                        if (!issuesByCategory[category]) {
                            issuesByCategory[category] = 0;
                        }
                        issuesByCategory[category]++;
                    });
                }
            });
        });
        
        console.log('\n📈 Summary');
        console.log('='.repeat(20));
        console.log(`Total Tests: ${totalTests}`);
        console.log(`Passed: ${passedTests}`);
        console.log(`Failed: ${totalTests - passedTests}`);
        console.log(`Issues Found: ${totalIssues}`);
        console.log(`Success Rate: ${Math.round((passedTests / totalTests) * 100)}%`);
        
        if (Object.keys(issuesByCategory).length > 0) {
            console.log('\n📊 Issues by Category');
            console.log('-'.repeat(25));
            Object.entries(issuesByCategory)
                .sort(([,a], [,b]) => b - a)
                .forEach(([category, count]) => {
                    console.log(`${category}: ${count} issue${count > 1 ? 's' : ''}`);
                });
        }
        
        if (totalIssues === 0) {
            console.log('\n🎉 All responsive design tests passed!');
            console.log('Your application is fully responsive across all tested viewports.');
        } else {
            console.log(`\n⚠️  Found ${totalIssues} responsive design issues that need attention.`);
            console.log('Review the issues above to improve responsive behavior.');
        }
        
        // Generate recommendations
        this.generateRecommendations();
    }

    /**
     * Generate recommendations based on test results
     */
    generateRecommendations() {
        console.log('\n💡 Recommendations');
        console.log('='.repeat(20));
        
        const allIssues = this.testResults.flatMap(viewport => 
            viewport.tests.flatMap(test => test.issues)
        );
        
        const recommendations = new Set();
        
        allIssues.forEach(issue => {
            if (issue.includes('Touch target too small')) {
                recommendations.add('• Increase button and interactive element sizes to minimum 44x44px for mobile');
            }
            if (issue.includes('padding too large') || issue.includes('padding too small')) {
                recommendations.add('• Adjust padding using responsive utilities (e.g., p-4 sm:p-6 lg:p-8)');
            }
            if (issue.includes('wider than viewport')) {
                recommendations.add('• Use max-width and responsive width classes to prevent overflow');
            }
            if (issue.includes('single column') || issue.includes('multiple columns')) {
                recommendations.add('• Implement responsive grid layouts (grid-cols-1 md:grid-cols-2 lg:grid-cols-3)');
            }
            if (issue.includes('Navigation')) {
                recommendations.add('• Ensure navigation is properly hidden/shown based on viewport size');
            }
            if (issue.includes('Font too small')) {
                recommendations.add('• Use responsive typography classes (text-sm md:text-base lg:text-lg)');
            }
        });
        
        if (recommendations.size === 0) {
            console.log('No specific recommendations - your responsive design is working well!');
        } else {
            recommendations.forEach(rec => console.log(rec));
        }
    }
}

// Export for global use
window.ResponsiveTestRunner = ResponsiveTestRunner;

// Auto-run if requested
if (window.location.search.includes('run-comprehensive-responsive-tests')) {
    document.addEventListener('DOMContentLoaded', async () => {
        const runner = new ResponsiveTestRunner();
        await runner.runComprehensiveTests();
    });
}