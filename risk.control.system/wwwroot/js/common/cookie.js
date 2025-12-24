$(document).ready(function () {

    const cookiePopup = $("#cookiePopup");
    const cookieManagePopup = $("#cookieManagePopup");

    if (!getCookie("icheckify.pageLoaded")) {
        clearAllCookies();
        setCookie("icheckify.pageLoaded", "True");
    }

    if (!getCookie("cookieConsent")) {
        cookiePopup.fadeIn();
    }

    // Accept all cookies
    $("#acceptCookies").on("click", function () {
        cookiePopup.fadeOut(300, function () {
            $("#email").focus();
        });
        acceptCookies();
    });

    // Revoke cookies
    $("#revokeConsent").on("click", function () {
        cookieManagePopup.fadeOut(300, function () {
            $("#email").focus();
        });
    });

    // Manage cookies
    $("#manageCookies").on("click", function () {
        cookiePopup.fadeOut();
        cookieManagePopup.fadeIn();
    });

    // Save preferences
    $("#savePreferences").on("click", function () {

        const analyticsCookies = $("#analyticsCookies").is(":checked");
        const perfomanceCookies = $("#perfomanceCookies").is(":checked");

        // Save to localStorage only
        //localStorage.setItem("analyticsCookies", analyticsCookies);
        //localStorage.setItem("perfomanceCookies", perfomanceCookies);

        const token = $('input[name="__RequestVerificationToken"]').val();

        fetch('/api/auth/SavePreferences', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': token // <-- correct antiforgery header
            },
            body: JSON.stringify({
                analyticsCookies: analyticsCookies,
                perfomanceCookies: perfomanceCookies
            })
        })
            .then(r => r.json())
            .then((r) => {
                console.log('cookie saved');
                console.log(r);
                // Ensure main consent cookie exists
                if (!getCookie("cookieConsent")) {
                    setCookie("cookieConsent", "Accepted", 365);
                }

                // Close popup + focus email box
                cookieManagePopup.fadeOut(300, function () {
                    $("#email").focus();
                });
            })
            .catch((err) => console.error(err));
    });


    // Cancel manage
    $("#cancelManage").on("click", function () {
        cookieManagePopup.fadeOut();
        cookiePopup.fadeIn();
    });
});


// ------------------------------
// UPDATED FUNCTIONS
// ------------------------------

function acceptCookies() {

    var token = $('input[name="__RequestVerificationToken"]').val();

    fetch('/api/auth/AcceptCookies', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': token   // ✔ Correct header name
        },
        body: JSON.stringify({})
    })
        .then(r => r.json())
        .then((r) => {
            console.log('cookie accepted');
            console.log(r);
            if (!getCookie("cookieConsent")) {
                setCookie("cookieConsent", "Accepted", 365);
            }
        })
        .catch(err => console.error(err));
}

async function revokeCookies() {

    const token = $('input[name="__RequestVerificationToken"]').val();

    await fetch('/api/auth/RevokeCookies',
        {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': token   // <-- correct antiforgery header
            },
            body: JSON.stringify({})   // <-- required for POST (even empty)
        });

    // Your logic after the POST
    if (!getCookie("cookieConsent")) {
        setCookie("cookieConsent", "Accepted", 365);
    }
}


// ------------------------------
// Cookie utility functions
// ------------------------------

function setCookie(name, value, days) {
    const expires = new Date(Date.now() + days * 86400000).toUTCString();
    document.cookie = `${name}=${value}; expires=${expires}; path=/; Secure; SameSite=Strict`;
}

function getCookie(name) {
    const match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
    return match ? match[2] : null;
}

function deleteCookie(name) {
    document.cookie =
        `${name}=; ` +
        "expires=Thu, 01 Jan 1970 00:00:00 UTC; " +
        "path=/; " +
        "Secure; " +        // ✔ ensures cookie only sent over HTTPS
        "SameSite=Strict";  // ✔ prevents CSRF leakage
}

function clearAllCookies() {
    const cookies = document.cookie.split("; ");
    cookies.forEach(c => deleteCookie(c.split("=")[0]));
}