// Apply theme to <html> immediately to prevent flash
(function () {
    const savedTheme = localStorage.getItem("theme");
    const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
    if (savedTheme === "dark" || (!savedTheme && prefersDark)) {
        document.documentElement.classList.add("dark-mode");
    }
})();

function initThemeToggle() {
    const themeToggle = document.getElementById("theme-toggle");
    if (!themeToggle) return;

    const themeIcon = themeToggle.querySelector("i");
    if (!themeIcon) return;

    // Always read from localStorage — DOM state may have been reset by Blazor
    const savedTheme = localStorage.getItem("theme");
    const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
    const isDark = savedTheme === "dark" || (!savedTheme && prefersDark);

    // Re-apply the class (Blazor enhanced nav may have wiped it)
    if (isDark) {
        document.documentElement.classList.add("dark-mode");
        themeIcon.classList.remove("bi-sun");
        themeIcon.classList.add("bi-moon");
    } else {
        document.documentElement.classList.remove("dark-mode");
        themeIcon.classList.remove("bi-moon");
        themeIcon.classList.add("bi-sun");
    }

    // Remove old listener by cloning
    const newToggle = themeToggle.cloneNode(true);
    themeToggle.parentNode.replaceChild(newToggle, themeToggle);

    newToggle.addEventListener("click", () => {
        document.documentElement.classList.toggle("dark-mode");
        const icon = newToggle.querySelector("i");

        if (document.documentElement.classList.contains("dark-mode")) {
            localStorage.setItem("theme", "dark");
            icon.classList.remove("bi-sun");
            icon.classList.add("bi-moon");
        } else {
            localStorage.setItem("theme", "light");
            icon.classList.remove("bi-moon");
            icon.classList.add("bi-sun");
        }
    });
}

// Run on initial load
document.addEventListener("DOMContentLoaded", initThemeToggle);
