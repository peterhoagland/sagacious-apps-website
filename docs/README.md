# Sagacious Apps Website

A modern, responsive static website for Sagacious Apps, showcasing our mobile learning applications.

## Local Development

To preview the site locally, you can use any static file server. Here are a few options:

### Option 1: Python (if installed)
```bash
cd docs
python -m http.server 8000
```
Then visit `http://localhost:8000`

### Option 2: Node.js (if installed)
```bash
npx serve docs
```

### Option 3: VS Code Live Server
Install the "Live Server" extension and right-click `index.html` → "Open with Live Server"

## Customization

Before deploying, update the following placeholders in `index.html`:

1. **Google Play Developer ID**: Replace `YOUR_DEVELOPER_ID` with your actual Google Play developer ID
2. **Rad Libs Package ID**: Replace `YOUR_RAD_LIBS_PACKAGE_ID` with the actual package ID (likely `com.sagaciousapps.radlibs` or similar)
3. **Images**: Replace placeholder boxes with actual app screenshots and graphics

## File Structure

```
docs/
├── index.html      # Main HTML file
├── styles.css      # All CSS styles
├── script.js       # JavaScript functionality
└── README.md       # This file
```

## Technologies Used

- Pure HTML5, CSS3, and JavaScript (no frameworks)
- Google Fonts (Inter)
- Material Design Icons (SVG)
- CSS Grid and Flexbox for layout
- CSS Custom Properties for theming
- Intersection Observer API for animations
