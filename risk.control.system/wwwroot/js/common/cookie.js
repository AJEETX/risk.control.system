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
        localStorage.setItem("cookieConsentAccepted", "true");
        cookiePopup.fadeOut(); // Hide the popup
        acceptCookies(cookieCancel);
    });

    $("#revokeConsent").on("click", function () {
        cookiePopup.fadeOut(); // Hide the popup
        localStorage.removeItem("cookieConsentAccepted");
        revokeCookies(cookiePopup);
    });

    $("body").on("click", "#cancelConsent", function () {
        cookiePopup.fadeOut();
        localStorage.removeItem("cookieConsentAccepted");
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
        console.log('Cookie accepted:', data);
        showAlertWithAutoClose(data.message || "Cookies have been successfully accepted.", 2000); // Show alert with auto-close)
        cookieCancel.fadeIn();
    } catch (error) {
        console.error('Error:', error);
    }
}

async function revokeCookies(cookiePopup) {
    try {
        const response = await fetch('/api/secure/RevokeCookies', { method: 'POST' });
        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }
        const data = await response.json(); // Assuming the server returns JSON
        console.log('Cookie cancelled :', data);
        showAlertWithAutoClose(data.message || "Cookies cancelled.", 2000); // Show alert with auto-close)
        cookiePopup.fadeIn(1000);
    } catch (error) {
        console.error('Error:', error);
    }
}
function showAlertWithAutoClose(message, delay) {
    showAlert(message);
    setTimeout(function () {
        $("#customAlert").fadeOut();
    }, delay || 3000); // Default delay is 3 seconds
}
function showAlert(message) {
    $("#alertMessage").text(message); // Set the alert message
    $("#customAlert").fadeIn(); // Show the alert
}