
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

    $('input#emailAddress').on('input change', function () {
        if ($(this).val() != '' && $(this).val().length > 4) {
            $('#check-email').prop('disabled', false);
            $("#check-email").css('color', 'white');
            $("#check-email").css('background-color', '#004788');
            $("#check-email").css('cursor', 'default');
        } else {
            //$('#create-agency').prop('disabled', 'true !important');
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
            //$('#create-agency').prop('disabled', 'true !important');
            $('#check-email').prop('disabled', true);
            $("#check-email").css('color', '#ccc');
            $("#check-email").css('background-color', 'grey');
            $("#check-email").css('cursor', 'not-allowed');
        }
    });
});
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


$('#emailAddress').focus();