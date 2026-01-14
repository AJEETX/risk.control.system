$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit",
            content: "Are you sure to save?",
            icon: 'fas fa-user-plus',

            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Save",
                    btnClass: 'btn-warning',
                    action: function () {
                        $("#edit-form").addClass("submit-progress-bg");

                        // Update UI with a short delay to show spinner
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        // Disable all buttons, submit inputs, and anchors
                        $('button, input[type="submit"], a').prop('disabled', true);

                        // Add a class to visually indicate disabled state for anchors
                        $('a').addClass('disabled-anchor').on('click', function (e) {
                            e.preventDefault(); // Prevent default action for anchor clicks
                        });
                        $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Save Profile");

                        form.submit();
                        if (form) {
                            const formElements = form.getElementsByTagName("*");
                            for (const element of formElements) {
                                element.disabled = true;
                                if (element.hasAttribute("readonly")) {
                                    element.classList.remove("valid", "is-valid", "valid-border");
                                    element.removeAttribute("aria-invalid");
                                }
                                if (element.classList.contains("filled-valid")) {
                                    element.classList.remove("filled-valid");
                                }
                            }
                        }
                    }
                },
                cancel: {
                    text: "Cancel",
                    btnClass: 'btn-default'
                }
            }
        });
    }
});
$(document).ready(function () {
    $("#edit-form").validate();
});