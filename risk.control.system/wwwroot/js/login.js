function focusLogin() {
    const pwd = document.getElementById("password");
    pwd.select();
}
focusLogin();

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
        $('html a *, html button *').attr('disabled', 'disabled');
        $('html a *, html button *').css('pointer-events', 'none')

        var nodes = document.getElementById("fullpage").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });
});