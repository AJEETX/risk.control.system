var askConfirmation = true;
var alertTimeInSeconds = 15;
var alertImeMilliSeconds = alertTimeInSeconds * 1000;
var idleTimeDuration = 300;

function startTimer(duration, display) {
	var timer = duration, minutes, seconds;
	setInterval(function () {
		minutes = parseInt(timer / 60, 10);
		seconds = parseInt(timer % 60, 10);

		minutes = minutes < 10 ? "0" + minutes : minutes;
		seconds = seconds < 10 ? "0" + seconds : seconds;

		display.textContent = minutes + ":" + seconds;
		if (--timer < alertTimeInSeconds) {
			if (askConfirmation) {
				askConfirmation = false;
				$.alert(
					{
						title: "Idle Session timeout!",
						content: `Your idle time out to expire! <br /> Click <b> REFRESH </b> to continue... `,
						icon: 'fas fa-hourglass-end fa-spin',
						type: 'orange',
						closeIcon: true,
						autoClose: `cancel|` + alertImeMilliSeconds + ``,
						buttons: {
							confirm: {
								text: "REFRESH",
								btnClass: 'btn-warning',
								action: function () {
									window.location.href = window.location.pathname;
									return;
								}
							},
							cancel: {
								text: "LOGOUT",
								btnClass: 'btn-default',
								action: function () {
									window.location.href = "/account/logout";
									return;
								}
							}
						}
					}
				);
			}
				
		} else if (timer < 0) {
			timer = duration;
			window.location.href = "/account/logout";
		}
		
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
	startTimer(idleTimeDuration, display);
	showTime();
}
