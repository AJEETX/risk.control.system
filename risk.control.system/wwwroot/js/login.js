$.validator.setDefaults({
    submitHandler: function (form) {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#login').css('color', 'lightgrey');
        $('#reset-pwd').css('color', 'lightgrey');

        $('#login').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Login');
        $('#reset-pwd').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Reset Password');

        $('a').attr('disabled', 'disabled');
        $('button').attr('disabled', 'disabled');
        $('html button').css('pointer-events', 'none')
        $('html a').css({ 'pointer-events': 'none' }, { 'cursor': 'none' })
        $('.text').css({ 'pointer-events': 'none' }, { 'cursor': 'none' })

        form.submit();

        $('#login-form').attr('disabled', 'disabled');
        $('#reset-form').attr('disabled', 'disabled');

        $('#email, .login-link').attr('disabled', 'disabled');
        $('#flip').attr('disabled', 'disabled');

        $('#resetemail').attr('disabled', 'disabled');

        $('#password').attr('disabled', 'disabled');

        

        var nodes = document.getElementById("login-form").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
        var nodes = document.getElementById("reset-form").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
});

function focusLogin() {

    const login = document.getElementById("email");
    if (login) {
        login.focus();
    }
}

// Function to handle successful geolocation retrieval
function handleGeolocationSuccess(position) {
    const latlong = `${position.coords.latitude},${position.coords.longitude}`;
    fetchIpInfo(latlong); // Call fetchIpInfo with geolocation data
}

// Function to handle geolocation errors
function handleGeolocationError(err) {
    console.error('Geolocation request failed or was denied:', err.message);
    fetchIpInfo(); // Optionally fetch IP info without geolocation
}

// Function to fetch IP info and update the UI
async function fetchIpInfo(latlong = '') {
    try {
        // Construct the URL with or without latlong
        const url = `/api/Notification/GetClientIp?url=${encodeURIComponent(window.location.pathname)}${latlong ? `&latlong=${encodeURIComponent(latlong)}` : ''}`;

        // Make the fetch request
        const response = await fetch(url);

        // Handle non-OK responses
        if (!response.ok) {
            console.error(`IP fetch failed with status: ${response.status}`);
            updateInfoDisplay('---', '---');
            return;
        }

        // Parse and process the JSON response
        const data = await response.json();
        const district = data.district || 'Not available';
        updateInfoDisplay(district, district);
    } catch (error) {
        console.error('Error during IP info fetch operation:', error.message);
        updateInfoDisplay('Not available', 'Not available');
    }
}

// Function to update the UI with the fetched data
function updateInfoDisplay(ipAddress, ipAddress1) {
    document.querySelector('#ipAddress .info-data').textContent = ipAddress;
    document.querySelector('#ipAddress1 .info-data').textContent = ipAddress1;
}

// Initialize geolocation handling
function initGeolocation() {
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(handleGeolocationSuccess, handleGeolocationError);
    } else {
        console.error('Geolocation is not supported by this browser.');
        fetchIpInfo(); // Optionally fetch IP info without geolocation
    }
}

// Call the initialization function

$(document).ready(function () {
    $("#login-form").validate();
    $("#reset-form").validate();
    $("#email").autocomplete({
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
        minLength: 0, // Start showing suggestions after 1 character
        select: function (event, ui) {
            // Set the selected value to the input field
            $("#email").val(ui.item.value);
        },
        messages: {
            noResults: "No results found",
            results: function (amount) {
                return `${amount} result${amount > 1 ? "s" : ""} found`;
            }
        }
    });
    $("#email").on("focus", function () {
        console.log("Focus triggered");
        const emailValue = $(this).val();
        if (!emailValue && emailValue.trim() ==="") {
            $(this).autocomplete("search", ""); // Trigger autocomplete with an empty search term
        }
    });

    $('input').on('focus', function () {
        $(this).select();
    });
});

function onlyDigits(el) {
    el.value = el.value.replace(/[^0-9.]/g, '').replace(/(\..*)\./g, '$1');
}
window.onload = function () {
    initGeolocation();

    focusLogin();
}