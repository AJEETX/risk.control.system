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
        $('#login').html(" Login");

        $('#login-form').submit();
        var nodes = document.getElementById("fullpage").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });
});