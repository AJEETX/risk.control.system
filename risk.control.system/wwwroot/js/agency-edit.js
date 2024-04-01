$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit Agency",
                content: "Are you sure to edit?",
                icon: 'fas fa-building',
                columnClass: 'medium',
                type: 'orange',
                closeIcon: true,
                typeAnimated: true,
                buttons: {
                    confirm: {
                        text: "Edit Agency",
                        btnClass: 'btn-warning',
                        action: function () {
                            askConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('.btn.btn-warning').attr('disabled', 'disabled');
                            $('.btn.btn-warning').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Agency");

                            $('#create-form').submit();
                            var nodes = document.getElementById("create-form").getElementsByTagName('*');
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
    var currentImage = document.getElementById('profileImage').src;

    $("#documentImageInput").on('change', function () {
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
                        if (currentImage.startsWith('https://') && currentImage.endsWith('/img/no-image.png')) {
                            document.getElementById('profileImage').src = '/img/no-image.png';
                            document.getElementById('documentImageInput').value = '';
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
                        document.getElementById('profileImage').src = window.URL.createObjectURL($(this)[0].files[i]);
                    }
                }

            } else {
                $.alert(
                    {
                        title: "Outdated Browser !",
                        content: "This browser does not support FileReader. Try on modern browser!",
                        icon: 'fas fa-exclamation-triangle',
                        columnClass: 'medium',
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
                    columnClass: 'medium',
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
});