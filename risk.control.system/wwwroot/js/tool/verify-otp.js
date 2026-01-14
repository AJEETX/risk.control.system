$(document).ready(function () {
    const inputs = $('.otp-input');
    const UserEnteredOtp = $('#UserEnteredOtp');
    const resendBtn = $('#resendBtn');
    const timerElement = $('#timer');

    // 1. Form Validation & Submission
    $("#login-form").validate({
        submitHandler: function (form) {
            $("body").addClass("submit-progress-bg");
            $(".submit-progress").removeClass("hidden");
            $('#login').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Login');
            $('#login').attr('disabled', 'disabled').addClass('login-disabled');
            form.submit();
        }
    });

    // 2. OTP Input Logic (Auto-focus & Join)
    inputs.on('input', function (e) {
        const val = $(e.target).val().replace(/[^0-9]/g, '');
        $(e.target).val(val);

        if (val !== "") {
            $(e.target).next('.otp-input').focus();
        }
        updateHiddenInput();
    });

    inputs.on('keydown', function (e) {
        if (e.key === 'Backspace' && $(e.target).val() === '') {
            $(e.target).prev('.otp-input').focus();
        }
    });

    function updateHiddenInput() {
        let otp = "";
        inputs.each(function () { otp += $(this).val(); });
        UserEnteredOtp.val(otp);
    }

    // 3. Timer Logic
    let timeLeft = 30;
    function startTimer() {
        resendBtn.prop('disabled', true);
        let countdown = setInterval(function () {
            timeLeft--;
            timerElement.text(timeLeft);
            if (timeLeft <= 0) {
                clearInterval(countdown);
                resendBtn.prop('disabled', false).html('<span class="fa fa-sync"></span> Resend OTP');
            }
        }, 1000);
    }
    startTimer();

    // 4. Resend OTP Ajax
    // In verify-otp.js
    resendBtn.click(function () {
        const data = {
            CountryIsd: $('input[name="CountryIsd"]').val(),
            MobileNumber: $('input[name="MobileNumber"]').val(),
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        };

        $.post('/Tool/ResendOtp', data)
            .done(function (response) {
                // This runs if the SERVER responds (even if response.success is false)
                if (response.success) {
                    timeLeft = 60;
                    startTimer();
                    inputs.val('').first().focus();

                    $.alert({
                        title: 'Success',
                        content: response.message,
                        type: 'green',
                        icon: 'fas fa-check-circle'
                    });
                } else {
                    $.alert({
                        title: 'Failed',
                        content: response.message,
                        type: 'orange',
                        icon: 'fas fa-exclamation-triangle'
                    });
                }
            })
            .fail(function (xhr, status, error) {
                // This runs if the REQUEST fails (e.g., 500 Error, Network down)
                console.error("Error: " + error);
                $.alert({
                    title: 'System Error',
                    content: 'Something went wrong on the server. Please try again later.',
                    type: 'red',
                    icon: 'fas fa-bug'
                });
            });
    });

    inputs.first().focus();
});