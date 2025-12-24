var askConfirmation = true; // Prevent duplicate confirmation dialogs
var logoutPath = "/Account/Logout";
var defaultTimeoutSeconds = parseInt(document.getElementById('timeout')?.value || "900", 10); // Default 15 minutes
var sessionTimer = localStorage.getItem("sessionTimer")
    ? parseInt(localStorage.getItem("sessionTimer"), 10)
    : defaultTimeoutSeconds;
const refreshSessionPath = "/Account/KeepSessionAlive"; // Path to refresh session

async function refreshSession() {
    const currentPageUrl = window.location.href;
    console.log(`Refreshing session on ${currentPageUrl}`);
    var token = $('input[name="__RequestVerificationToken"]').val();

    try {
        // Send a POST request to keep the session alive
        const response = await fetch(refreshSessionPath, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                'X-CSRF-TOKEN': token   // ✔ Correct header name
            },
            credentials: "include", // Include cookies in the request
            body: JSON.stringify({ currentPage: currentPageUrl }), // Include the current page URL
        });

        if (response.status === 401 || response.status === 403) {
            console.warn("Session expired or user is unauthorized. Redirecting to login page...");
            window.location.href = "/Account/Login"; // Redirect to the login page
            return;
        }

        if (!response.ok) {
            const errorText = await response.text(); // Extract error message from response
            console.error(`Failed to refresh session. Status: ${response.status}, Response: ${errorText}`);
            return;
        }

        // Parse and log the user identity details if available
        try {
            const userDetails = await response.json();
            console.log("User Identity Details:", userDetails);
        } catch (error) {
            console.error("Failed to parse user identity details as JSON:", error);
        }
    } catch (error) {
        console.error("Error during session refresh request:", error);
    }
}


//// Function to start the timer
//function startTimer(timeout, display) {
//    let timer = timeout;

//    // Function to update the display
//    function updateDisplay() {
//        const minutes = String(Math.floor(timer / 60)).padStart(2, "0");
//        const seconds = String(timer % 60).padStart(2, "0");

//        if (display) {
//            display.textContent = `${minutes}:${seconds}`;
//        }

//        console.log(`Timer Updated: ${minutes}:${seconds}`); // Debugging timer
//    }

//    // Initial display update
//    updateDisplay();

//    // Main countdown logic
//    const countdown = setInterval(function () {
//        // Sync timer with localStorage
//        const storedTimer = parseInt(localStorage.getItem("sessionTimer"), 10);
//        timer = isNaN(storedTimer) ? timer : storedTimer;

//        // Update the display
//        updateDisplay();

//        // Handle session expiration warning
//        if (timer <= 15 && askConfirmation) {
//            askConfirmation = false; // Prevent duplicate popups
//            const clockDisplay = document.getElementById("time");

//            if (clockDisplay) {
//                clockDisplay.style.visibility = "hidden"; // Hide timer display during popup
//            }
//            const hourGlassIcon = document.getElementById("hour-glass");

//            // Set the class dynamically
//            if (hourGlassIcon) {
//                console.log(`hourGlassIcon Updated`); // Debugging timer
//                hourGlassIcon.classList.remove("fa-spin"); // Remove unwanted classes, if necessary
//            }
//            // Confirmation dialog
//            $.confirm({
//                title: "Session Expiring!",
//                content: `Your session expiring!<br>Click <b>CONTINUE</b>.`,
//                icon: "fas fa-hourglass-end fa-spin",
//                type: "orange",
//                closeIcon: true,
//                autoClose: `cancel|${timer * 1000}`, // Auto-close after the timer expires
//                onOpenBefore: function () {
//                    // Add blur effect to the background
//                    document.getElementById("main-container").classList.add("blur-background");
//                },
//                buttons: {
//                    confirm: {
//                        text: "CONTINUE",
//                        btnClass: "btn-warning",
//                        action: function () {
//                            timer = defaultTimeoutSeconds; // Reset session timer
//                            localStorage.setItem("sessionTimer", timer);
//                            refreshSession(); // Refresh session on confirmation
//                            askConfirmation = true;
//                            // Remove blur effect from the background
//                            document.getElementById("main-container").classList.remove("blur-background");
//                            // Show clock again
//                            if (clockDisplay) {
//                                clockDisplay.style.visibility = "visible";
//                            }
//                            if (hourGlassIcon) {
//                                hourGlassIcon.classList.add("fa-spin"); // Remove unwanted classes, if necessary
//                            }

//                            updateDisplay();
//                        },
//                    },
//                    cancel: {
//                        text: "LOGOUT",
//                        btnClass: "btn-default",
//                        action: function () {
//                            localStorage.removeItem("sessionTimer");
//                            clearInterval(countdown); // Stop the timer
//                            console.log("User logged out."); // Debug
//                            window.location.href = logoutPath; // Logout
//                        },
//                    },
//                },
//                onDestroy: function () {
//                    // Remove blur effect if dialog is closed
//                    document.getElementById("main-container").classList.remove("blur-background");
//                },
//            });
//        }

//        // Handle session expiration
//        if (timer <= 0) {
//            clearInterval(countdown); // Stop countdown
//            localStorage.removeItem("sessionTimer");
//            console.log("Session expired, logging out."); // Debug
//            window.location.href = logoutPath;
//        } else {
//            timer--;
//            localStorage.setItem("sessionTimer", timer);

//            // Periodically refresh the session (every 5 minutes)
//            if (timer % 300 === 0) {
//                refreshSession();
//            }
//        }
//    }, 1000);
//}

//// Handle tab visibility changes
//document.addEventListener("visibilitychange", function () {
//    if (document.visibilityState === "visible") {
//        const lastInactiveTime = parseInt(localStorage.getItem("lastInactiveTime"), 10);
//        if (lastInactiveTime) {
//            const elapsed = Math.floor((Date.now() - lastInactiveTime) / 1000);
//            sessionTimer = Math.max(0, sessionTimer - elapsed);

//            if (sessionTimer <= 0) {
//                localStorage.setItem("sessionTimer", 0);
//                console.log("Session expired during inactivity."); // Debug
//                window.location.href = logoutPath;
//                return;
//            }

//            localStorage.setItem("sessionTimer", sessionTimer);
//        }
//    } else {
//        localStorage.setItem("lastInactiveTime", Date.now());
//    }
//});

//// Function to display current time in 12-hour format
//function showTime() {
//    const now = new Date();
//    let hour = now.getHours();
//    const minute = String(now.getMinutes()).padStart(2, "0");
//    const second = String(now.getSeconds()).padStart(2, "0");
//    const am_pm = hour >= 12 ? "PM" : "AM";

//    hour = hour % 12 || 12; // Convert to 12-hour format
//    const timeString = `${String(hour).padStart(2, "0")}:${minute}:${second} ${am_pm}`;
//    const clockDisplay = document.getElementById("clock");

//    if (clockDisplay) {
//        clockDisplay.innerHTML = timeString;
//    }
//}

//// Initialize on page load
//window.onload = function () {
//    const display = document.querySelector("#time");
//    const timeoutElement = document.getElementById("timeout");

//    if (timeoutElement) {
//        sessionTimer = parseInt(timeoutElement.value, 10);
//        localStorage.setItem("sessionTimer", sessionTimer);
//    }

//    const savedTimer = localStorage.getItem("sessionTimer");
//    if (savedTimer) {
//        sessionTimer = parseInt(savedTimer, 10);
//    }

//    startTimer(sessionTimer, display);
//    setInterval(showTime, 1000); // Update clock every second
//};
