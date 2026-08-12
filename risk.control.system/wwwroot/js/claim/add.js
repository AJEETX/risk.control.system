document.addEventListener('DOMContentLoaded', function () {
    const tabButtons = document.querySelectorAll('#claimFormTabs button[data-bs-toggle="tab"]');
    const tabPanes = document.querySelectorAll('#claimFormTabsContent .tab-pane');

    // 1. Manual Tab Switching
    tabButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            e.preventDefault();

            // Deactivate all tabs & panes
            tabButtons.forEach(btn => {
                btn.classList.remove('active');
                btn.setAttribute('aria-selected', 'false');
            });

            tabPanes.forEach(pane => {
                pane.classList.remove('show', 'active');
            });

            // Activate clicked tab
            this.classList.add('active');
            this.setAttribute('aria-selected', 'true');

            // Show matching panel
            const targetPaneId = this.getAttribute('data-bs-target');
            const targetPane = document.querySelector(targetPaneId);

            if (targetPane) {
                targetPane.classList.add('active');
                // Small delay to trigger smooth fade-in animation
                setTimeout(() => {
                    targetPane.classList.add('show');
                }, 15);
            }
        });
    });

    // 2. Validation Handler: Jump to tab containing the invalid field
    const form = document.getElementById('claimSubmitForm') || document.querySelector('form');
    if (form) {
        form.addEventListener('invalid', function (e) {
            const invalidField = e.target;
            const parentPane = invalidField.closest('.tab-pane');

            if (parentPane && !parentPane.classList.contains('active')) {
                const paneId = parentPane.getAttribute('id');
                const matchingTab = document.querySelector(`[data-bs-target="#${paneId}"]`);
                if (matchingTab) {
                    matchingTab.click(); // Triggers the tab click above
                    setTimeout(() => invalidField.focus(), 100);
                }
            }
        }, true);

        // Show loading spinner on valid submission
        form.addEventListener('submit', function () {
            if (form.checkValidity()) {
                const progressSpinner = document.querySelector('.submit-progress');
                if (progressSpinner) {
                    progressSpinner.classList.remove('d-none');
                }
            }
        });
    }

    function setupImagePreview(inputId, containerId, imageId, placeholderId = null, linkId = null) {
        const fileInput = document.getElementById(inputId);
        const container = document.getElementById(containerId);
        const image = document.getElementById(imageId);
        const placeholder = placeholderId ? document.getElementById(placeholderId) : null;
        const link = linkId ? document.getElementById(linkId) : null;

        if (!fileInput || !container || !image) return;

        fileInput.addEventListener("change", function (event) {
            const file = event.target.files[0];

            if (file && file.type.startsWith("image/")) {
                const reader = new FileReader();

                reader.onload = function (e) {
                    const resultUrl = e.target.result;

                    image.src = resultUrl;
                    if (link) {
                        link.href = resultUrl;
                    }

                    container.classList.remove("d-none");
                    if (placeholder) {
                        placeholder.classList.add("d-none");
                    }
                };

                reader.readAsDataURL(file);
            } else {
                image.src = "#";
                if (link) {
                    link.href = "#";
                }
                container.classList.add("d-none");
                if (placeholder) {
                    placeholder.classList.remove("d-none");
                }
            }
        });
    }

    // Initialize Preview for Create Form Nominee Photo
    setupImagePreview(
        "nomineeFileInput",
        "nomineePhotoPreviewContainer",
        "nomineePhotoPreview",
        "nomineePhotoIcon",
        "nomineePhotoLink"
    );
    // 2. Policy Document Preview (if applicable)
    setupImagePreview(
        "policyFileInput",
        "policyDocContainer",
        "policyDocImg",
        "policyDocIcon"
    );

    // 3. Claim Document Preview (if applicable)
    setupImagePreview(
        "claimFileInput",
        "claimDocContainer",
        "claimDocImg",
        "claimDocIcon"
    );

    const firstInput = document.querySelector(
        '#claimSubmitForm input:not([type="hidden"]):not([disabled]):not([readonly]), ' +
        '#claimSubmitForm select:not([disabled]), ' +
        '#claimSubmitForm textarea:not([disabled])'
    );

    if (firstInput) {
        firstInput.focus();
    }
});

$(document).ready(function () {
    /**
     * Reusable handler for form submission with jConfirm and loading states.
     * @param {string} formSelector - jQuery selector for the target form(s).
     */
    function initConfirmableForm(formSelector) {
        $(document).on('submit', formSelector, function (e) {
            var formElement = this;
            var $form = $(this);

            // 1. Native HTML5 form validation check
            if (!formElement.checkValidity()) {
                return;
            }

            // 2. Intercept submission if not yet confirmed
            if (!$form.data('confirmed')) {
                e.preventDefault();

                // Read dynamic attributes from the form (with fallbacks)
                var confirmTitle = $form.data('confirm-title') || 'Confirm Submission';
                var confirmMessage = $form.data('confirm-message') || 'Are you sure you want to proceed?';
                var submitBtnText = $form.data('submit-text') || 'Submit';
                var loadingText = $form.data('loading-text') || submitBtnText;
                var themeColor = $form.data('confirm-theme') || 'green';
                var btnColor = $form.data('btn-theme') || 'green';

                $.confirm({
                    title: confirmTitle,
                    content: confirmMessage,
                    type: themeColor,
                    typeAnimated: true,
                    icon: 'fa fa-check-circle',
                    buttons: {
                        confirm: {
                            text: '<i class="fa fa-check-circle me-1"></i> ' + submitBtnText,
                            btnClass: `btn-${btnColor}`,
                            action: function () {
                                var $submitBtn = $form.find('button[type="submit"]');

                                // Disable button and swap icon with spinning sync loader
                                $submitBtn
                                    .prop('disabled', true)
                                    .html('<i class="fas fa-sync fa-spin me-2"></i> ' + loadingText);

                                // Show progress indicator if present
                                $('.submit-progress').removeClass('hidden');

                                // Mark as confirmed and submit natively
                                $form.data('confirmed', true);
                                formElement.submit();
                            }
                        },
                        cancel: {
                            text: 'Cancel'
                        }
                    }
                });
            }
        });
    }

    // Attach to both Add and Edit forms by ID (or pass a common class like '.confirmable-form')
    initConfirmableForm('#claimSubmitForm, #editClaimForm, .confirmable-form');
});