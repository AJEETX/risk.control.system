var askConfirmation = true; // Flag to prevent duplicate confirmation dialogs
var logoutPath = "/Account/Logout";
var defaultTimeoutSeconds = parseInt(document.getElementById('timeout')?.value || "900", 10); // Default 15 minutes
var sessionTimer = localStorage.getItem("sessionTimer")
    ? parseInt(localStorage.getItem("sessionTimer"), 10)
    : defaultTimeoutSeconds;

// Function to start the timer
function startTimer(timeout, display) {
    let timer = timeout;

    // Function to update the display
    function updateDisplay() {
        const minutes = String(Math.floor(timer / 60)).padStart(2, "0");
        const seconds = String(timer % 60).padStart(2, "0");
        if (display) {
            display.textContent = `${minutes}:${seconds}`;
        }
    }

    // Update the display initially
    updateDisplay();

    // Main countdown logic
    const countdown = setInterval(function () {
        // Update the timer from localStorage
        timer = parseInt(localStorage.getItem("sessionTimer"), 10) || timer;

        // Update the display
        updateDisplay();

        // Show confirmation popup when 15 seconds are left
        if (timer <= 15 && askConfirmation) {
            askConfirmation = false; // Prevent duplicate confirmation dialogs
            const clockDisplay = document.getElementById("time");
            if (clockDisplay) {
                timer
                clockDisplay.style.visibility = "hidden"; // Hide clock when popup shows
            }
            // Display confirmation popup
            $.confirm({
                title: "Session Expiring!",
                content: `Your session is about to expire!<br>Click <b>CONTINUE</b> to stay logged in.`,
                icon: "fas fa-hourglass-end fa-spin",
                type: "orange",
                closeIcon: true,
                autoClose: `cancel|${timer * 1000}`, // Auto-close after the timer expires
                buttons: {
                    confirm: {
                        text: "CONTINUE",
                        btnClass: "btn-warning",
                        action: function () {
                            // Reset session timer and continue
                            timer = defaultTimeoutSeconds;
                            localStorage.setItem("sessionTimer", timer);
                            askConfirmation = true; // Allow future popups if needed
                            // Show the clock again
                            if (clockDisplay) {
                                clockDisplay.style.visibility = "visible"; // Show clock again
                            }
                            updateDisplay();
                        },
                    },
                    cancel: {
                        text: "LOGOUT",
                        btnClass: "btn-default",
                        action: function () {
                            localStorage.removeItem("sessionTimer");
                            window.location.href = logoutPath; // Logout
                        },
                    },
                },
            });
        }

        // Handle session expiration
        if (timer <= 0) {
            clearInterval(countdown); // Stop the countdown
            localStorage.removeItem("sessionTimer");
            window.location.href = logoutPath; // Redirect to logout
        } else {
            timer--;
            localStorage.setItem("sessionTimer", timer);
        }
    }, 1000);
}

// Handle tab visibility changes
document.addEventListener("visibilitychange", function () {
    if (document.visibilityState === "visible") {
        // Tab becomes active
        const lastInactiveTime = parseInt(localStorage.getItem("lastInactiveTime"), 10);
        if (lastInactiveTime) {
            const elapsed = Math.floor((Date.now() - lastInactiveTime) / 1000);
            sessionTimer = Math.max(0, sessionTimer - elapsed); // Decrease timer by elapsed time

            if (sessionTimer <= 0) {
                // Log out immediately if session expired during inactivity
                localStorage.setItem("sessionTimer", 0);
                window.location.href = logoutPath;
                return;
            }

            // Save the updated session timer
            localStorage.setItem("sessionTimer", sessionTimer);
        }

        // Immediately update the display with the current timer value
        const display = document.querySelector("#time");
        if (display) {
            const minutes = String(Math.floor(sessionTimer / 60)).padStart(2, "0");
            const seconds = String(sessionTimer % 60).padStart(2, "0");
            display.textContent = `${minutes}:${seconds}`;
        }
    } else {
        // Tab becomes inactive
        localStorage.setItem("lastInactiveTime", Date.now());
    }
});

// Display the current time in 12-hour format
function showTime() {
    const now = new Date();
    let hour = now.getHours();
    const minute = String(now.getMinutes()).padStart(2, "0");
    const second = String(now.getSeconds()).padStart(2, "0");
    const am_pm = hour >= 12 ? "PM" : "AM";

    hour = hour % 12 || 12; // Convert to 12-hour format
    const timeString = `${String(hour).padStart(2, "0")}:${minute}:${second} ${am_pm}`;
    const clockDisplay = document.getElementById("clock");

    if (clockDisplay) {
        clockDisplay.innerHTML = timeString;
    }
}

// Initialize on page load
window.onload = function () {
    const display = document.querySelector("#time");
    const timeoutElement = document.getElementById("timeout");

    // Set the session timer based on the timeout element if present
    if (timeoutElement) {
        sessionTimer = parseInt(timeoutElement.value, 10);
        localStorage.setItem("sessionTimer", sessionTimer);
    }

    // Use the saved session timer from localStorage
    const savedTimer = localStorage.getItem("sessionTimer");
    if (savedTimer) {
        sessionTimer = parseInt(savedTimer, 10);
    }

    // Start the timer
    startTimer(sessionTimer, display);

    // Show the current time
    setInterval(showTime, 1000); // Ensure the clock updates every second
};
