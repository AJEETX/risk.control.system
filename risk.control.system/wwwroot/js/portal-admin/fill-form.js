document.addEventListener('DOMContentLoaded', function () {
    const tabs = document.querySelectorAll('#claimFormTabs .nav-link');
    const panes = document.querySelectorAll('#claimFormTabsContent .tab-pane');

    tabs.forEach(tab => {
        tab.addEventListener('click', function (e) {
            e.preventDefault();

            // 1. Remove active state from all tab headers
            tabs.forEach(t => {
                t.classList.remove('active');
                t.setAttribute('aria-selected', 'false');
            });

            // 2. Add active state to clicked tab header
            this.classList.add('active');
            this.setAttribute('aria-selected', 'true');

            // 3. Find the matching target content pane ID
            const targetSelector = this.getAttribute('data-bs-target');
            const targetPane = document.querySelector(targetSelector);

            if (targetPane) {
                // 4. Hide all content panes completely
                panes.forEach(pane => {
                    pane.classList.remove('show', 'active');
                    pane.style.display = 'none'; // Hard override to ensure visibility changes
                });

                // 5. Show the clicked content pane
                targetPane.style.display = 'block';
                // Small timeout allows the fade animation to trigger naturally
                setTimeout(() => {
                    targetPane.classList.add('show', 'active');
                }, 10);
            }
        });
    });

    // Initialize the very first view state explicitly on load
    const activeTab = document.querySelector('#claimFormTabs .nav-link.active');
    if (activeTab) {
        const initialPane = document.querySelector(activeTab.getAttribute('data-bs-target'));
        if (initialPane) {
            initialPane.style.display = 'block';
        }
    }
});