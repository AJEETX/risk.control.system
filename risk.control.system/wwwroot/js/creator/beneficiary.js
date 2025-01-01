$(document).ready(function () {

    $('#BeneficiaryName, #CountryId, #StateId, #DistrictId, #Addressline').on('input', function () {
        const regex = /^[a-zA-Z ]*$/; // Allow alphabets and spaces
        const value = $(this).val();
        if (!regex.test(value)) {
            $(this).val(value.replace(/[^a-zA-Z ]/g, '')); // Remove invalid characters
        } 
    });

    let askCreateConfirmation = true;
    let askEditConfirmation = true;

    // Common function to show confirmation dialog
    function showConfirmationDialog(options, onConfirm) {
        $.confirm({
            title: options.title,
            content: options.content,
            icon: options.icon,
            type: options.type,
            closeIcon: true,
            buttons: {
                confirm: {
                    text: options.confirmText,
                    btnClass: options.confirmClass,
                    action: onConfirm
                },
                cancel: {
                    text: "Cancel",
                    btnClass: 'btn-default'
                }
            }
        });
    }

    // Common function to handle form submission
    function handleFormSubmit(e, formId, confirmationFlag, confirmationOptions) {
        if ($(`#${formId}`).valid() && confirmationFlag) {
            e.preventDefault();
            showConfirmationDialog(confirmationOptions, function () {
                // Disable further confirmation to avoid duplicate submissions
                if (formId === "create-form") askCreateConfirmation = false;
                if (formId === "edit-form") askEditConfirmation = false;

                // Add loading spinner and disable UI
                $("body").addClass("submit-progress-bg");
                setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);
                $(`#${formId} button[type=submit]`).html(
                    `<i class='fas fa-sync fa-spin' aria-hidden='true'></i> ${confirmationOptions.actionText}`
                );
                disableAllInteractiveElements();

                // Submit the form
                document.getElementById(formId).submit();

                // Disable all elements in the form
                const formElements = document.getElementById(formId).getElementsByTagName("*");
                for (const element of formElements) {
                    element.disabled = true;
                }
            });
        } else if (confirmationFlag) {
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
    }

    // Handle "Create" form submission
    $('#create-form').submit(function (e) {
        handleFormSubmit(e, "create-form", askCreateConfirmation, {
            title: "Confirm Add Beneficiary",
            content: "Are you sure to add?",
            icon: 'fas fa-user-tie',
            type: 'green',
            confirmText: "Add Beneficiary",
            confirmClass: 'btn-success',
            actionText: "Add Beneficiary"
        });
    });

    // Handle "Edit" form submission
    $('#edit-form').submit(function (e) {
        handleFormSubmit(e, "edit-form", askEditConfirmation, {
            title: "Confirm Edit Beneficiary",
            content: "Are you sure to edit?",
            icon: 'fas fa-user-tie',
            type: 'orange',
            confirmText: "Edit Beneficiary",
            confirmClass: 'btn-warning',
            actionText: "Edit Beneficiary"
        });
    });

    // Initialize form validations
    $("#create-form").validate();
    $("#edit-form").validate();

    // Automatically set focus to the Beneficiary Name input field
    $("#BeneficiaryName").focus();
});
