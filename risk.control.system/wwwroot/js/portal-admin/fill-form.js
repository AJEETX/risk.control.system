document.addEventListener('DOMContentLoaded', function () {
    const tabs = document.querySelectorAll('#claimFormTabs button[data-bs-toggle="tab"]');
    const panes = document.querySelectorAll('#claimFormTabsContent .tab-pane');

    // 1. Tab Click Event Handling
    tabs.forEach(tab => {
        tab.addEventListener('click', function (e) {
            e.preventDefault();

            // Deactivate all tabs & panes
            tabs.forEach(t => {
                t.classList.remove('active');
                t.setAttribute('aria-selected', 'false');
            });
            panes.forEach(p => {
                p.classList.remove('show', 'active');
            });

            // Activate clicked tab
            this.classList.add('active');
            this.setAttribute('aria-selected', 'true');

            // Show target pane using Bootstrap CSS classes (avoiding style.display = 'none')
            const targetPaneId = this.getAttribute('data-bs-target');
            const targetPane = document.querySelector(targetPaneId);

            if (targetPane) {
                targetPane.classList.add('active');
                // Brief delay to allow CSS opacity transition
                setTimeout(() => {
                    targetPane.classList.add('show');
                }, 15);
            }
        });
    });

    // 2. Form Validation Handler: Automatically switch tabs if a required field is missing
    const form = document.querySelector('form');
    if (form) {
        form.addEventListener('invalid', function (e) {
            const invalidField = e.target;
            const parentPane = invalidField.closest('.tab-pane');

            if (parentPane && !parentPane.classList.contains('active')) {
                const paneId = parentPane.getAttribute('id');
                const matchingTab = document.querySelector(`[data-bs-target="#${paneId}"]`);
                if (matchingTab) {
                    matchingTab.click(); // Programmatically click the tab to show the error
                }
            }
        }, true);
    }
});