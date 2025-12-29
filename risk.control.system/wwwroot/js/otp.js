$.validator.setDefaults({
    submitHandler: function (form) {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#otp').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Send OTP');
        $('#login').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Login');

        $('#otp').attr('disabled', 'disabled');
        $('#login').attr('disabled', 'disabled');
        $('#otp').addClass('login-disabled');
        $('#login').addClass('login-disabled');
        $('html a').addClass('anchor-disabled');
        $('.text').addClass('anchor-disabled');

        form.submit();

        $('#login-form').attr('disabled', 'disabled');

        var loginForm = document.getElementById("login-form");
        if (loginForm) {
            var nodes = loginForm.getElementsByTagName('*');
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
    if (closeTermsButton) {
        // Close the modal when clicking the close button
        closeTermsButton.addEventListener('click', function () {
            termsModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
            termsModal.classList.remove('show'); // Remove the 'show' class to hide the modal
        });
    }

    // Optionally, you can close the modal if clicked outside the modal content
    window.addEventListener('click', function (e) {
        if (e.target === termsModal) {
            termsModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
            termsModal.classList.remove('show'); // Close the modal if clicked outside
        }
    });

});

$(document).ready(function () {

    validateNumber('#MobileNumber', /[^0-9]/g); // Allow numeric only no spaces
   
    $("#login-form").validate();
   
    $("#CountryIsd").autocomplete({
        source: function (request, response) {
            $("#loader").show(); // Show loader
            $.ajax({
                url: "/api/MasterData/GetIsdCode", // API endpoint for country suggestions
                type: "GET",
                data: {
                    term: request.term
                }, // Use the term entered by the user
                success: function (data) {
                    console.log(data); // Check what the server is sending
                    // Ensure data is in the format [{ label: "CountryName", value: "CountryCode", countryId: "CountryId", flag: "FlagImage" }]
                    response($.map(data, function (item) {
                        return {
                            label: item.label,  // This will be displayed as the text
                            value: item.isdCode,  // This will be used for the input value
                            flag: item.flag,   // Store the flag image URL
                            countryId: item.countryId  // Store the country ID
                        };
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
        autoFocus: true, // Automatically highlight the first item in the list
        select: function (event, ui) {
            // Set the selected value to the input field
            $(this).val('+' + ui.item.value);
            // Optionally, set the CountryId in a hidden input or elsewhere if needed
            $("#CountryIsd").val(ui.item.value);

            // Set the flag image based on the selected country
            $("#country-flag").attr("src", ui.item.flag); // Update the flag image source

            // Prevent the input field from allowing custom values
            return false; // This stops the input from being updated by the user directly
        },
        focus: function (event, ui) {
            // Prevent default focus behavior, to allow selecting only from the list
            return false; // This prevents the input field from updating when an item is highlighted
        },
        change: function (event, ui) {
            // If the value doesn't match any of the autocomplete suggestions, clear the input
            if (!ui.item) {
                $(this).val(''); // Optionally clear the input field
                $("#country-flag").attr("src", "/img/no-map.jpeg"); // Reset flag image to default
            }
        },
        messages: {
            noResults: "No results found",
            results: function (data) {
                return `${data} result${data > 1 ? "s" : ""} found`;
            }
        }
    });

    // Trigger autocomplete on focus for country code field
    $("#CountryIsd").on("focus", function () {
        const countryCodeValue = $(this).val();
        // If the field is empty, trigger autocomplete
        if (!countryCodeValue.trim()) {
            $(this).autocomplete("search", ""); // Trigger autocomplete with an empty search term
        }
    });

    $('input').on('focus', function () {
        $(this).select();
    });

    let timeLeft = 30;
    let countdown; // Move this variable here so it's accessible everywhere
    const timerElement = $('#timer');
    const resendBtn = $('#resendBtn');
    const loginForm = $('#login-form');

    function startTimer() {
        resendBtn.prop('disabled', true);
        // Clear any existing interval before starting a new one
        if (countdown) clearInterval(countdown);

        countdown = setInterval(function () {
            timeLeft--;
            timerElement.text(timeLeft);
            if (timeLeft <= 0) {
                clearInterval(countdown);
                resendBtn.prop('disabled', false);
                resendBtn.html('<span class="fa fa-sync"></span> Resend OTP');
            }
        }, 1000);
    }

    if (timerElement && resendBtn) {
        startTimer();
    }
    loginForm.on('submit', function () {
        clearInterval(countdown); // This stops the timer immediately
    });
    $('#resendBtn').click(function () {
        const isd = $('input[name="isd"]').val();
        const mobileNumber = $('input[name="mobileNumber"]').val();
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/Account/ResendOtp',
            type: 'POST',
            data: {
                isd: isd,
                mobileNumber: mobileNumber,
                __RequestVerificationToken: token
            },
            success: function (response) {
                if (response.success) {
                    alert(response.message);
                    timeLeft = 60; // Increase cooldown for next attempt
                    startTimer();
                }
            },
            error: function () {
                alert('Error resending OTP. Please try again later.');
            }
        });
    });
});

function validateNumber(selector, regex) {
    $(selector).on('input', function () {
        const value = $(this).val();
        // Remove invalid characters directly using the regex
        $(this).val(value.replace(regex, ''));
    });
}
function focusOtp() {
    const otp = document.getElementById("CountryIsd");
    if (otp) {
        otp.focus();
    }
    const verifyOtp = document.getElementById("userEnteredOtp");
    if (verifyOtp) {
        verifyOtp.focus();
    }
}

function onlyDigits(el) {
    el.value = el.value.replace(/[^0-9.]/g, '').replace(/(\..*)\./g, '$1');
}
window.onload = function () {
    focusOtp();
}
