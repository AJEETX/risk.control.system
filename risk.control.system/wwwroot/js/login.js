$.validator.setDefaults({
    submitHandler: function (form) {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#reset-pwd').addClass('login-disabled');

        $('#login').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Login');
        $('#reset-pwd').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Reset Password');

        $('a').attr('disabled', 'disabled');
        $('#login').attr('disabled', 'disabled');
        $('#login').addClass('login-disabled');
        $('html a').addClass('anchor-disabled');
        $('.text').addClass('anchor-disabled');

        $('#cancelConsent').attr('disabled', 'disabled');
        $('#cancelConsent').addClass('login-disabled');
        form.submit();

        $('#login-form').attr('disabled', 'disabled');
        $('#reset-form').attr('disabled', 'disabled');

        $('#email, .login-link').attr('disabled', 'disabled');
        $('#flip').attr('disabled', 'disabled');

        $('#resetemail').attr('disabled', 'disabled');

        $('#password').attr('disabled', 'disabled');

        var loginForm = document.getElementById("login-form");
        if (loginForm) {
            var nodes = loginForm.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
        
        var resetForm = document.getElementById("reset-form");
        if (resetForm) {
            var nodes = resetForm.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    }
});
document.addEventListener("DOMContentLoaded", function () {

    // Reference to the modal and close button
    var termsModal = document.getElementById('termsModal');
    var closeTermsButton = document.getElementById('closeterms');
    // Select all elements with the class 'termsLink'
    var termsLinks = document.querySelectorAll('.termsLink');

    // Add a click event listener to each element
    termsLinks.forEach(function (termsLink) {
        termsLink.addEventListener('click', function (e) {
            e.preventDefault(); // Prevent default link behavior (i.e., not navigating anywhere)

            // Show the terms modal
            var termsModal = document.querySelector('#termsModal');
            termsModal.classList.remove('hidden-section');
            termsModal.classList.add('show');
        });
    });

    // Close the modal when clicking the close button
    closeTermsButton.addEventListener('click', function () {
        termsModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
        termsModal.classList.remove('show'); // Remove the 'show' class to hide the modal
    });

    // Optionally, you can close the modal if clicked outside the modal content
    window.addEventListener('click', function (e) {
        if (e.target === termsModal) {
            termsModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
            termsModal.classList.remove('show'); // Close the modal if clicked outside
        }
    });

    // Bind event to "Login with OTP" link
    const otpLoginLink = document.getElementById('otp-login-link');
    if (otpLoginLink) {
        otpLoginLink.addEventListener('click', function () {
            showOtpSection();
        });
    }

    // Bind event to "Send OTP" button
    const sendOtpBtn = document.getElementById('send-otp-btn');
    if (sendOtpBtn) {
        sendOtpBtn.addEventListener('click', function () {
            sendOtp();
        });
    }

    // Bind event to "Verify OTP" button
    const verifyOtpBtn = document.getElementById('verify-otp-btn');
    if (verifyOtpBtn) {
        verifyOtpBtn.addEventListener('click', function () {
            verifyOtp();
        });
    }
});

$(document).ready(function () {

    $("#flip").change(function () {
        if ($(this).prop("checked")) {
            // If checkbox is checked (Forgot Password form)
            $("#resetemail").focus(); // Focus on resetemail input
        } else {
            // If checkbox is unchecked (Login form)
            $("#email").focus(); // Focus on email input
        }
    });

   
    $("#login-form").validate();
    $("#reset-form").validate();
    $("#email, #resetemail").autocomplete({
        source: function (request, response) {
            $("#loader").show(); // Show loader
            $.ajax({
                url: "/api/MasterData/GetUserBySearch",
                type: "GET",
                data: { search: request.term },
                success: function (data) {
                    // Ensure data is in the format [{ label: "email", value: "email" }]
                    response($.map(data, function (item) {
                        return { label: item, value: item };
                    }));
                    $("#loader").hide(); // Hide loader
                },
                error: function () {
                    response([]);
                    $("#loader").hide(); // Hide loader
                }
            });
        },
        minLength: 1, // Start showing suggestions after 1 character
        select: function (event, ui) {
            // Set the selected value to the input field
            $(this).val(ui.item.value);
        },
        messages: {
            noResults: "No results found",
            results: function (data) {
                return `${data} result${data > 1 ? "s" : ""} found`;
            }
        }
    });

    // Trigger autocomplete on focus for both fields
    $("#email, #resetemail").on("focus", function () {
        console.log("Focus triggered");
        const emailValue = $(this).val();

        // If the field is empty, trigger autocomplete
        if (!emailValue.trim()) {
            $(this).autocomplete("search", ""); // Trigger autocomplete with an empty search term
        }
    });


    $('input').on('focus', function () {
        $(this).select();
    });
});

function focusLogin() {
    const login = document.getElementById("email");
    if (login) {
        login.focus();
    }
}

function onlyDigits(el) {
    el.value = el.value.replace(/[^0-9.]/g, '').replace(/(\..*)\./g, '$1');
}
window.onload = function () {
    //initGeolocation();
    focusLogin();
}