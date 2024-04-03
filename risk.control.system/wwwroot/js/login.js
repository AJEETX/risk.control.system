function focusLogin() {
    const pwd = document.getElementById("password");
    pwd.select();
}
focusLogin();
    async function fetchIpInfo() {
        try {
            const response = await fetch('/api/Notification/GetClientIp');
            if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
            const data = await response.json();
            document.querySelector('#ipAddress .info-data').textContent = data.ipAddress || 'Not available';

            } catch (error) {
            console.error('There has been a problem with your fetch operation:', error);
            }
        }

$(document).ready(function () {
    $('#login').on('click', function (event) {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#login').attr('disabled', 'disabled');
        $('#login').css('color', 'lightgrey');
        $('#login').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Login');

        $('#login-form').submit();
        $('html *').css('cursor', 'not-allowed');
        $('html input *').attr('disabled', 'disabled');
        $('html a *, html button *').attr('disabled', 'disabled');
        $('html a *, html button *').css('pointer-events', 'none')

        var nodes = document.getElementById("login-form").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });
    $('#reset-pwd').on('click', function (event) {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#reset-pwd').attr('disabled', 'disabled');
        $('#reset-pwd').css('color', 'lightgrey');
        $('#reset-pwd').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Login');

        $('#reset-form').submit();
        $('html *').css('cursor', 'not-allowed');
        $('#reset-form').attr('disabled', 'disabled');

        var nodes = document.getElementById("reset-pwd").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });
    fetchIpInfo();
});