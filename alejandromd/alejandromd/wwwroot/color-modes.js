// Theme toggle functionality
document.addEventListener("DOMContentLoaded", () => {
    // Get the theme toggle button
    const themeToggle = document.getElementById("theme-toggle");
    const themeIcon = themeToggle.querySelector("i");

    // Check for saved theme preference or use preferred color scheme
    const savedTheme = localStorage.getItem("theme");
    const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;

    // Apply the theme based on saved preference or system preference
    if (savedTheme === "dark" || (!savedTheme && prefersDark)) {
        document.body.classList.add("dark-mode");
        themeIcon.classList.remove("bi-sun");
        themeIcon.classList.add("bi-moon");
    }

    // Toggle theme when button is clicked
    themeToggle.addEventListener("click", () => {
        document.body.classList.toggle("dark-mode");

        if (document.body.classList.contains("dark-mode")) {
            localStorage.setItem("theme", "dark");
            themeIcon.classList.remove("bi-sun");
            themeIcon.classList.add("bi-moon");
        } else {
            localStorage.setItem("theme", "light");
            themeIcon.classList.remove("bi-moon");
            themeIcon.classList.add("bi-sun");
        }
    });
});