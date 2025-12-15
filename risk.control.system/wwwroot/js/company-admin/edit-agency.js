$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit",
            content: "Are you sure to save?",

            icon: 'fas fa-building',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Save",
                    btnClass: 'btn-warning',
                    action: function () {
                        askConfirmation = false;
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('#edit').attr('disabled', 'disabled');
                        $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Save Agency");

                        form.submit();
                        var createForm = document.getElementById("edit-form");
                        if (createForm) {
                            const formElements = createForm.getElementsByTagName("*");
                            for (const element of formElements) {
                                element.disabled = true;
                                if (element.hasAttribute("readonly")) {
                                    element.classList.remove("valid", "is-valid", "valid-border");
                                    element.removeAttribute("aria-invalid");
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
    $('#Name').focus();

    $("#edit-form").validate();   
});