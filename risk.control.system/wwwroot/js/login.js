document.getElementById("password").focus();
const togglePassword = document.querySelector('#togglePassword');
function myFunction(x, show) {
    if (show) {
        togglePassword.style.visibility = "visible";
    } else {
        togglePassword.style.visibility = "hidden";
    }
}
const password = document.querySelector('#password');

togglePassword.addEventListener('click', function (e) {
    // toggle the type attribute
    const type = password.getAttribute('type') === 'password' ? 'text' : 'password';
    password.setAttribute('type', type);
    // toggle the eye slash icon
    this.classList.toggle('fa-eye-slash');
});