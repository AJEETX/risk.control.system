$(document).ready(function () {
    $("#InsuranceType").focus();

    // Common form submission handler to avoid duplication
    function handleFormSubmission(formId, buttonId, confirmationText, confirmCallback) {
        let askConfirmation = true;

        $(formId).submit(function (e) {
            if ($(formId).valid() && askConfirmation) {
                e.preventDefault();
                $.confirm({
                    title: confirmationText.title,
                    content: confirmationText.content,
                    icon: confirmationText.icon,
                    type: confirmationText.type,
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: confirmationText.confirmButtonText,
                            btnClass: confirmationText.buttonClass,
                            action: function () {
                                askConfirmation = false;
                                $("body").addClass("submit-progress-bg");

                                // Update UI with a short delay to show spinner
                                setTimeout(function () {
                                    $(".submit-progress").removeClass("hidden");
                                }, 1);

                                // Disable all buttons, submit inputs, and anchors
                                $('button, input[type="submit"], a').prop('disabled', true);
                                $('a').addClass('disabled-anchor');

                                // Change button to show loading spinner
                                $(buttonId).html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> " + confirmationText.buttonText);

                                // Disable form inputs
                                disableFormElements(formId);

                                // Submit the form
                                $(formId).submit();
                            }
                        },
                        cancel: {
                            text: "Cancel",
                            btnClass: 'btn-default'
                        }
                    }
                });
            } else if (askConfirmation) {
                // Show validation error alert if form is invalid
                e.preventDefault();
                $.alert({
                    title: "Form Validation Error",
                    content: "Please fill in all required fields correctly.",
                    icon: 'fas fa-exclamation-triangle',
                    type: 'red',
                    closeIcon: true,
                    buttons: {
                        ok: {
                            text: "OK",
                            btnClass: 'btn-danger'
                        }
                    }
                });
            }
        });
    }

    // Function to disable all form elements
    function disableFormElements(formId) {
        const form = document.getElementById(formId);
        if (form) {
            const nodes = form.getElementsByTagName('*');
            for (let i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    }

    // Apply the handler for the create form
    handleFormSubmission(
        '#create-form',
        '#create',
        {
            title: "Confirm Add Service",
            content: "Are you sure to add?",
            icon: 'fas fa-truck fa-sync',
            type: 'green',
            confirmButtonText: "Add Service",
            buttonText: "Add Service",
            buttonClass: "btn-success"
        }
    );

    // Apply the handler for the edit form
    handleFormSubmission(
        '#edit-form',
        '#edit',
        {
            title: "Confirm Edit Service",
            content: "Are you sure to save?",
            icon: 'fas fa-truck fa-sync',
            type: 'orange',
            confirmButtonText: "Save Service",
            buttonText: "Save Service",
            buttonClass: "btn-warning"
        }
    );

    // Initialize validation
    $("#create-form").validate();
    $("#edit-form").validate();
});
