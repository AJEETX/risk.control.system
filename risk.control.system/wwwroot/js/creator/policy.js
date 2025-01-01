$(document).ready(function () {
    var askConfirmation = true;
    var askEditConfirmation = true;

    // Common functions
    function showConfirmation(formId, title, content, type, buttonText, buttonClass, successCallback) {
        $.confirm({
            title: title,
            content: content,
            icon: 'far fa-file-powerpoint',
            type: type,
            closeIcon: true,
            buttons: {
                confirm: {
                    text: buttonText,
                    btnClass: buttonClass,
                    action: successCallback
                },
                cancel: {
                    text: "Cancel",
                    btnClass: 'btn-default'
                }
            }
        });
    }

    function handleFormSubmit(e, formId, confirmationFlag, confirmationDetails) {
        if ($(`#${formId}`).valid() && confirmationFlag) {
            e.preventDefault();
            showConfirmation(
                formId,
                confirmationDetails.title,
                confirmationDetails.content,
                confirmationDetails.type,
                confirmationDetails.buttonText,
                confirmationDetails.buttonClass,
                function () {
                    // Disable confirmation to prevent duplicate prompts
                    if (formId === "create-form") askConfirmation = false;
                    if (formId === "edit-form") askEditConfirmation = false;

                    // Add a loading animation
                    $("body").addClass("submit-progress-bg");
                    setTimeout(function () {
                        $(".submit-progress").removeClass("hidden");
                    }, 1);

                    // Update button text
                    $(`#${formId} button[type=submit]`).html(
                        `<i class='fas fa-sync fa-spin' aria-hidden='true'></i> ${confirmationDetails.buttonText}`
                    );
                    disableAllInteractiveElements();

                    // Submit the form
                    $(`#${formId}`)[0].submit();
                    // Disable all elements in the form
                    const formElements = document.getElementById(formId).getElementsByTagName("*");
                    for (const element of formElements) {
                        element.disabled = true;
                    }
                }
            );
        } else if (confirmationFlag) {
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

    // Handle form submissions
    $('#create-form').submit(function (e) {
        handleFormSubmit(e, "create-form", askConfirmation, {
            title: "Confirm Add Policy",
            content: "Are you sure to add?",
            type: "green",
            buttonText: "Add Policy",
            buttonClass: "btn-success"
        });
    });

    $('#edit-form').submit(function (e) {
        handleFormSubmit(e, "edit-form", askEditConfirmation, {
            title: "Confirm Edit Policy",
            content: "Are you sure to edit?",
            type: "orange",
            buttonText: "Edit Policy",
            buttonClass: "btn-warning"
        });
    });

    // Initialize form validations
    $("#create-form").validate();
    $("#edit-form").validate();

    // Automatically set focus
    $("#ContractNumber").focus();

    // Handle add policy click
    $('#create-policy').on('click', function () {
        $("body").addClass("submit-progress-bg");
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#create-policy').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Policy");
        disableAllInteractiveElements();

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    // Set max dates for contract and incident dates
    var maxDate = new Date().toISOString().split("T")[0];
    $("#dateContractId, #dateIncidentId").attr("max", maxDate);
});
