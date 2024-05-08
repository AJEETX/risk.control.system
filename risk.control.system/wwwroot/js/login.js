$.validator.setDefaults({
    submitHandler: function (form) {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        form.submit();

        $('#login-form').attr('disabled', 'disabled');
        $('#reset-form').attr('disabled', 'disabled');

        $('#email, .login-link').attr('disabled', 'disabled');
        $('#flip').attr('disabled', 'disabled');

        $('#resetemail').attr('disabled', 'disabled');

        $('#password').attr('disabled', 'disabled');
        $('#password').css('color', 'lightgrey');

        $('#login').css('color', 'lightgrey');
        $('#reset-pwd').css('color', 'lightgrey');

        $('#login').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Login');
        $('#reset-pwd').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Reset Password');

        $('a').attr('disabled', 'disabled');
        $('button').attr('disabled', 'disabled');
        $('html button').css('pointer-events', 'none')
        $('html a').css({ 'pointer-events': 'none' }, { 'cursor': 'none' })
        $('.text').css({ 'pointer-events': 'none' }, { 'cursor': 'none' })

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
async function fetchIpInfo() {
    try {
        const url = "/api/Notification/GetClientIp?url=" + encodeURIComponent(window.location.pathname);
        //const url = "/api/Notification/GetClientIp";
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        const data = await response.json();
        document.querySelector('#ipAddress .info-data').textContent = data.ipAddress || 'Not available';
        document.querySelector('#ipAddress1 .info-data').textContent = data.ipAddress || 'Not available';

    } catch (error) {
        console.error('There has been a problem with your fetch operation:', error);
    }
}
var askConfirm = true;
$(document).ready(function () {
    $("#login-form").validate();
    $("#reset-form").validate();

    //$('#reset-form').submit(function (event) {
    //    if ($(this).valid()) {
    //        event.preventDefault();
    //        $("body").addClass("submit-progress-bg");
    //        // Wrap in setTimeout so the UI
    //        // can update the spinners
    //        setTimeout(function () {
    //            $(".submit-progress").removeClass("hidden");
    //        }, 1);

    //        $('#reset-pwd').attr('disabled', 'disabled');
    //        $('#reset-pwd').css('color', 'lightgrey');
    //        $('#reset-pwd').html('<span class="fas fa-sync fa-spin" aria-hidden="true"></span> Login');
    //        askConfirm = false;
    //        $('#reset-form').submit();
    //        $('html *').css('cursor', 'not-allowed');
    //        $('#reset-form').attr('disabled', 'disabled');

    //        var nodes = document.getElementById("reset-pwd").getElementsByTagName('*');
    //        for (var i = 0; i < nodes.length; i++) {
    //            nodes[i].disabled = true;
    //        }
    //    }
        
    //});
    fetchIpInfo();
});

function onlyDigits(el) {
    el.value = el.value.replace(/[^0-9.]/g, '').replace(/(\..*)\./g, '$1');
}
window.onload = function () {
    focusLogin();
}