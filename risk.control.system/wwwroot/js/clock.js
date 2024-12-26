var askConfirmation = true;
var logoutPath = "/Account/Logout";
var defaultTimeoutSeconds = parseInt(document.getElementById('timeout')?.value || "900", 10); // Default 15 minutes
var sessionTimer = localStorage.getItem("sessionTimer")
    ? parseInt(localStorage.getItem("sessionTimer"), 10)
    : defaultTimeoutSeconds;

function startTimer(timeout, display) {
    var timer = timeout;
    var countdown;

    function updateDisplay() {
        var minutes = parseInt(timer / 60, 10);
        var seconds = parseInt(timer % 60, 10);

        minutes = minutes < 10 ? "0" + minutes : minutes;
        seconds = seconds < 10 ? "0" + seconds : seconds;

        if (display) {
            display.textContent = minutes + ":" + seconds;
        }
    }

    function stopCountdown() {
        clearInterval(countdown);
    }

    countdown = setInterval(function () {
        // Fetch the latest timer value from localStorage
        timer = parseInt(localStorage.getItem("sessionTimer"), 10) || timer;

        // Update the display
        updateDisplay();

        if (timer <= 0) {
            stopCountdown();
            localStorage.removeItem("sessionTimer");
            window.location.href = logoutPath; // Redirect to logout
        } else if (timer <= 15 && askConfirmation) {
            askConfirmation = false; // Prevent duplicate confirmation dialogs
            stopCountdown(); // Pause the timer

            // Show confirmation popup
            $.confirm({
                title: "Session Expiring!",
                content: `Your session is about to expire!<br />Click <b>CONTINUE</b> to stay logged in.`,
                icon: 'fas fa-hourglass-end fa-spin',
                type: 'orange',
                closeIcon: true,
                autoClose: `cancel|${timer * 1000}`,
                buttons: {
                    confirm: {
                        text: "CONTINUE",
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

        // Decrement the timer and store the updated value
        timer--;
        localStorage.setItem("sessionTimer", timer);
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
        var lastTime = localStorage.getItem("lastTime");
        if (lastTime) {
            var elapsed = Math.floor((new Date().getTime() - lastTime) / 1000);

            sessionTimer = parseInt(localStorage.getItem("sessionTimer"), 10) || sessionTimer;
            sessionTimer -= elapsed;

            if (sessionTimer <= 0) {
                sessionTimer = 0;
                localStorage.setItem("sessionTimer", sessionTimer);
                window.location.href = logoutPath; // Logout if session expired
                return;
            }

            localStorage.setItem("sessionTimer", sessionTimer);
        }
    } else {
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
        sessionTimer = parseInt(timeoutElement ? timeoutElement.value : "900", 10); // Default to 15 minutes
    }

    // Start the session timer
    startTimer(sessionTimer, display);

    // Show current time
    showTime();
};
