$(document).ready(function () {
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
            title: "Confirm Add Case",
            content: "Are you sure to add?",
            icon: 'far fa-file-powerpoint',
            type: 'green',
            confirmText: "Add Case",
            confirmClass: 'btn-success',
            actionText: "Add Case"
        });
    });

    // Handle "Edit" form submission
    $('#edit-form').submit(function (e) {
        handleFormSubmit(e, "edit-form", askEditConfirmation, {
            title: "Confirm Edit Case",
            content: "Are you sure to save?",
            icon: 'far fa-file-powerpoint',
            type: 'orange',
            confirmText: "Save Case",
            confirmClass: 'btn-warning',
            actionText: "Save Case"
        });
    });

    // Initialize form validations
    $("#create-form").validate();
    $("#edit-form").validate();


    // Automatically set focus
    $("#ContractNumber").focus();

    // Set the max date for customer date input
    const maxDate = new Date().toISOString().split("T")[0];
    $("#DateOfBirth").attr("max", maxDate);

    // Set max dates for contract and incident dates
    $("#ContractIssueDate, #DateOfIncident").attr("max", maxDate);
    $("#InsuranceType").on("change", function () {
        // Clear and reset InvestigationServiceTypeId dropdown
        $('#InvestigationServiceTypeId').empty();
        $('#InvestigationServiceTypeId').append("<option value=''>--- SELECT ---</option>");

        var value = $(this).val();

        if (value != '') {
            var token = $('input[name="icheckifyAntiforgery"]').val();

            $.ajax({
                url: "/api/MasterData/GetInvestigationServicesByInsuranceType",
                type: "GET",
                data: {
                    InsuranceType: value
                },
                headers: {
                    "Content-Type": "application/json",
                    "X-CSRF-TOKEN": token
                },
                success: function (data) {
                    PopulateInvestigationServices("#InvestigationServiceTypeId", data);
                },
                error: function (xhr) {
                    console.error("Error loading services:", xhr);
                }
            });
        }
    });
});
function PopulateInvestigationServices(dropDownId, list) {
    $.each(list, function (index, row) {
        $(dropDownId).append("<option value='" + row.investigationServiceTypeId + "'>" + row.name + "</option>")
    });
}
