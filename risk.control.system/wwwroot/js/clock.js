var askConfirmation = true; // Prevent duplicate confirmation dialogs
var logoutPath = "/Account/Logout";
var defaultTimeoutSeconds = parseInt(document.getElementById('timeout')?.value || "900", 10); // Default 15 minutes
var sessionTimer = localStorage.getItem("sessionTimer")
    ? parseInt(localStorage.getItem("sessionTimer"), 10)
    : defaultTimeoutSeconds;
const refreshSessionPath = "/Session/KeepSessionAlive"; // Path to refresh session

async function refreshSession() {
    const currentPageUrl = window.location.href;
    console.log(`Refreshing session on ${currentPageUrl}`);
    var token = $('input[name="__RequestVerificationToken"]').val();

    try {
        const response = await fetch(refreshSessionPath, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                'X-CSRF-TOKEN': token
            },
            credentials: "include",
            body: JSON.stringify({ currentPage: currentPageUrl }),
        });

        if (response.status === 401 || response.status === 403 || !response.ok) {
            // Use jConfirm before redirecting
            $.confirm({
                title: 'Session Expired!',
                content: 'Your session has expired or you are unauthorized. You will be redirected to the login page.',
                type: 'red',
                typeAnimated: true,
                buttons: {
                    Ok: {
                        text: 'Login',
                        btnClass: 'btn-red',
                        action: function () {
                            window.location.href = "/Account/Login";
                        }
                    }
                },
                onClose: function () {
                    // In case user closes the dialog manually
                    window.location.href = "/Account/Login";
                }
            });
            return;
        }

        // Parse and log user identity if available
        try {
            const userDetails = await response.json();
            console.log("User Identity Details:", userDetails);
        } catch (error) {
            console.error("Failed to parse user identity details as JSON:", error);
            // Optional: show jConfirm before redirect
            $.confirm({
                title: 'Session Error!',
                content: 'Unable to parse session. Redirecting to login.',
                type: 'red',
                typeAnimated: true,
                buttons: {
                    Ok: function () {
                        window.location.href = "/Account/Login";
                    }
                }
            });
        }
    } catch (error) {
        console.error("Error during session refresh request:", error);
        $.confirm({
            title: 'Connection Error!',
            content: 'There was a problem refreshing your session. You will be redirected to the login page.',
            type: 'red',
            typeAnimated: true,
            buttons: {
                Ok: function () {
                    window.location.href = "/Account/Login";
                }
            }
        });
    }
}

let idleTimer = null;
let idleLimit = defaultTimeoutSeconds * 1000; // 15 minutes

function resetIdleTimer() {
    clearTimeout(idleTimer);
    idleTimer = setTimeout(userIsIdle, idleLimit);
}

function userIsIdle() {
    $.confirm({
        title: '<i class="fas fa-exclamation-triangle text-danger"></i> Session Expired',
        content: 'You have been inactive for 15 minutes. Please login again.',
        type: 'red',
        closeIcon: false,
        buttons: {
            login: {
                text: 'Login',
                btnClass: 'btn-danger',
                action: function () {
                    window.location.href = '/Account/Login';
                }
            }
        }
    });
}

function handleSessionExpired() {
    clearTimeout(idleTimer);
    userIsIdle();
}

// Detect activity
$(document).on(
    'mousemove keydown click scroll touchstart',
    resetIdleTimer
);

// Start timer on load
resetIdleTimer();