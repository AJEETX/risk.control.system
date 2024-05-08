var askConfirmation = true;
var timeoutSeconds = 15;
var logoutPath = "/Account/Logout";
function startTimer(timeout, display) {
	var timer = timeout, minutes, seconds;
	var countdown = setInterval(function () {
		minutes = parseInt(timer / 60, 10);
		seconds = parseInt(timer % 60, 10);

		minutes = minutes < 10 ? "0" + minutes : minutes;
		seconds = seconds < 10 ? "0" + seconds : seconds;

		display.textContent = minutes + ":" + seconds;

		if (timer <= 0) {
			clearInterval(countdown);
			window.location.href = logoutPath;
		}

		else if (timer == timeoutSeconds) {
			$.confirm(
				{
					title: "Session expire!",
					content: `Session to expire! <br /> Click <b> REFRESH </b> to continue... `,
					icon: 'fas fa-hourglass-end fa-spin',
					type: 'orange',
					closeIcon: true,
					autoClose: `cancel|` + timer * 1000 + ``,
					buttons: {
						confirm: {
							text: "REFRESH",
							btnClass: 'btn-warning',
							action: function () {
								window.location.href = window.location.href;
								return;
							}
						},
						cancel: {
							text: "LOGOUT",
							btnClass: 'btn-default',
							action: function () {
								window.location.href = logoutPath;
								return;
							}
						}
					}
				}
			);
				
		}
		timer--;
		
	}, 1000);
}

// Calling showTime function at every second
var logoutTimer = setInterval(showTime, 1000);

// Defining showTime funcion
function showTime() {
	// Getting current time and date
	let time = new Date();
	let hour = time.getHours();
	let min = time.getMinutes();
	let sec = time.getSeconds();
	am_pm = "AM";

	// Setting time for 12 Hrs format
	if (hour >= 12) {
		if (hour > 12) hour -= 12;
		am_pm = "PM";
	} else if (hour == 0) {
		hr = 12;
		am_pm = "AM";
	}

	hour =
		hour < 10 ? "0" + hour : hour;
	min = min < 10 ? "0" + min : min;
	sec = sec < 10 ? "0" + sec : sec;

	let currentTime =
		hour +
		":" +
		min +
		":" +
		sec +
		am_pm;

	// Displaying the time
	var clockTime = document.getElementById("clock");
	// Displaying the time
	clockTime.innerHTML = currentTime;
}
window.onload = function () {
	var display = document.querySelector('#time');
	var timeout = document.getElementById('timeout').value;
	startTimer(timeout, display);
	showTime();
}