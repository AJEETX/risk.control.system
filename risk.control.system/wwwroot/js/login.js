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
        $('#reset-pwd').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Get');

        $('input').attr('disabled', 'disabled');
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
        $('.login-link').addClass('anchor-disabled');

        $('#resetemail').attr('disabled', 'disabled');

        $('#Password').attr('disabled', 'disabled');

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

    // Add event listeners to all elements with class `toggle-password-visibility`
    const toggleButtons = document.querySelectorAll('.toggle-password-visibility');

    toggleButtons.forEach(function (button) {
        button.addEventListener('click', function () {
            // Get the target input field ID from the `data-target` attribute
            const passwordFieldId = button.getAttribute('data-target');
            const passwordField = document.getElementById(passwordFieldId);
            const eyeIcon = button.querySelector('i');

            // Toggle password visibility
            if (passwordField.type === "password") {
                passwordField.type = "text";
                eyeIcon.classList.remove("fa-eye-slash");
                eyeIcon.classList.add("fa-eye");
            } else {
                passwordField.type = "password";
                eyeIcon.classList.remove("fa-eye");
                eyeIcon.classList.add("fa-eye-slash");
            }
        });
    });
});

$(document).ready(function () {

    validateMobile('#mobile', /[^0-9]/g); // Allow numeric only no spaces
    $('#mobile')
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
    $("#CountryId").autocomplete({
        source: function (request, response) {
            $("#loader").show(); // Show loader
            $.ajax({
                url: "/api/Company/GetCountryIsdCode", // API endpoint for country suggestions
                type: "GET",
                data: { term: request.term }, // Use the term entered by the user
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
            $("#CountryId").val(ui.item.value);

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
    $("#CountryId").on("focus", function () {
        const countryCodeValue = $(this).val();
        // If the field is empty, trigger autocomplete
        if (!countryCodeValue.trim()) {
            $(this).autocomplete("search", ""); // Trigger autocomplete with an empty search term
        }
    });


    $('input').on('focus', function () {
        $(this).select();
    });
});

function validateMobile(selector, regex) {
    $(selector).on('input', function () {
        const value = $(this).val();
        // Remove invalid characters directly using the regex
        $(this).val(value.replace(regex, ''));
    });
}
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