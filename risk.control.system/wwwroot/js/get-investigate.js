﻿$(document).ready(function () {
    let askConfirmation = false;
    $('#digitalImage').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadFaceImageButton');
        val ? fbtn.removeAttr("disabled") : fbtn.attr("disabled");
    });
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(success);
    } else {
        alert("There is Some Problem on your current browser to get Geo Location!");
    }

    function success(position) {
        var coordinates = position.coords;
        $('#digitalIdLatitude').val(coordinates.latitude);
        $('#digitalIdLongitude').val(coordinates.longitude);

        $('#documentIdLatitude').val(coordinates.latitude);
        $('#documentIdLongitude').val(coordinates.longitude);
    }
    let askFaceUploadConfirmation = true;
    $('#UploadFaceImageButton').click(function (e) {
        if (askFaceUploadConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Photo Image Upload",
                content: "Are you sure to upload?",
                icon: 'fa fa-upload',
                columnClass: 'medium',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Upload",
                        btnClass: 'btn-success',
                        action: function () {
                            askFaceUploadConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#UploadFaceImageButton').attr('disabled', 'disabled');
                            $('#UploadFaceImageButton').html("<i class='fas fa-sync fa-spin'></i> Uploading");

                            $('#upload-face').submit();
                            $('html *').css('cursor', 'not-allowed');
                            $('#back').attr('disabled', 'disabled');

                            $('html a *, html button *').css('pointer-events', 'none');

                            var nodes = document.getElementById("article").getElementsByTagName('*');
                            for (var i = 0; i < nodes.length; i++) {
                                nodes[i].disabled = true;
                            }
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

    $('#panImage').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadPanImageButton');
        val ? fbtn.removeAttr("disabled") : fbtn.attr("disabled");
    });

    let askPanUploadConfirmation = true;

    $('#UploadPanImageButton').click(function (e) {
        if (askPanUploadConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm PAN Image Upload",
                content: "Are you sure to upload?",
                icon: 'fa fa-upload',
                columnClass: 'medium',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Upload",
                        btnClass: 'btn-success',
                        action: function () {
                            askPanUploadConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#UploadPanImageButton').attr('disabled', 'disabled');
                            $('#UploadPanImageButton').html("<i class='fas fa-sync fa-spin'></i> Uploading");

                            $('#upload-pan').submit();
                            $('#back').attr('disabled', 'disabled');

                            $('html *').css('cursor', 'not-allowed');
                            $('html a *, html button *').css('pointer-events', 'none')

                            var nodes = document.getElementById("article").getElementsByTagName('*');
                            for (var i = 0; i < nodes.length; i++) {
                                nodes[i].disabled = true;
                            }
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

    $('#terms_and_conditions').click(function () {
        //If the checkbox is checked.
        var report = $('#remarks').val();
        if ($(this).is(':checked') && report != '') {
            //Enable the submit button.
            $('#submit-case').attr("disabled", false);
            $('#questionaire').css('background-color', 'green');
            $('#questionaire-border').addClass('border-success');
        } else {
            //If it is not checked, disable the button.
            $('#submit-case').attr("disabled", true);
            $('#questionaire').css('background-color', 'grey');
            $('#questionaire-border').removeClass('border-success');
        }
    });

    $('#remarks').on('keydown', function () {
        var report = $('#remarks').val();
        if ($('#terms_and_conditions').is(':checked') && report != '') {
            //Enable the submit button.
            $('#submit-case').attr("disabled", false);
            $('#questionaire').css('background-color', 'green');
            $('#questionaire-border').addClass('border-success');

        } else {
            //If it is not checked, disable the button.
            $('#submit-case').attr("disabled", true);
            $('#questionaire').css('background-color', 'grey');
            $('#questionaire-border').removeClass('border-success');
        }
    })
    $('#remarks').on('blur', function () {
        var report = $('#remarks').val();
        if ($('#terms_and_conditions').is(':checked') && report != '') {
            //Enable the submit button.
            $('#submit-case').attr("disabled", false);
            $('#questionaire').css('background-color', 'green');
            $('#questionaire-border').addClass('border-success');

        } else {
            //If it is not checked, disable the button.
            $('#submit-case').attr("disabled", true);
            $('#questionaire').css('background-color', 'grey');
            $('#questionaire-border').removeClass('border-success');

        }
    })
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

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#submit-case').attr('disabled', 'disabled');
                            $('#submit-case').html("<i class='fas fa-sync fa-spin'></i> Submit");
                            $('#create-form').submit();

                            var nodes = document.getElementById("body").getElementsByTagName('*');
                            for (var i = 0; i < nodes.length; i++) {
                                nodes[i].disabled = true;
                            }
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

question4.max = new Date().toISOString().split("T")[0];

//var nodes = document.getElementById("audio-video").getElementsByTagName('*');
//for (var i = 0; i < nodes.length; i++) {
//    nodes[i].disabled = true;
//}
//const startButton = document.getElementById('audio-start');
//const stopButton = document.getElementById('audio-stop');
//const playButton = document.getElementById('audio-play');
//let output = document.getElementById('audio-output');
//let audioRecorder;
//let audioChunks = [];
//navigator.mediaDevices.getUserMedia({ audio: true })
//    .then(stream => {
//        // Initialize the media recorder object
//        audioRecorder = new MediaRecorder(stream);

//        // dataavailable event is fired when the recording is stopped
//        audioRecorder.addEventListener('dataavailable', e => {
//            audioChunks.push(e.data);
//        });

//        // start recording when the start button is clicked
//        startButton.addEventListener('click', (e) => {
//            e.preventDefault();

//            audioChunks = [];
//            audioRecorder.start();
//            output.innerHTML = 'Recording started! Speak now.';
//        });

//        // stop recording when the stop button is clicked
//        stopButton.addEventListener('click', (e) => {
//            e.preventDefault();
//            audioRecorder.stop();
//            output.innerHTML = 'Recording stopped! Click on the play button to play the recorded audio.';
//        });

//        // play the recorded audio when the play button is clicked
//        playButton.addEventListener('click', (e) => {
//            e.preventDefault();
//            const blobObj = new Blob(audioChunks, { type: 'audio/webm' });
//            const audioUrl = URL.createObjectURL(blobObj);
//            const audio = new Audio(audioUrl);
//            audio.play();
//            output.innerHTML = 'Playing the recorded audio!';
//        });
//    }).catch(err => {
//        // If the user denies permission to record audio, then display an error.
//        console.log('Error: ' + err);
//    });

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