$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Update Password",
            content: "Remeber the new password.",
            icon: 'fa fa-key',

            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Update",
                    btnClass: 'btn-warning',
                    action: function () {
                        askConfirmation = false;
                        $("body").addClass("submit-progress-bg");

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
                        $('#updatebutton').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Update Password");
                        form.submit();
                        var createForm = document.getElementById("edit-form");
                        if (createForm) {
                            var nodes = createForm.getElementsByTagName('*');
                            for (var i = 0; i < nodes.length; i++) {
                                nodes[i].disabled = true;
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

    const currentpassword = $('#NewPassword');
    if (currentpassword) {
        currentpassword.focus()
    }

    $('#editButton').on('click', function (e) {
        $("#edit-form").addClass("submit-progress-bg");

        // Update UI with a short delay to show spinner
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#editButton').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> User Profile");
    });
    $("#edit-form").validate();

});
document.addEventListener('DOMContentLoaded', function () {
    // Add event listeners to all elements with class `toggle-password-visibility`
    const toggleButtons = document.querySelectorAll('.toggle-password-visibility');

    toggleButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            // Get the target input field ID from the `data-target` attribute
            const passwordFieldId = button.getAttribute('data-target');
            const passwordField = document.getElementById(passwordFieldId);
            const eyeIcon = button.querySelector('i');

            // Toggle password visibility
            if (passwordField.type === "password") {
                passwordField.type = "text";
                eyeIcon.classList.remove("fa-eye-slash");
                eyeIcon.classList.add("fa-eye");
            } else {
                passwordField.type = "password";
                eyeIcon.classList.remove("fa-eye");
                eyeIcon.classList.add("fa-eye-slash");
            }
        });
    });
});