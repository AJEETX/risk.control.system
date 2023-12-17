$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Create",
            content: "Are you sure to create?",
            columnClass: 'medium',
            icon: 'fas fa-building',
            type: 'green',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Create",
                    btnClass: 'btn-success',
                    action: function () {
                        askConfirmation = false;
                        form.submit();
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
});
function alphaOnly(event) {
    var key = event.keyCode;
    return ((key >= 65 && key <= 90) || key == 8);
};
AgreementDate.max = new Date().toISOString().split("T")[0];
$("input#emailAddress").on({
    keydown: function (e) {
        if (e.which === 32)
            return false;
    },
    change: function () {
        this.value = this.value.replace(/\s/g, "");
    }
});

function checkDomain() {
    var url = "/Account/CheckUserName";
    var name = $('#emailAddress').val();
    var domain = $('#domain').val();
    if (name) {
        $('#result').fadeOut(1000); // 1.5 seconds
        $('#result').fadeOut('slow'); // 1.5 seconds
        $.get(url, { input: name, domain: domain }, function (data) {
            if (data == 0) { //available
                $('#mailAddress').val($('#emailAddress').val());
                $('#domainName').val($('#domain').val());
                var mailDomain = $('#domain').val();
                $("#domainAddress").val(mailDomain);
                $("#result").html("<span style='color:green;padding-top:.5rem;'> <i class='fas fa-check' style='color:#298807'></i> </span>");
                $('#result').css('padding', '.5rem')
                $("#emailAddress").css('background-color', '');
                $("#emailAddress").css('border-color', '#ccc');
                $('#create-agency').prop('disabled', false);
                $('#result').fadeIn(1000); // 1.5 seconds
                $('#result').fadeIn('slow'); // 1.5 seconds
                $("#Name").focus();
            }
            else if (data == 1) {//domain exists
                $("#result").html("<span style='color:red;padding-top:.5rem;display:inline !important'><i class='fa fa-times-circle' style='color:red;'></i>  </span>");
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