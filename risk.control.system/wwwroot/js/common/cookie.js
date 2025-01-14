$(document).ready(function () {
    const cookiePopup = $("#cookiePopup");
    const cookieCancel = $("#cookieRevoke");

    // Step 1: Check if the pageLoaded cookie exists and clear cookies if not
    if (!getCookie("pageLoaded")) {
        clearAllCookies(); // Clear all cookies
        setCookie("pageLoaded", "true", 365); // Mark that the page has been loaded for a year
    }

    // Check if the user has accepted the cookie consent
    if (!getCookie("cookieConsentAccepted")) {
        cookiePopup.fadeIn();
    } else {
        cookieCancel.fadeIn();
    }

    // Handle accept cookies button click
    $("#acceptCookies").on("click", function () {
        if (!getCookie("cookieConsentAccepted")) {
            setCookie("cookieConsentAccepted", "true", 365); // Set consent for 1 year
        }
        cookiePopup.fadeOut(); // Hide the popup
        acceptCookies(cookieCancel);
    });

    // Handle revoke consent button click
    $("#revokeConsent").on("click", function () {
        cookiePopup.fadeOut();
        if (getCookie("cookieConsentAccepted")) {
            deleteCookie("cookieConsentAccepted");
        }
        revokeCookies(cookiePopup);
    });

    // Handle cancel consent button click
    $("body").on("click", "#cancelConsent", function () {
        if (getCookie("cookieConsentAccepted")) {
            deleteCookie("cookieConsentAccepted");
        }
        revokeCookies(cookiePopup);
    });
});

// Function to accept cookies and make a server call
async function acceptCookies(cookieCancel) {
    try {
        const response = await fetch('/api/secure/AcceptCookies', { method: 'POST' });
        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }
        const data = await response.json();
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

// Function to revoke cookies and make a server call
async function revokeCookies(cookiePopup) {
    try {
        const response = await fetch('/api/secure/RevokeCookies', { method: 'POST' });
        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }
        const data = await response.json();
        console.log('Cookie cancelled:', data.message);
        const message = data.message || "Cookies have been cancelled.";
        showAlertWithAutoClose(`<i class="fas fa-times" class="danger"></i> ${message}`, 2000);
        cookiePopup.fadeIn(2000);
    } catch (error) {
        console.error('Error:', error);
        showAlertWithAutoClose("An error occurred while revoking cookies. Please try again.", 4000);
    }
}

// Utility functions for cookie operations
function setCookie(name, value, days) {
    const expires = new Date();
    expires.setDate(expires.getDate() + days);
    document.cookie = `${name}=${value}; expires=${expires.toUTCString()}; path=/; Secure; SameSite=Strict;`;
}

function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
    return null;
}

function deleteCookie(name) {
    document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;`;
}

function clearAllCookies() {
    const cookies = document.cookie.split("; ");
    for (let i = 0; i < cookies.length; i++) {
        const cookieName = cookies[i].split("=")[0];
        deleteCookie(cookieName);
    }
}

// Alert functions
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
