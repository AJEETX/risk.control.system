var askConfirmation = true;
var logoutPath = "/Account/Logout";
var defaultTimeoutSeconds = parseInt(document.getElementById('timeout')?.value || "900", 10); // Default 15 minutes
var sessionTimer = localStorage.getItem("sessionTimer")
    ? parseInt(localStorage.getItem("sessionTimer"), 10)
    : defaultTimeoutSeconds;

function startTimer(timeout, display) {
    var timer = timeout;

    var countdown = setInterval(function () {
        var minutes = parseInt(timer / 60, 10);
        var seconds = parseInt(timer % 60, 10);

        // Pad minutes and seconds with leading zeros
        minutes = minutes < 10 ? "0" + minutes : minutes;
        seconds = seconds < 10 ? "0" + seconds : seconds;

        // Update the timer display
        display.textContent = minutes + ":" + seconds;

        // If time is over, logout
        if (timer <= 0) {
            clearInterval(countdown);
            localStorage.removeItem("sessionTimer"); // Clear session storage
            window.location.href = logoutPath;
        }

        // Show session expiration confirmation when timer <= 15 seconds
        else if (timer <= 15 && askConfirmation) {
            askConfirmation = false; // Ensure confirmation dialog only shows once
            $.confirm({
                title: "Session Expiring!",
                content: `Your session is about to expire!<br />Click <b>REFRESH</b> to stay logged in.`,
                icon: 'fas fa-hourglass-end fa-spin',
                type: 'orange',
                closeIcon: true,
                autoClose: `cancel|${timer * 1000}`,
                buttons: {
                    confirm: {
                        text: "REFRESH",
                        btnClass: 'btn-warning',
                        action: function () {
                            localStorage.removeItem("sessionTimer"); // Reset session timer
                            window.location.href = window.location.href; // Refresh the page
                        }
                    },
                    cancel: {
                        text: "LOGOUT",
                        btnClass: 'btn-default',
                        action: function () {
                            localStorage.removeItem("sessionTimer");
                            window.location.href = logoutPath; // Logout
                        }
                    }
                }
            });
        }

        timer--; // Decrement timer every second
        localStorage.setItem("sessionTimer", timer); // Update sessionTimer in localStorage
    }, 1000);
}


// Show current time in 12-hour format
function showTime() {
    let time = new Date();
    let hour = time.getHours();
    let min = time.getMinutes();
    let sec = time.getSeconds();
    let am_pm = "AM";

    if (hour >= 12) {
        if (hour > 12) hour -= 12;
        am_pm = "PM";
    } else if (hour == 0) {
        hour = 12;
        am_pm = "AM";
    }

    hour = hour < 10 ? "0" + hour : hour;
    min = min < 10 ? "0" + min : min;
    sec = sec < 10 ? "0" + sec : sec;

    let currentTime = hour + ":" + min + ":" + sec + " " + am_pm;

    var clockTime = document.getElementById("clock");
    if (clockTime) {
        clockTime.innerHTML = currentTime; // Display current time in clock element
    }
}

// Detect when the tab becomes visible or hidden
document.addEventListener("visibilitychange", function () {
    if (document.visibilityState === 'visible') {
        // Tab becomes visible
        var lastTime = localStorage.getItem("lastTime");
        if (lastTime) {
            var elapsed = Math.floor((new Date().getTime() - lastTime) / 1000);

            // Update sessionTimer based on elapsed time
            sessionTimer = parseInt(localStorage.getItem("sessionTimer"), 10) || sessionTimer;
            sessionTimer -= elapsed;

            // Ensure timer doesn't go below zero
            if (sessionTimer <= 0) {
                sessionTimer = 0;
                localStorage.setItem("sessionTimer", sessionTimer);
                window.location.href = logoutPath; // Logout if session expired
                return;
            }

            // Update localStorage with new sessionTimer value
            localStorage.setItem("sessionTimer", sessionTimer);

            // Update the display with the adjusted timer
            var display = document.querySelector('#time');
            if (display) {
                var minutes = parseInt(sessionTimer / 60, 10);
                var seconds = parseInt(sessionTimer % 60, 10);
                display.textContent =
                    (minutes < 10 ? "0" : "") + minutes + ":" +
                    (seconds < 10 ? "0" : "") + seconds;
            }
        }
    } else {
        // Tab becomes hidden
        localStorage.setItem("lastTime", new Date().getTime());
    }
});


window.onload = function () {
    var display = document.querySelector('#time');
    var timeoutElement = document.getElementById('timeout');

    // Initialize sessionTimer from the 'timeout' element value if available
    if (timeoutElement != null) {
        sessionTimer = parseInt(timeoutElement.value, 10);
        localStorage.setItem("sessionTimer", sessionTimer);
    }

    // Load saved session time from localStorage if available
    var savedTimeout = localStorage.getItem("sessionTimer");
    if (savedTimeout) {
        sessionTimer = parseInt(savedTimeout, 10);
    }

    // Ensure sessionTimer is valid and not less than zero
    if (sessionTimer <= 0) {
        sessionTimer = parseInt(timeoutElement ? timeoutElement.value : "15", 10); // Fallback to default 15 seconds if no timeout element
    }

    // Initialize the timer
    startTimer(sessionTimer, display);
    showTime();
};
