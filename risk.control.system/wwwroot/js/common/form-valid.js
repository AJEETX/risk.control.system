$(document).ready(function () {
    const $form = $('form');
    if (!$form.length) return;

    const $submitBtn = $form.find('button[type="submit"]');

    function validateForm() {
        const isFormValid = $form[0].checkValidity();
        const hasCustomErrors = $form.find('.is-invalid').length > 0;

        $submitBtn.prop('disabled', !(isFormValid && !hasCustomErrors));
    }

    // Use 'input' for real-time button toggling (no red colors yet)
    $form.on('input change', 'input, select, textarea', function () {
        updateFieldStatus($(this), false); // Validate but don't force 'is-invalid' visually yet
        validateForm();
    });

    // Use 'blur' to show errors, but SKIP if the user is trying to leave/cancel
    $form.on('blur', 'input, select, textarea', function (e) {
        const $field = $(this);

        // Short delay to see where the focus went
        setTimeout(() => {
            const activeEl = document.activeElement;
            // If the user clicked a button with "btn-secondary" or id "back", don't show red errors
            if (activeEl && (activeEl.id === 'back' || $(activeEl).hasClass('btn-secondary'))) {
                return;
            }
            updateFieldStatus($field, true);
            validateForm();
        }, 100);
    });

    function updateFieldStatus($field, showVisualErrors) {
        const el = $field[0];
        if (el.checkValidity()) {
            $field.removeClass('is-invalid').addClass('is-valid');
        } else if (showVisualErrors) {
            // Only add is-invalid if we explicitly want to show errors (on blur)
            $field.addClass('is-invalid').removeClass('is-valid');
        }
    }

    validateForm();
});