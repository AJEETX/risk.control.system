$(document).ready(function () {
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

        $('#passportIdLatitude').val(coordinates.latitude);
        $('#passportIdLongitude').val(coordinates.longitude);
    }
    var currentImage;
    var currentImageEl = document.getElementById('face-Image');
    if (currentImageEl) {
        currentImage = currentImageEl.src;
    }

    $("#digitalImage").on('change', function () {
        var MaxSizeInBytes = 2097152;
        //Get count of selected files
        var countFiles = $(this)[0].files.length;

        var imgPath = $(this)[0].value;
        var extn = imgPath.substring(imgPath.lastIndexOf('.') + 1).toLowerCase();

        if (extn == "gif" || extn == "png" || extn == "jpg" || extn == "jpeg") {
            if (typeof (FileReader) != "undefined") {

                //loop for each file selected for uploaded.
                for (var i = 0; i < countFiles; i++) {
                    var fileSize = $(this)[0].files[i].size;
                    if (fileSize > MaxSizeInBytes) {
                        if (currentImage && currentImage.startsWith('https://') && currentImage.endsWith('/img/no-user.png')) {
                            document.getElementById('face-Image').src = '/img/no-user.png';
                            document.getElementById('digitalImage').value = '';
                        }
                        $.alert(
                            {
                                title: " Image UPLOAD issue !",
                                content: " <i class='fa fa-upload'></i> Upload Image size limit exceeded. <br />Max file size is 2 MB!",
                                icon: 'fas fa-exclamation-triangle',
                                type: 'red',
                                closeIcon: true,
                                buttons: {
                                    cancel: {
                                        text: "CLOSE",
                                        btnClass: 'btn-danger'
                                    }
                                }
                            }
                        );
                    }
                    else {
                        document.getElementById('face-Image').src = window.URL.createObjectURL($(this)[0].files[i]);
                    }
                }

            } else {
                $.alert(
                    {
                        title: "Outdated Browser !",
                        content: "This browser does not support FileReader. Try on modern browser!",
                        icon: 'fas fa-exclamation-triangle',
            
                        type: 'red',
                        closeIcon: true,
                        buttons: {
                            cancel: {
                                text: "CLOSE",
                                btnClass: 'btn-danger'
                            }
                        }
                    }
                );
            }
        } else {
            $.alert(
                {
                    title: "FILE UPLOAD TYPE !!",
                    content: "Pls select only image with extension jpg, png,gif ! ",
                    icon: 'fas fa-exclamation-triangle',
        
                    type: 'red',
                    closeIcon: true,
                    buttons: {
                        cancel: {
                            text: "CLOSE",
                            btnClass: 'btn-danger'
                        }
                    }
                }
            );
        }
    });

    var panImage;
    var panImageEl = document.getElementById('pan-Image');
    if (panImageEl) {
        panImage = panImageEl.src;
    }
    $("#panImage").on('change', function () {
        var MaxSizeInBytes = 2097152;
        //Get count of selected files
        var countFiles = $(this)[0].files.length;

        var imgPath = $(this)[0].value;
        var extn = imgPath.substring(imgPath.lastIndexOf('.') + 1).toLowerCase();

        if (extn == "gif" || extn == "png" || extn == "jpg" || extn == "jpeg") {
            if (typeof (FileReader) != "undefined") {

                //loop for each file selected for uploaded.
                for (var i = 0; i < countFiles; i++) {
                    var fileSize = $(this)[0].files[i].size;
                    if (fileSize > MaxSizeInBytes) {
                        if (panImage && panImage.startsWith('https://') && panImage.endsWith('/img/no-image.png')) {
                            document.getElementById('pan-Image').src = '/img/no-image.png';
                            document.getElementById('panImage').value = '';
                        }
                        $.alert(
                            {
                                title: " Image UPLOAD issue !",
                                content: " <i class='fa fa-upload'></i> Upload Image size limit exceeded. <br />Max file size is 2 MB!",
                                icon: 'fas fa-exclamation-triangle',
                                type: 'red',
                                closeIcon: true,
                                buttons: {
                                    cancel: {
                                        text: "CLOSE",
                                        btnClass: 'btn-danger'
                                    }
                                }
                            }
                        );
                    }
                    else {
                        document.getElementById('pan-Image').src = window.URL.createObjectURL($(this)[0].files[i]);
                    }
                }

            } else {
                $.alert(
                    {
                        title: "Outdated Browser !",
                        content: "This browser does not support FileReader. Try on modern browser!",
                        icon: 'fas fa-exclamation-triangle',
            
                        type: 'red',
                        closeIcon: true,
                        buttons: {
                            cancel: {
                                text: "CLOSE",
                                btnClass: 'btn-danger'
                            }
                        }
                    }
                );
            }
        } else {
            $.alert(
                {
                    title: "FILE UPLOAD TYPE !!",
                    content: "Pls select only image with extension jpg, png,gif ! ",
                    icon: 'fas fa-exclamation-triangle',
        
                    type: 'red',
                    closeIcon: true,
                    buttons: {
                        cancel: {
                            text: "CLOSE",
                            btnClass: 'btn-danger'
                        }
                    }
                }
            );
        }
    });

    var passportImage;
    var passportImageEl = document.getElementById('passport-Image');
    if (passportImageEl) {
        passportImage = passportImageEl.src;
    }
    $("#passportImage").on('change', function () {
        var MaxSizeInBytes = 2097152;
        //Get count of selected files
        var countFiles = $(this)[0].files.length;

        var imgPath = $(this)[0].value;
        var extn = imgPath.substring(imgPath.lastIndexOf('.') + 1).toLowerCase();

        if (extn == "gif" || extn == "png" || extn == "jpg" || extn == "jpeg") {
            if (typeof (FileReader) != "undefined") {

                //loop for each file selected for uploaded.
                for (var i = 0; i < countFiles; i++) {
                    var fileSize = $(this)[0].files[i].size;
                    if (fileSize > MaxSizeInBytes) {
                        if (passportImage && passportImage.startsWith('https://') && passportImage.endsWith('/img/no-image.png')) {
                            document.getElementById('passport-Image').src = '/img/no-image.png';
                            document.getElementById('passportImage').value = '';
                        }
                        $.alert(
                            {
                                title: " Image UPLOAD issue !",
                                content: " <i class='fa fa-upload'></i> Upload Image size limit exceeded. <br />Max file size is 2 MB!",
                                icon: 'fas fa-exclamation-triangle',
                                type: 'red',
                                closeIcon: true,
                                buttons: {
                                    cancel: {
                                        text: "CLOSE",
                                        btnClass: 'btn-danger'
                                    }
                                }
                            }
                        );
                    }
                    else {
                        document.getElementById('passport-Image').src = window.URL.createObjectURL($(this)[0].files[i]);
                    }
                }

            } else {
                $.alert(
                    {
                        title: "Outdated Browser !",
                        content: "This browser does not support FileReader. Try on modern browser!",
                        icon: 'fas fa-exclamation-triangle',

                        type: 'red',
                        closeIcon: true,
                        buttons: {
                            cancel: {
                                text: "CLOSE",
                                btnClass: 'btn-danger'
                            }
                        }
                    }
                );
            }
        } else {
            $.alert(
                {
                    title: "FILE UPLOAD TYPE !!",
                    content: "Pls select only image with extension jpg, png,gif ! ",
                    icon: 'fas fa-exclamation-triangle',

                    type: 'red',
                    closeIcon: true,
                    buttons: {
                        cancel: {
                            text: "CLOSE",
                            btnClass: 'btn-danger'
                        }
                    }
                }
            );
        }
    });
    let askFaceUploadConfirmation = true;
    $('#upload-face').on('submit',function (e) {
        if (askFaceUploadConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Upload",
                content: "Are you sure to upload Face Image?",
                icon: 'fa fa-upload',
    
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
                            $('html a').css('pointer-events', 'none');
                            $('html a').css('cursor', 'none');
                            $('html button').attr('disabled', true);
                            $('#back').attr('disabled', true);

                            $('html a *, html button *').css('pointer-events', 'none');

                            var article = document.getElementById("article");
                            if (article) {
                                var nodes = article.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
                                }
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

    $('#passportImage').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadPassportImageButton');
        val ? fbtn.removeAttr("disabled") : fbtn.attr("disabled");
    });

    let askPanUploadConfirmation = true;

    $('#upload-pan').on('submit',function (e) {
        if (askPanUploadConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Upload",
                content: "Are you sure to upload PAN Card?",
                icon: 'fa fa-upload',
    
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

                            var article = document.getElementById("article");
                            if (article) {
                                var nodes = article.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
                                }
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

    let askPassportUploadConfirmation = true;

    $('#upload-passport').on('submit', function (e) {
        if (askPassportUploadConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Upload",
                content: "Are you sure to upload Passport?",
                icon: 'fa fa-upload',

                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Upload",
                        btnClass: 'btn-success',
                        action: function () {
                            askPassportUploadConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#UploadPassportImageButton').attr('disabled', 'disabled');
                            $('#UploadPassportImageButton').html("<i class='fas fa-sync fa-spin'></i> Uploading");

                            $('#upload-passport').submit();
                            $('#back').attr('disabled', 'disabled');

                            $('html *').css('cursor', 'not-allowed');
                            $('html a *, html button *').css('pointer-events', 'none')

                            var article = document.getElementById("article");
                            if (article) {
                                var nodes = article.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
                                }
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
                title: "Confirm SUBMIT",
                content: "Are you sure?",
                icon: 'fa fa-binoculars',
    
                type: 'red',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "SUBMIT",
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
                            $('#submit-case').html("<i class='fas fa-sync fa-spin'></i> SUBMIT");
                            $('#create-form').submit();

                            var article = document.getElementById("article");
                            if (article) {
                                var nodes = article.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
                                }
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