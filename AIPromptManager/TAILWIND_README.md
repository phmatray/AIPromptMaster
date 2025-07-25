# Tailwind CSS Integration

This project uses Tailwind CSS for styling with a comprehensive set of custom components and responsive design utilities.

## Build Process

### Development
```bash
npm run build-css-watch
```
This command watches for changes and rebuilds CSS automatically during development.

### Production
```bash
npm run build-css-prod
```
This command builds minified CSS for production.

### Standard Build
```bash
npm run build-css
```
This command builds CSS without watching or minification.

## Custom Components

The project includes several pre-built component classes:

### Buttons
- `.btn-primary` - Primary action button
- `.btn-secondary` - Secondary action button  
- `.btn-success` - Success/confirmation button
- `.btn-danger` - Danger/delete button
- `.btn-sm` - Small button variant
- `.btn-lg` - Large button variant

### Forms
- `.form-input` - Standard text input styling
- `.form-textarea` - Textarea with vertical resize
- `.form-select` - Select dropdown styling
- `.form-label` - Form label styling
- `.form-error` - Error state styling

### Cards
- `.card` - Basic card container
- `.card-header` - Card header section
- `.card-body` - Card content section
- `.card-footer` - Card footer section

### Tags
- `.tag-primary` - Primary colored tag
- `.tag-secondary` - Secondary colored tag
- `.tag-success` - Success colored tag
- `.tag-warning` - Warning colored tag
- `.tag-error` - Error colored tag

### Layout
- `.container` - Responsive container with max-width
- `.sidebar` - Responsive sidebar with mobile support
- `.main-content` - Main content area
- `.grid-responsive` - Responsive grid (1-4 columns)

### Loading States
- `.loading-spinner` - Animated spinner
- `.loading-dots` - Animated dots
- `.loading-dot` - Individual dot for custom loading

### Notifications
- `.toast` - Base toast notification
- `.toast-success` - Success toast
- `.toast-error` - Error toast
- `.toast-warning` - Warning toast

### Mobile Navigation
- `.mobile-nav-toggle` - Mobile navigation toggle button
- `.mobile-overlay` - Mobile navigation overlay
- `.sidebar-hidden` - Hidden sidebar state

## Responsive Design

The project uses a mobile-first approach with the following breakpoints:
- `xs`: 475px
- `sm`: 640px (Tailwind default)
- `md`: 768px (Tailwind default)
- `lg`: 1024px (Tailwind default)
- `xl`: 1280px (Tailwind default)

## Custom Colors

The project extends Tailwind's color palette with:
- Primary colors (blue theme)
- Custom animations (fade-in, slide-up, pulse-slow)
- Additional spacing utilities

## Accessibility Features

- Focus styles with proper contrast
- Semantic HTML structure
- ARIA-friendly components
- Keyboard navigation support
- Screen reader compatibility

## Integration with .NET Build

Tailwind CSS is automatically built during the .NET build process through MSBuild targets defined in the project file.