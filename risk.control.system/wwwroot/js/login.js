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
    const showUsers = document.getElementById("show-users").value;
    if (showUsers == 'true') {
        const login = document.getElementById("email");
        login.focus();
    }
    else {
        const pwd = document.getElementById("password");
        pwd.select();
    }

}

// Success function to handle geolocation success
function success(position) {
    var lat = position.coords.latitude;
    var long = position.coords.longitude;
    var latlong = lat + "," + long; // Store lat and long in the latlong variable

    // Call fetchIpInfo only after we have the geolocation
    fetchIpInfo(latlong);
}

// Error function to handle geolocation failure
function error(err) {
    console.error('Geolocation request failed or was denied.' + err);
    // You may want to handle a default location or notify the user here
    fetchIpInfo(); // Optionally, still send the request even without location data
}
async function fetchIpInfo(latlong) {
    try {
        if (latlong) {
            // Prepare the URL with the latlong parameter if available
            const url = "/api/Notification/GetClientIp?url=" + encodeURIComponent(window.location.pathname) + "&latlong=" + encodeURIComponent(latlong);

            // Make the fetch call
            const response = await fetch(url);

            // Handle if the response is not OK
            if (!response.ok) {
                console.error('There has been a problem with your ip fetch operation');
                document.querySelector('#ipAddress .info-data').textContent = data.ipAddress || 'Not available';
                document.querySelector('#ipAddress1 .info-data').textContent = data.ipAddress || 'Not available';
            }

            else {
                // Parse the response data as JSON
                const data = await response.json();

                // Update the page with the received data
                document.querySelector('#ipAddress .info-data').textContent = data.ipAddress || 'Not available';
                document.querySelector('#ipAddress1 .info-data').textContent = data.ipAddress || 'Not available';
            }
        }
    } catch (error) {
        console.error('There has been a problem with your fetch operation:', error);
    }
}

if (navigator.geolocation) {
    navigator.geolocation.getCurrentPosition(success, error);
} else {
    console.error('Geolocation is not supported by this browser.');
    fetchIpInfo(); // Optionally call fetchIpInfo even if geolocation is not available
}
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
        minLength: 1, // Start showing suggestions after 1 character
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
});

function onlyDigits(el) {
    el.value = el.value.replace(/[^0-9.]/g, '').replace(/(\..*)\./g, '$1');
}
window.onload = function () {
    focusLogin();
}