$(document).ready(function () {
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(success);
    } else {
        alert("There is Some Problem on your current browser to get Geo Location!");
    }

    function success(position) {
        var coordinates = position.coords;
        $('#agentIdLatitude').val(coordinates.latitude);
        $('#agentIdLongitude').val(coordinates.longitude);

        $('#digitalIdLatitude').val(coordinates.latitude);
        $('#digitalIdLongitude').val(coordinates.longitude);

        $('#documentIdLatitude').val(coordinates.latitude);
        $('#documentIdLongitude').val(coordinates.longitude);

        $('#passportIdLatitude').val(coordinates.latitude);
        $('#passportIdLongitude').val(coordinates.longitude);

        $('#audioLatitude').val(coordinates.latitude);
        $('#audioLongitude').val(coordinates.longitude);

        $('#videoLatitude').val(coordinates.latitude);
        $('#videoLongitude').val(coordinates.longitude);
    }

    //FACE IMAGE
    let askConfirmation = false;
    let askFaceUploadConfirmation = true;
    var currentImage;
    var currentImageEl = document.getElementById('face-Image');
    if (currentImageEl) {
        currentImage = currentImageEl.src;
    }
    $('#digitalImage').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadFaceImageButton');
        val ? fbtn.removeAttr("disabled") : fbtn.attr("disabled");
        var uploadType = $('#digitalImage').val();
        uploadType && (uploadType.endsWith("png") || uploadType.endsWith("jpg") || uploadType.endsWith("jpeg")) ? fbtn.attr("disabled", false) : fbtn.removeAttr("disabled");
    });
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
    $('#upload-face').on('submit', function (e) {
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

                            $('#UploadFaceImageButton').html("<i class='fas fa-sync fa-spin'></i> Uploading");
                            disableAllInteractiveElements();

                            $('#upload-face').submit();

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

    //AGENT IMAGE
    var currentAgentImage;
    var currentAgentImageEl = document.getElementById('agent-Image');
    if (currentAgentImageEl) {
        currentAgentImage = currentAgentImageEl.src;
    }
    $('#agentImage').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadAgentImageButton');
        val ? fbtn.removeAttr("disabled") : fbtn.attr("disabled");
        var uploadType = $('#agentImage').val();
        uploadType && (uploadType.endsWith("png") || uploadType.endsWith("jpg") || uploadType.endsWith("jpeg")) ? fbtn.attr("disabled", false) : fbtn.removeAttr("disabled");
    });
    $("#agentImage").on('change', function () {
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
                            document.getElementById('agent-Image').src = '/img/no-user.png';
                            document.getElementById('agentImage').value = '';
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
                        document.getElementById('agent-Image').src = window.URL.createObjectURL($(this)[0].files[i]);
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
    $('#upload-agent').on('submit', function (e) {
        if (askFaceUploadConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Upload",
                content: "Are you sure to upload Agent Image?",
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

                            $('#UploadAgentImageButton').html("<i class='fas fa-sync fa-spin'></i> Uploading");
                            disableAllInteractiveElements();

                            $('#upload-agent').submit();

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

    //PAN IMAGE
    let askPanUploadConfirmation = true;
    var panImage;
    var panImageEl = document.getElementById('pan-Image');
    if (panImageEl) {
        panImage = panImageEl.src;
    }
    $('#panImage').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadPanImageButton');
        val ? fbtn.removeAttr("disabled") : fbtn.attr("disabled");
        var uploadType = $('#panImage').val();
        uploadType && (uploadType.endsWith("png") || uploadType.endsWith("jpg") || uploadType.endsWith("jpeg")) ? fbtn.attr("disabled", false) : fbtn.removeAttr("disabled");
    });
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
    $('#upload-pan').on('submit', function (e) {
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

                            $('#UploadPanImageButton').html("<i class='fas fa-sync fa-spin'></i> Uploading");
                            disableAllInteractiveElements();

                            $('#upload-pan').submit();
                            
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

    //PASSPORT IMAGE
    let askPassportUploadConfirmation = true;
    var passportImage;
    var passportImageEl = document.getElementById('passport-Image');
    if (passportImageEl) {
        passportImage = passportImageEl.src;
    }

    $('#passportImage').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadPassportImageButton');
        val ? fbtn.removeAttr("disabled") : fbtn.attr("disabled");
        var uploadType = $('#passportImage').val();
        uploadType && (uploadType.endsWith("png") || uploadType.endsWith("jpg") || uploadType.endsWith("jpeg")) ? fbtn.attr("disabled", false) : fbtn.removeAttr("disabled");
    });
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

                            $('#UploadPassportImageButton').html("<i class='fas fa-sync fa-spin'></i> Uploading");
                            disableAllInteractiveElements();

                            $('#upload-passport').submit();
                            
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

    //AUDIO FILE
    let askAudioUploadConfirmation = true;
    
    $('#audioFile').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadAudioButton');
        val ? fbtn.removeAttr("disabled") : fbtn.attr("disabled");
        var uploadType = $('#audioFile').val();
        uploadType && uploadType.endsWith("mp3") ? fbtn.attr("disabled", false) : fbtn.removeAttr("disabled");
    });
    $("#audioFile").on('change', function () {
        var MaxSizeInBytes = 5297152;
        //Get count of selected files
        var countFiles = $(this)[0].files.length;

        var imgPath = $(this)[0].value;
        var extn = imgPath.substring(imgPath.lastIndexOf('.') + 1).toLowerCase();

        if (extn == "mp3") {
            if (typeof (FileReader) != "undefined") {

                //loop for each file selected for uploaded.
                for (var i = 0; i < countFiles; i++) {
                    var fileSize = $(this)[0].files[i].size;
                    if (fileSize > MaxSizeInBytes) {
                         var btn = $('#UploadAudioButton');
                        btn.attr("disabled");
                        $.alert(
                            {
                                title: " UPLOAD issue !",
                                content: " <i class='fa fa-upload'></i> Upload File size limit exceeded. <br />Max file size is 1 MB!",
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
                    content: "Pls select only image with extension mp3 ! ",
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
    $('#upload-audio').on('submit', function (e) {
        if (askAudioUploadConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Upload",
                content: "Are you sure to upload Audio?",
                icon: 'fa fa-upload',

                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Upload",
                        btnClass: 'btn-success',
                        action: function () {
                            askAudioUploadConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#UploadAudioButton').html("<i class='fas fa-sync fa-spin'></i> Uploading");
                            disableAllInteractiveElements();

                            $('#upload-audio').submit();
                           
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

    //VIDEO FILE
    let askVideoUploadConfirmation = true;

    $('#videoFile').on("change", function () {
        var val = $(this).val(),
            fbtn = $('#UploadVideoButton');
        val ? fbtn.removeAttr("disabled") : fbtn.attr("disabled");
        var uploadType = $('#videoFile').val();
        uploadType && uploadType.endsWith("mp4") ? fbtn.attr("disabled", false) : fbtn.removeAttr("disabled");
    });
    $("#videoFile").on('change', function () {
        var MaxSizeInBytes = 5297152;
        //Get count of selected files
        var countFiles = $(this)[0].files.length;

        var imgPath = $(this)[0].value;
        var extn = imgPath.substring(imgPath.lastIndexOf('.') + 1).toLowerCase();

        if (extn == "mp4") {
            if (typeof (FileReader) != "undefined") {

                //loop for each file selected for uploaded.
                for (var i = 0; i < countFiles; i++) {
                    var fileSize = $(this)[0].files[i].size;
                    if (fileSize > MaxSizeInBytes) {
                        var btn = $('#UploadVideoButton');
                        btn.attr("disabled");
                        $.alert(
                            {
                                title: " UPLOAD issue !",
                                content: " <i class='fa fa-upload'></i> Upload File size limit exceeded. <br />Max file size is 5 MB!",
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
                    content: "Pls select only image with extension mp4 ! ",
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
    $('#upload-video').on('submit', function (e) {
        if (askVideoUploadConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Upload",
                content: "Are you sure to upload Video?",
                icon: 'fa fa-upload',

                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Upload",
                        btnClass: 'btn-success',
                        action: function () {
                            askVideoUploadConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#UploadVideoButton').html("<i class='fas fa-sync fa-spin'></i> Uploading");
                            disableAllInteractiveElements();

                            $('#upload-video').submit();
                           
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

                            $('#submit-case').html("<i class='fas fa-sync fa-spin'></i> SUBMIT");
                            disableAllInteractiveElements();

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
var questionDate = document.getElementById("question4");
if (questionDate) {
    questionDate.max = new Date().toISOString().split("T")[0];
}

document.addEventListener("DOMContentLoaded", function () {

    // Reference to the modal and close button
    var termsModal = document.getElementById('termsModal');
    var closeTermsButton = document.getElementById('closeterms');
    // Select all elements with the class 'termsLink'
    var termsLinks = document.querySelectorAll('.termsLink');

    // Add a click event listener to each element
    if (termsLinks) {
            termsLinks.forEach(function (termsLink) {
            termsLink.addEventListener('click', function (e) {
                e.preventDefault(); // Prevent default link behavior (i.e., not navigating anywhere)

                // Show the terms modal
                var termsModal = document.querySelector('#termsModal');
                termsModal.classList.remove('hidden-section');
                termsModal.classList.add('show');
            });
        });
    }
    // Close the modal when clicking the close button
    if (closeTermsButton) {
            closeTermsButton.addEventListener('click', function () {
            termsModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
            termsModal.classList.remove('show'); // Remove the 'show' class to hide the modal
        });
    }

    // Optionally, you can close the modal if clicked outside the modal content
    window.addEventListener('click', function (e) {
        if (e.target === termsModal) {
            termsModal.classList.add('hidden-section'); // Remove the 'show' class to hide the modal
            termsModal.classList.remove('show'); // Close the modal if clicked outside
        }
    });
});

document.addEventListener("click", function (e) {
    if (e.target.classList.contains("btn-clear-date")) {
        const input = e.target.closest(".position-relative").querySelector("input[type='date']");
        if (input) {
            input.value = ""; // Clear the value
            input.blur();     // Remove focus to hide calendar
        }
    }
});


document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
        const activeElement = document.activeElement;
        if (activeElement && activeElement.type === "date") {
            activeElement.blur(); // Closes the date picker
        }
    }
});

document.querySelectorAll(".delete-question-btn").forEach(btn => {
    btn.addEventListener("click", () => {
        if (document.activeElement && document.activeElement.type === "date") {
            document.activeElement.blur();
        }
    });
});

document.addEventListener("click", function (event) {
    const activeElement = document.activeElement;

    // Check if the active element is a date input
    if (activeElement && activeElement.type === "date") {
        // Check if the click was outside of that input
        if (!activeElement.contains(event.target)) {
            activeElement.blur(); // This will close the date picker
        }
    }
});

document.addEventListener("DOMContentLoaded", function () {
    const questionType = document.getElementById("questionType");
    const optionsGroup = document.getElementById("optionsGroup");
    if (questionType && optionsGroup) {
        function toggleOptions() {
            const shouldShow = questionType.value === "dropdown" || questionType.value === "radio";
            optionsGroup.classList.toggle("hidden", !shouldShow);
        }

        questionType.addEventListener("change", toggleOptions);
        toggleOptions(); // Run once on load
    }
});
