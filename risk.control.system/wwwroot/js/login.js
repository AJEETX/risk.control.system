function focusLogin() {
    const pwd = document.getElementById("password");
    pwd.select();
}
focusLogin();





$(document).ready(function () {

    $('#login').on('click', function (event) {
        $('#login').prop('disabled', true);
        $('#login').html("... loggin...");
        $('#login-form').submit();
    });
});
