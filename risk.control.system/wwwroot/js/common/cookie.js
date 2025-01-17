$(document).ready(function () {
    const cookiePopup = $("#cookiePopup");
    const cookieManagePopup = $("#cookieManagePopup");
    const cookieCancel = $("#cookieRevoke");

    // Step 1: Check if the pageLoaded cookie exists and clear cookies if not
    if (!getCookie("pageLoaded")) {
        clearAllCookies(); // Clear all cookies
        setCookie("pageLoaded", "true", 365); // Mark that the page has been loaded for a year
    }

    // Check if the user has accepted the cookie consent
    if (!getCookie("cookieConsent")) {
        cookiePopup.fadeIn();
    } 

    // Handle accept cookies button click
    $("#acceptCookies").on("click", function () {
        cookiePopup.fadeOut(); // Hide the popup
        acceptCookies(cookieCancel);
    });

    // Handle revoke consent button click
    $("#revokeConsent").on("click", function () {
        cookiePopup.fadeOut(); // Hide the popup
        acceptCookies(cookieCancel);
    });

    // Handle manage cookies button click
    $("#manageCookies").on("click", function () {
        cookiePopup.fadeOut(); // Hide main popup
        cookieManagePopup.fadeIn(); // Show manage popup
    });

    $("#savePreferences").on("click", function () {
        const analyticsCookies = $("#analyticsCookies").is(":checked");
        const marketingCookies = $("#marketingCookies").is(":checked");

        fetch('/api/secure/SavePreferences', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json', // Set content type to JSON
            },
            body: JSON.stringify({
                analyticsCookies: analyticsCookies,
                marketingCookies: marketingCookies
            })
        })
            .then((response) => {
                if (!response.ok) {
                    throw new Error(`HTTP error! Status: ${response.status}`);
                }
                return response.json();
            })
            .then((data) => {
                if (data.analyticsCookies !== undefined) {
                    document.cookie = `analyticsCookies=${data.analyticsCookies}; path=/; max-age=${365 * 24 * 60 * 60}`;
                }
                if (data.marketingCookies !== undefined) {
                    document.cookie = `marketingCookies=${data.marketingCookies}; path=/; max-age=${365 * 24 * 60 * 60}`;
                }

                console.log('Cookie accepted:', data.message);
                const message = data.message || "Cookies have been successfully saved.";
                // Only set cookieConsent if not already set
                if (!getCookie("cookieConsent")) {
                    setCookie("cookieConsent", "Accepted", 365); // Set consent for 1 year
                }

                cookieManagePopup.fadeOut(); // Hide manage popup
                showAlertWithAutoClose("Preferences saved successfully.", 1000);
                const login = document.getElementById("email");
                if (login) {
                    login.focus();
                }
            })
            .catch((error) => {
                console.error('Error:', error);
                showAlertWithAutoClose("An error occurred while saving cookie preferences. Please try again.", 3000);
            });
    });

    // Handle cancel manage button click
    $("#cancelManage").on("click", function () {
        cookieManagePopup.fadeOut();
        cookiePopup.fadeIn(); // Return to main popup
    });
});

function acceptCookies(cookieCancel) {
    fetch('/api/secure/AcceptCookies', { method: 'POST' })
        .then((response) => {
            if (!response.ok) {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }
            return response.json();
        })
        .then((data) => {
            if (data.analyticsCookies !== undefined) {
                document.cookie = `analyticsCookies=${data.analyticsCookies}; path=/; max-age=${365 * 24 * 60 * 60}`;
            }
            if (data.marketingCookies !== undefined) {
                document.cookie = `marketingCookies=${data.marketingCookies}; path=/; max-age=${365 * 24 * 60 * 60}`;
            }
            console.log('Cookie accepted:', data.message);
            const message = data.message || "Cookies have been successfully accepted.";
            if (!getCookie("cookieConsent")) {
                setCookie("cookieConsent", "Accepted", 365); // Set consent for 1 year
            }
            showAlertWithAutoClose(`<i class="fas fa-check"></i> ${message}`, 1000);
            const login = document.getElementById("email");
            if (login) {
                login.focus();
            }
        })
        .catch((error) => {
            console.error('Error:', error);
            showAlertWithAutoClose("An error occurred while accepting cookies consent. Please try again.", 3000);
        });
}

async function revokeCookies(cookiePopup) {
    try {
        const response = await fetch('/api/secure/RevokeCookies', { method: 'POST' });
        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }
        const data = await response.json();
        console.log('Cookie cancelled:', data.message);
        if (data.analyticsCookies !== undefined) {
            document.cookie = `analyticsCookies=${data.analyticsCookies}; path=/; max-age=${365 * 24 * 60 * 60}`;
        }
        if (data.marketingCookies !== undefined) {
            document.cookie = `marketingCookies=${data.marketingCookies}; path=/; max-age=${365 * 24 * 60 * 60}`;
        }
        const message = data.message || "Cookies have been successfully accepted.";
        if (!getCookie("cookieConsent")) {
            setCookie("cookieConsent", "Accepted", 365); // Set consent for 1 year
        }
        const login = document.getElementById("email");
        if (login) {
            login.focus();
        }
    } catch (error) {
        console.error('Error:', error);
        showAlertWithAutoClose("An error occurred while revoking cookies. Please try again.", 3000);
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
    if (parts.length === 2) return parts.pop().split(";").shift();
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

function showAlertWithAutoClose(message, delay) {
    showAlert(message);
    setTimeout(function () {
        $("#customAlert").fadeOut();
    }, delay || 1000); // Default delay is 3 seconds
}

function showAlert(message) {
    $("#alertMessage").html(message); // Set the alert message
    $("#customAlert").fadeIn(); // Show the alert
}
