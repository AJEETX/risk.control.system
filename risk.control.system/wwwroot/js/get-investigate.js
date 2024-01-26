$(document).ready(function () {
    $('#postedFile').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadFileButton');
        val ? fbtn.removeAttr("disabled") : fbtn.attr("disabled");
    });

    $('#UploadFileButton').click(function () {
        //If the checkbox is checked.
        var report = $('#remarks').val();
        if ($(this).is(':checked') && report != '') {
            //Enable the submit button.
            $('#submit-case').attr("disabled", false);
        } else {
            //If it is not checked, disable the button.
            $('#submit-case').attr("disabled", true);
        }
    });
    $('#audio-start').click(function (e) {
        e.preventDefault();
    });
    $('#audio-stop').click(function (e) {
        e.preventDefault();
    });
    $('#audio-play').click(function (e) {
        e.preventDefault();
    });
    $('#video-play').click(function (e) {
        e.preventDefault();
    });
    $('#video-stop').click(function (e) {
        e.preventDefault();
    });
    $('#video-play').click(function (e) {
        e.preventDefault();
    });
    $('#terms_and_conditions').click(function () {
        //If the checkbox is checked.
        var report = $('#remarks').val();
        if ($(this).is(':checked') && report != '') {
            //Enable the submit button.
            $('#submit-case').attr("disabled", false);
        } else {
            //If it is not checked, disable the button.
            $('#submit-case').attr("disabled", true);
        }
    });

    $('#remarks').on('keydown', function () {
        var report = $('#remarks').val();
        if ($('#terms_and_conditions').is(':checked') && report != '') {
            //Enable the submit button.
            $('#submit-case').attr("disabled", false);
        } else {
            //If it is not checked, disable the button.
            $('#submit-case').attr("disabled", true);
        }
    })
    $('#remarks').on('blur', function () {
        var report = $('#remarks').val();
        if ($('#terms_and_conditions').is(':checked') && report != '') {
            //Enable the submit button.
            $('#submit-case').attr("disabled", false);
        } else {
            //If it is not checked, disable the button.
            $('#submit-case').attr("disabled", true);
        }
    })
    let askConfirmation = false;
    $('#create-form').on('submit', function (e) {
        var report = $('#remarks').val();

        if (report == '') {
            e.preventDefault();
            $.alert({
                title: "Report submission !!!",
                content: "Please enter remarks ?",
                icon: 'fas fa-exclamation-triangle',
                columnClass: 'medium',
                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "OK",
                        btnClass: 'btn-danger',
                        action: function () {
                            $.alert('Canceled!');
                            $('#remarks').focus();
                        }
                    }
                }
            });
        }
        else if (!askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Report submission",
                content: "Are you sure?",
                icon: 'fa fa-binoculars',
                columnClass: 'medium',
                type: 'red',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Submit",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = true;
                            $('#create-form').submit();
                        }
                    },
                    cancel: {
                        text: "Cancel",
                        btnClass: 'btn-default'
                    }
                }
            });
        }
    });
});

const startButton = document.getElementById('audio-start');
const stopButton = document.getElementById('audio-stop');
const playButton = document.getElementById('audio-play');
let output = document.getElementById('audio-output');
let audioRecorder;
let audioChunks = [];
navigator.mediaDevices.getUserMedia({ audio: true })
    .then(stream => {
        // Initialize the media recorder object
        audioRecorder = new MediaRecorder(stream);

        // dataavailable event is fired when the recording is stopped
        audioRecorder.addEventListener('dataavailable', e => {
            audioChunks.push(e.data);
        });

        // start recording when the start button is clicked
        startButton.addEventListener('click', (e) => {
            e.preventDefault();

            audioChunks = [];
            audioRecorder.start();
            output.innerHTML = 'Recording started! Speak now.';
        });

        // stop recording when the stop button is clicked
        stopButton.addEventListener('click', (e) => {
            e.preventDefault();
            audioRecorder.stop();
            output.innerHTML = 'Recording stopped! Click on the play button to play the recorded audio.';
        });

        // play the recorded audio when the play button is clicked
        playButton.addEventListener('click', (e) => {
            e.preventDefault();
            const blobObj = new Blob(audioChunks, { type: 'audio/webm' });
            const audioUrl = URL.createObjectURL(blobObj);
            const audio = new Audio(audioUrl);
            audio.play();
            output.innerHTML = 'Playing the recorded audio!';
        });
    }).catch(err => {
        // If the user denies permission to record audio, then display an error.
        console.log('Error: ' + err);
    });


//const videostartButton = document.getElementById('video-start');
//const videostopButton = document.getElementById('video-stop');
//const videoplayButton = document.getElementById('video-play');
//let videoOutput = document.getElementById('video-output');
//let videoRecorder;
//let videoChunks = [];
//navigator.mediaDevices.getUserMedia({ audio: true })
//    .then(stream => {
//        // Initialize the media recorder object
//        videoRecorder = new MediaRecorder(stream);

//        // dataavailable event is fired when the recording is stopped
//        videoRecorder.addEventListener('dataavailable', e => {
//            videoChunks.push(e.data);
//        });

//        // start recording when the start button is clicked
//        vstartButton.addEventListener('click', (e) => {
//            e.preventDefault();

//            videoChunks = [];
//            videoRecorder.start();
//            videoOutput.innerHTML = 'Recording started! Speak now.';
//        });

//        // stop recording when the stop button is clicked
//        vstopButton.addEventListener('click', (e) => {
//            e.preventDefault();
//            videoRecorder.stop();
//            videoOutput.innerHTML = 'Recording stopped! Click on the play button to play the recorded audio.';
//        });

//        // play the recorded audio when the play button is clicked
//        vplayButton.addEventListener('click', (e) => {
//            e.preventDefault();
//            const blobObj = new Blob(videoChunks, { type: 'audio/webm' });
//            const audioUrl = URL.createObjectURL(blobObj);
//            const audio = new Video(audioUrl);
//            audio.play();
//            videoOutput.innerHTML = 'Playing the recorded audio!';
//        });
//    }).catch(err => {
//        // If the user denies permission to record audio, then display an error.
//        console.log('Error: ' + err);
//    });