
$(document).ready(function () {

    var askConfirmation = true;

    $('#create-form').submit(function (e) {
        // Validate the form before showing the confirmation prompt
        if ($("#create-form").valid() && askConfirmation) {
            e.preventDefault(); // Prevent form submission

            $.confirm({
                title: "Confirm Add Agency",
                content: "Are you sure to add?",

                icon: 'fas fa-building',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Add Agency",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            // Disable all buttons, submit inputs, and anchors
                            $('button, input[type="submit"], a').prop('disabled', true);

                            // Add a class to visually indicate disabled state for anchors
                            $('a').addClass('disabled-anchor').on('click', function (e) {
                                e.preventDefault(); // Prevent default action for anchor clicks
                            });
                            $('#create').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Agency");

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
            // If the form is not valid, prevent form submission and show a validation error
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

    $('input#emailAddress').on('input change focus blur', function () {
        if ($(this).val() !== '' && $(this).val().length > 4) {
            $('#check-domain').prop('disabled', false).removeClass('disabled-btn').addClass('enabled-btn');
        } else {
            $('#create').prop('disabled', true);
            $('#check-domain').prop('disabled', true).removeClass('enabled-btn').addClass('disabled-btn');
        }
    });
    const $emailInput = $("#emailAddress");

    // Handle blur event
    if ($emailInput) {
        $emailInput.on("blur", function () {

            if ($(this).val()) {
                CheckIfEmailValid($(this).val());
            }
        });

        // Handle keydown event
        $emailInput.on("keydown", function (event) {
            return alphaOnly(event);
        });

        // Handle click event
        $emailInput.on("click", function () {
            $(this).select();
        });
    }
    $('#check-domain').on('click', function () {
        checkDomain();
    });
});
//ActivatedDate.max = new Date().toISOString().split("T")[0];
function CheckIfEmailValid() {
    var name = $('#emailAddress').val();
    if (name && name.length > 4) {
        $('#check-email').prop('disabled', false);
    }
    else {
        $('#check-email').css('disabled', true);
    }
}

// Example function: Allow only alphabetical characters
function alphaOnly(event) {
    const key = event.key;
    if (!/^[a-zA-Z]$/.test(key) && key !== "Backspace" && key !== "Tab") {
        event.preventDefault();
        return false;
    }
    return true;
}
function checkDomain() {
    $("#domainAddress").val('');
    $('#mailAddress').val('');

    var url = "/Account/CheckAgencyName";
    var name = $('#emailAddress').val().toLowerCase();
    var domain = $('#domain').val().toLowerCase();
    if (name) {
        $('#result').fadeOut(1000); // 1.5 seconds
        $('#result').fadeOut('slow'); // 1.5 seconds
        $.get(url, { input: name, domain: domain }, function (data) {
            if (data == 0) { //available
                $('#mailAddress').val($('#emailAddress').val());
                $('#domainName').val($('#domain').val());
                var mailDomain = $('#domain').val();
                $("#domainAddress").val(mailDomain);
                $("#result").html("<span class='available' title='Available' data-bs-toggle='tooltip'> <i class='fas fa-check'></i></span>");
                $('#result').addClass('result-padding');
                $("#emailAddress").removeClass('error-border');
                $('#result').fadeIn(1000); // 1.5 seconds
                $('#result').fadeIn('slow'); // 1.5 seconds
            }
            else if (data == 1) {//domain exists
                $("#domainAddress").val('');
                $('#mailAddress').val('');
                $("#result").html("<span class='unavailable' title='Domain exists' data-bs-toggle='tooltip'><i class='fa fa-times-circle'></i></span>");
                $('#result').addClass('result-padding');
                $("#emailAddress").addClass('error-border');
                $('#result').fadeIn(1000); // 1.5 seconds
                $('#result').fadeIn('slow'); // 1.5 seconds
            }
            else if (data = null || data == undefined) {
                $("#domainAddress").val('');
                $('#mailAddress').val('');
            }
        });
    }
}

$('#emailAddress').focus();