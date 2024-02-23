$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Add New",
            columnClass: 'medium',
            content: "Are you sure to add ?",
            icon: 'fas fa-user-plus',
            type: 'green',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Add New",
                    btnClass: 'btn-success',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('#create-user').attr('disabled', 'disabled');
                        $('#create-user').html("<i class='fas fa-spinner' aria-hidden='true'></i> Add User");

                        form.submit();
                        var nodes = document.getElementById("create-form").getElementsByTagName('*');
                        for (var i = 0; i < nodes.length; i++) {
                            nodes[i].disabled = true;
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
    $("#create-form").validate();

    $('input#emailAddress').on('input change', function () {
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
        $('#result').fadeOut(1000); // 1.5 seconds
        $('#result').fadeOut('slow'); // 1.5 seconds
        $.get(url, { input: name + '@' + emailSuffix }, function (data) {
            if (data == 0) { //available
                $('#mailAddress').val($('#emailAddress').val());
                $("#result").html("<span style='color:green;padding-top:.5rem;'> <i class='fas fa-check' style='color:#298807'></i> </span>");
                $('#result').css('padding', '.5rem')

                $("#emailAddress").css('background-color', '');
                $("#emailAddress").css('border-color', '#ccc');
                $('#create-agency').prop('disabled', false);
                $('#result').fadeIn(1000); // 1.5 seconds
                $('#result').fadeIn('slow'); // 1.5 seconds
            }
            else if (data == 1) {//domain exists
                $("#result").html("<span style='color:red;padding-top:.5rem;display:inline !important'><i class='fa fa-times-circle' style='color:red;'></i> </span>");
                $('#result').css('padding', '.5rem')
                $('#result').css('display', 'inline')
                $("#emailAddress").css('border-color', '#e97878');
                $('#create-agency').prop('disabled', 'true !important');
                $('#result').fadeIn(1000); // 1.5 seconds
                $('#result').fadeIn('slow'); // 1.5 seconds
            }
            else if (data = null || data == undefined) {
            }
        });
    }
}

function CheckIfEmailValid() {
    $('#result').fadeOut(1000); // 1.5 seconds
    $('#result').fadeOut('slow'); // 1.5 seconds
    var name = $('#emailAddress').val();
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
    $('#result').fadeIn(1000); // 1.5 seconds
    $('#result').fadeIn('slow'); // 1.5 seconds
}
$('#emailAddress').focus();