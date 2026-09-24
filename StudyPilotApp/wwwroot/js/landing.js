(() => {
    const root = document.documentElement;
    const nav = document.querySelector('[data-landing-nav]');
    const themeButton = document.querySelector('[data-landing-theme]');
    const storageKey = 'studypilot-landing-theme';

    const applyTheme = theme => {
        root.dataset.theme = theme;
        const icon = themeButton?.querySelector('i');
        if (icon) icon.className = theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        themeButton?.setAttribute('aria-label', theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme');
    };

    applyTheme(root.dataset.theme || 'light');
    themeButton?.addEventListener('click', () => {
        const next = root.dataset.theme === 'dark' ? 'light' : 'dark';
        try { localStorage.setItem(storageKey, next); } catch { }
        applyTheme(next);
    });

    const updateNavigation = () => nav?.classList.toggle('is-scrolled', window.scrollY > 12);
    updateNavigation();
    window.addEventListener('scroll', updateNavigation, { passive: true });

    const revealItems = document.querySelectorAll('[data-reveal]');
    if ('IntersectionObserver' in window) {
        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('is-visible');
                observer.unobserve(entry.target);
            });
        }, { threshold: 0.12 });
        revealItems.forEach(item => observer.observe(item));
    } else {
        revealItems.forEach(item => item.classList.add('is-visible'));
    }

    document.querySelectorAll('#landingNavigation a[href^="#"]').forEach(link => {
        link.addEventListener('click', () => {
            const menu = document.getElementById('landingNavigation');
            if (!menu || !menu.classList.contains('show') || !window.bootstrap) return;
            window.bootstrap.Collapse.getOrCreateInstance(menu).hide();
        });
    });
})();
