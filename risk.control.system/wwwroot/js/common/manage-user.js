$(document).ready(function () {
    // Function to handle form submission with confirmation
    function handleFormSubmit(formSelector, formType) {
        var askConfirmation = true;
        $(formSelector).submit(function (e) {
            if (askConfirmation) {
                e.preventDefault();

                // Define the confirmation message based on form type
                var confirmationTitle = formType === 'create' ? "Confirm Add User" : "Confirm Edit User";
                var confirmationContent = formType === 'create' ? "Are you sure to add?" : "Are you sure to edit?";
                var buttonText = formType === 'create' ? "Add User" : "Edit User";
                var buttonClass = formType === 'create' ? 'btn-success' : 'btn-warning';
                var buttonIcon = formType === 'create' ? 'fas fa-user-plus' : 'fas fa-user-edit';

                $.confirm({
                    title: confirmationTitle,
                    content: confirmationContent,
                    icon: buttonIcon,
                    type: formType === 'create' ? 'green' : 'orange',
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: buttonText,
                            btnClass: buttonClass,
                            action: function () {
                                askConfirmation = false;
                                $("body").addClass("submit-progress-bg");

                                // Display loading spinner
                                setTimeout(function () {
                                    $(".submit-progress").removeClass("hidden");
                                }, 1);

                                // Disable the submit button and change text to show progress
                                var submitButton = $(formSelector).find('button[type="submit"]');
                                submitButton.attr('disabled', 'disabled');
                                submitButton.html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> " + buttonText);

                                // Disable all form inputs to prevent further edits
                                $(formSelector).find("input, select, button").prop("disabled", true);

                                // Submit the form after disabling inputs
                                $(formSelector).submit();
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
    }

    // Initialize both create and edit form submissions
    handleFormSubmit('#create-form', 'create');
    handleFormSubmit('#edit-form', 'edit');

    // Initialize form validation for both forms
    $("#create-form").validate();
    $("#edit-form").validate();
    $('input#emailAddress').on('input change', function () {
        if ($(this).val() != '' && $(this).val().length > 4) {
            $('#check-email').prop('disabled', false);
            $("#check-email").css('color', 'white');
            $("#check-email").css('background-color', '#004788');
            $("#check-email").css('cursor', 'default');
        } else {
            $('#check-email').prop('disabled', true);
            $("#check-email").css('color', '#ccc');
            $("#check-email").css('background-color', 'grey');
            $("#check-email").css('cursor', 'not-allowed');
        }
    });

    $('input#emailAddress').on('input focus', function () {
        if ($(this).val() != '' && $(this).val().length > 4) {
            $('#check-email').prop('disabled', false);
            $("#check-email").css('color', 'white');
            $("#check-email").css('background-color', '#004788');
            $("#check-email").css('cursor', 'default');
        } else {
            $('#create-agency').prop('disabled', 'true !important');
            $('#check-email').prop('disabled', true);
            $("#check-email").css('color', '#ccc');
            $("#check-email").css('background-color', 'grey');
            $("#check-email").css('cursor', 'not-allowed');
        }
    });

    $("input#emailAddress").on({
        keydown: function (e) {
            if (e.which === 32)
                return false;
        },
        change: function () {
            this.value = this.value.replace(/\s/g, "");
        }
    });
});

function alphaOnly(event) {
    var key = event.keyCode;
    return ((key >= 65 && key <= 90) || key == 8);
};

function checkUserEmail() {
    var url = "/Account/CheckUserEmail";
    var name = $('#emailAddress').val().toLowerCase();
    var emailSuffix = $('#emailSuffix').val().toLowerCase();
    if (name) {
        $.get(url, { input: name + '@' + emailSuffix }, function (data) {
            if (data == 0) { //available
                $('#mailAddress').val($('#emailAddress').val());
                $("#result").html("<span style='color:green;padding-top:.5rem;' title=' Available' data-toggle='tooltip'> <i class='fas fa-check' style='color:#298807'></i></span>");
                $('#result').css('padding', '.5rem')
                //$('#result').fadeOut(10000); // 1.5 seconds
                //$('#result').fadeOut('slow'); // 1.5 seconds
                $("#emailAddress").css('background-color', '');
                $("#emailAddress").css('border-color', '#ccc');
                $('#create').prop('disabled', false);
            }
            else if (data == 1) {//domain exists
                $("#result").html("<span style='color:red;padding-top:.5rem;display:inline !important' title=' Email exists' data-toggle='tooltip'><i class='fa fa-times-circle' style='color:red;'></i> </span>");
                $('#result').css('padding', '.5rem')
                $('#result').css('display', 'inline')
                $("#emailAddress").css('border-color', '#e97878');
                $('#create').prop('disabled', 'true !important');
            }
            else if (data = null || data == undefined) {
            }
        });
    }
}

function CheckIfEmailValid() {
    var name = $('#email').val();
    if (name && name.length > 4) {
        $('#check-email').prop('disabled', false);
        $("#check-email").css('color', 'white');
        $("#check-email").css('background-color', '#004788');
        $("#check-email").css('cursor', 'default');
    }
    else {
        $('#check-email').css('disabled', true);
        $("#check-email").css('color', '#ccc');
        $("#check-email").css('background-color', 'grey');
        $("#check-email").css('cursor', 'not-allowed');
    }
}

$('#emailAddress').focus();