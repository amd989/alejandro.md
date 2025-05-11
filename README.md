# Alejandro Mora - Personal Website

A personal portfolio website showcasing my work, blog posts, and projects.

## Implementation Details

This website is built with ASP.NET Core 8 using Blazor for rendering components. The design is inspired by [Carlos Cuesta's website](https://carloscuesta.me).

## Features

- **Clean, Minimalist Design**: Focus on content with a clean, readable interface
- **Light/Dark Mode**: Toggle between light and dark themes
- **Responsive Layout**: Works well on mobile, tablet, and desktop devices
- **GitHub Integration**: Fetches and displays repositories using the GitHub API
- **Blog Integration**: Displays latest blog posts from WordPress

## File Structure

- `app.css`: Main stylesheet containing all styling for the website
- `color-modes.js`: JavaScript to handle light/dark theme toggle
- `App.razor`: Root layout component that sets up the HTML structure
- `Home.razor`: Home page component with intro, blog posts, and projects sections
- `NavMenu.razor`: Navigation component 
- `MainLayout.razor`: Main layout structure for the app

## How to Run

1. Clone the repository
2. Navigate to the project directory
3. Run `dotnet restore` to restore dependencies
4. Run `dotnet run` to start the development server
5. Open your browser to `https://localhost:7171`

## Styling Notes

The styling is implemented using CSS variables for easy theming and consistency. The color scheme and typography have been chosen to provide optimal readability while maintaining a clean, professional look.

```css
:root {
  --font-sans: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Oxygen,
    Ubuntu, Cantarell, "Open Sans", "Helvetica Neue", sans-serif;
  --color-background: #fff;
  --color-text: #000;
  --color-text-secondary: #666;
  --color-link: #0070f3;
  --color-hover: #0060df;
  --color-border: #eaeaea;
  --box-shadow: 0 4px 14px rgba(0, 0, 0, 0.05);
  --radius: 8px;
  --transition: all 0.2s ease;
  --max-width: 960px;
}
```

## Dependencies

- ASP.NET Core 8
- Blazor WebAssembly and Server
- Bootstrap 5.3 (minimal usage, mostly for grid layout)
- Bootstrap Icons

## License

MIT