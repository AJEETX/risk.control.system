var askConfirmation = true;

function startTimer(duration, display) {
	var timer = duration, minutes, seconds;
	setInterval(function () {
		minutes = parseInt(timer / 60, 10);
		seconds = parseInt(timer % 60, 10);

		minutes = minutes < 10 ? "0" + minutes : minutes;
		seconds = seconds < 10 ? "0" + seconds : seconds;

		display.textContent = minutes + ":" + seconds;
		if (askConfirmation) {
			if (--timer < 10) {
				askConfirmation = false;
				$.alert(
					{
						title: "Inactivity Session timeout!",
						content: `<i class='fas fa-sync fa-spin'></i> Inactivity session timeout. <br /> <b>` + duration + `</b> seconds!`,
						icon: 'fas fa-exclamation-triangle',
						type: 'red',
						closeIcon: true,
						autoClose: 'cancel|2000',
						onClose: function () {
							// before the modal is hidden.
							window.location.href = "/Dashboard/Index";
						},
						buttons: {
							confirm: {
								text: "REGRESH",
								btnClass: 'btn-success',
								action: function () {
									window.location.href = "/Dashboard/Index";
								}
							},
							cancel: {
								text: "LOGOUT",
								btnClass: 'btn-default',
								action: function () {
									window.location.href = "/account/login";
								}
							}
						}
					}
				);

			}
		}
		
		if (--timer < 0) {
			timer = duration;
			window.location.href = "/account/login";
		}
	}, 1000);
}

window.onload = function () {
	var fiveMinutes = 60*5,
		display = document.querySelector('#time');
	startTimer(fiveMinutes, display);
};


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
	document.getElementById(
		"clock"
	).innerHTML = currentTime;
}

showTime();
