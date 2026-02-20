var loginPath = "/Account/Login";
const refreshSessionPath = "/Session/KeepSessionAlive"; // Path to refresh session
let isExpired = false; // New state flag

async function refreshSession() {
    const currentPageUrl = window.location.href;
    console.log(`Refreshing session on page ${currentPageUrl}`);
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
            isExpired = true; // Stop activity tracking immediately
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
                            smoothRedirect(loginPath);
                        }
                    }
                },
                onClose: function () {
                    // In case user closes the dialog manually
                    smoothRedirect(loginPath);
                }
            });
            return;
        }

        // Parse and log user identity if available
        try {
            const userDetails = await response.json();
            console.log("User Details:", userDetails);
        } catch (error) {
            console.error("Failed to parse user details as JSON:", error);
            smoothRedirect(loginPath);
        }
    } catch (error) {
        console.error("Error during session refresh request:", error);
        isExpired = true; // Stop activity tracking immediately

        $.confirm({
            title: 'Connection Error!',
            content: 'There was a problem refreshing your session. You will be redirected to the login page.',
            type: 'red',
            typeAnimated: true,
            buttons: {
                login: {
                    text: 'Login Again',
                    btnClass: 'btn-warning',
                    action: function () {
                        smoothRedirect(loginPath);
                    }
                }
            }
        });
    }
}

let idleTimer = null;
var lastSyncTime = 0;
var defaultTimeoutSeconds = parseInt(document.getElementById('timeout')?.value || "900", 10); // Default 900 seconds
var idleLimit = defaultTimeoutSeconds * 1000; // in minutes
var syncInterval = 60000;     // Sync server once per minute
function resetIdleTimer() {
    if (isExpired) return;
    const now = new Date().getTime();

    if (now - lastSyncTime > syncInterval) {
        refreshSession();
        lastSyncTime = now;
    }

    clearTimeout(idleTimer);
    idleTimer = setTimeout(userIsIdle, idleLimit);
    const expiry = new Date(now + idleLimit);
    console.log(`%c[${new Date().toLocaleTimeString()}] %cTimer Reset. %cExpires at: %c${expiry.toLocaleTimeString()}`,
        "color: gray;", "color: green; font-weight: bold;", "color: black;", "color: red; font-weight: bold;");
}

function userIsIdle() {
    isExpired = true; // Block further resets
    const idleMinutes = Math.floor(defaultTimeoutSeconds / 60);
    $.confirm({
        title: '<i class="fas fa-exclamation-triangle text-danger"></i> Session Expired',
        content: `
        <div class="text-center">
            <div class="mb-3">
                <i class="fas fa-history fa-4x text-warning animate__animated animate__pulse animate__infinite"></i>
            </div>
            <p>
                You have been inactive for <b><u><i>${idleMinutes} minutes</i></u></b>.<br>
                For your security, please log back in.
            </p>
        </div>`,
        type: 'orange',
        theme: 'modern', // Much cleaner than the default
        icon: 'fas fa-lock',
        backgroundDismiss: false,
        closeIcon: false,
        animation: 'scale', // Smooth entrance
        closeAnimation: 'zoom',
        animateFromElement: false,
        bgOpacity: 0.7,
        buttons: {
            login: {
                text: 'Login Again',
                btnClass: 'btn-warning',
                action: function () {
                    smoothRedirect(loginPath);
                }
            }
        }
    });
}
function smoothRedirect(url) {
    $('body').fadeOut(800, function () {
        window.location.href = url;
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