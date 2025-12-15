$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm  Edit Company",
            content: "Are you sure to save?",
            icon: 'fas fa-building',

            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Save Company",
                    btnClass: 'btn-warning',
                    action: function () {

                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('.btn.btn-warning').attr('disabled', 'disabled');
                        $('#edit.btn.btn-warning').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Save Company");

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
const ExpiryDate = document.getElementById("ExpiryDate");
if (ExpiryDate) {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1); // Add 1 day to the current date
    ExpiryDate.min = tomorrow.toISOString().split("T")[0];
}
