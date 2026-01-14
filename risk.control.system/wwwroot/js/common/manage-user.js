$(document).ready(function () {

    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if ($('#create-form').valid() &&  askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Add User",
                content: "Are you sure to add?",
                icon: 'fas fa-user-plus',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Add User",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#create').attr('disabled', 'disabled');
                            $('#create').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i>  Add User");

                            $('#create-form').submit();
                            var createForm = document.getElementById("create-form");
                            if (createForm) {
                                const formElements = createForm.getElementsByTagName("*");
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
        } else if (askConfirmation) {
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

    var askEditConfirmation = true;
    $('#edit-form').submit(function (e) {
        if ($('#edit-form').valid() && askEditConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit User",
                content: "Are you sure to save?",

                icon: 'fas fa-user-plus',
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Save User",
                        btnClass: 'btn-warning',
                        action: function () {
                            askEditConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#edit').attr('disabled', 'disabled');
                            $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Save User");

                            $('#edit-form').submit();
                            var createForm = document.getElementById("edit-form");
                            if (createForm) {
                                const formElements = createForm.getElementsByTagName("*");
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
        } else if (askEditConfirmation) {
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

    $("#create-form").validate();
    $("#edit-form").validate();

    $('input#emailAddress').on('input change focus blur', function () {
        if ($(this).val() !== '' && $(this).val().length > 4) {
            $('#check-email').prop('disabled', false).removeClass('disabled-btn').addClass('enabled-btn');
        } else {
            $('#create').prop('disabled', true);
            $('#check-email').prop('disabled', true).removeClass('enabled-btn').addClass('disabled-btn');
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
    $('#check-email').on('click', function () {
        checkUserEmail();
    });
});

function alphaOnly(event) {
    var key = event.keyCode;
    return ((key >= 65 && key <= 90) || key == 8);
};

function checkUserEmail() {
    var url = "/api/MasterData/CheckUserEmail";
    var name = $('#emailAddress').val().toLowerCase();
    var emailSuffix = $('#emailSuffix').val().toLowerCase();
    $('#mailAddress').val('');
    if (name) {
        $.get(url, { input: name + '@' + emailSuffix }, function (data) {
            if (data === 0) { //available
                $('#mailAddress').val($('#emailAddress').val());
                $("#result").html("<span class='available' data-toggle='tooltip' title='Available' data-toggle='tooltip'> <i class='fas fa-check'></i></span>");
                $('#result').addClass('result-padding');
                $("#emailAddress").removeClass('error-border');
            }
            else if (data === 1) { //domain exists
                $('#mailAddress').val('');
                $("#result").html("<span class='unavailable' data-toggle='tooltip' title='Email exists' data-toggle='tooltip'><i class='fa fa-times-circle'></i></span>");
                $('#result').addClass('result-padding');
                $("#emailAddress").addClass('error-border');
            }

            else if (data = null || data == undefined) {
                $('#mailAddress').val('');
            }
        });
    }
}

function CheckIfEmailValid() {
    var name = $('#email').val();
    if (name && name.length > 4) {
        $('#check-email').prop('disabled', false);
    }
    else {
        $('#check-email').css('disabled', true);
    }
}

$('#emailAddress').focus();