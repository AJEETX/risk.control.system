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
                content: "Are you sure to edit?",

                icon: 'fas fa-user-plus',
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Edit User",
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
                            $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit User");

                            $('#edit-form').submit();
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
                //$('#create').prop('disabled', false);
            }
            else if (data == 1) {//domain exists
                $("#result").html("<span style='color:red;padding-top:.5rem;display:inline !important' title=' Email exists' data-toggle='tooltip'><i class='fa fa-times-circle' style='color:red;'></i> </span>");
                $('#result').css('padding', '.5rem')
                $('#result').css('display', 'inline')
                $("#emailAddress").css('border-color', '#e97878');
                //$('#create').prop('disabled', 'true !important');
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