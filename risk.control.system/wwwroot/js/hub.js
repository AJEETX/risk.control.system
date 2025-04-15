var connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .build();

connection.start().catch(err => console.error(err));
const currentPageUrl = window.location.href;
const refreshSessionPath = "/Account/KeepSessionAlive"; // Path to refresh session

// Show idle warning
connection.on("ReceiveIdleWarning", function () {
    $.confirm({
        title: 'You are Idle!',
        content: 'You will be logged out in 1 minute unless you take action.',
        type: 'orange',
        buttons: {
            Stay: function () {
                // Notify the server that the user is still active
                fetch(refreshSessionPath, {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    credentials: "include", // Include cookies in the request
                    body: JSON.stringify({ currentPage: currentPageUrl }), // Include the current page URL
                });
            }
        }
    });
});

// Handle forced logout
connection.on("ReceiveForceLogout", function () {
    $.confirm({
        title: 'Session Expired!',
        content: 'You have been logged out due to inactivity.',
        type: 'red',
        buttons: {
            OK: {
                btnClass: 'btn-red',
                action: function () {
                    window.location.href = "/Account/Logout";
                }
            }
        }
    });

    // Auto-logout after 10 seconds
    setTimeout(() => {
        window.location.href = "/Account/Logout";
    }, 10000);
});