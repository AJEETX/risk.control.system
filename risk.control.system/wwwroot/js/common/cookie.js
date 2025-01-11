$(document).ready(function () {
    const cookiePopup = $("#cookiePopup");
    const cookieCancel = $("#cookieRevoke");

    if (!localStorage.getItem("cookieConsentAccepted")) {
        cookiePopup.fadeIn();
    }
    else {
        cookieCancel.fadeIn();
    }
    $("#acceptCookies").on("click", function () {
        if (!localStorage.getItem("cookieConsentAccepted")) {
            localStorage.setItem("cookieConsentAccepted", "true");
        }
        cookiePopup.fadeOut(); // Hide the popup
        acceptCookies(cookieCancel);
    });

    $("#revokeConsent").on("click", function () {
        cookiePopup.fadeOut();

        if (localStorage.getItem("cookieConsentAccepted")) {
            localStorage.removeItem("cookieConsentAccepted");
        }
        revokeCookies(cookiePopup);
    });

    $("body").on("click", "#cancelConsent", function () {
        if (localStorage.getItem("cookieConsentAccepted")) {
            localStorage.removeItem("cookieConsentAccepted");
        }
        revokeCookies(cookiePopup);
    });
});
async function acceptCookies(cookieCancel) {
    try {
        const response = await fetch('/api/secure/AcceptCookies', { method: 'POST' });
        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }
        const data = await response.json(); // Assuming the server returns JSON
        console.log('Cookie accepted:', data.message);
        const message = data.message || "Cookies have been successfully accepted.";
        showAlertWithAutoClose(`<i class="fas fa-check"></i> ${message}`, 2000);
        cookieCancel.fadeIn();
        const login = document.getElementById("email");
        if (login) {
            login.focus();
        }
    } catch (error) {
        console.error('Error:', error);
        showAlertWithAutoClose("An error occurred while accepting cookies consent. Please try again.", 4000);
    }
}

async function revokeCookies(cookiePopup) {
    try {
        const response = await fetch('/api/secure/RevokeCookies', { method: 'POST' });
        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }
        const data = await response.json(); // Assuming the server returns JSON
        console.log('Cookie cancelled :', data.message);
        showAlertWithAutoClose(data.message || "Cookies cancelled.", 0); // Show alert with auto-close)
        cookiePopup.fadeIn();
    } catch (error) {
        console.error('Error:', error);
        showAlertWithAutoClose("An error occurred while revoking cookies. Please try again.", 4000);
    }
}
function showAlertWithAutoClose(message, delay) {
    showAlert(message);
    setTimeout(function () {
        $("#customAlert").fadeOut();
    }, delay || 3000); // Default delay is 3 seconds
}
function showAlert(message) {
    $("#alertMessage").html(message); // Set the alert message
    $("#customAlert").fadeIn(); // Show the alert
}