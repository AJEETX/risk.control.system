$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm  Add New",
                icon: 'fas fa-user-plus',
    
                content: "Are you sure? to add ",
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: " Add New",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = false;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#create-user').attr('disabled', 'disabled');
                            $('#create-user').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add User");

                            $('#create-form').submit();
                            var form = document.getElementById("create-form");
                            if (form) {
                                const formElements = form.getElementsByTagName("*");
                                for (const element of formElements) {
                                    element.disabled = true;
                                    if (element.hasAttribute("readonly")) {
                                        element.classList.remove("valid", "is-valid", "valid-border");
                                        element.removeAttribute("aria-invalid");
                                    }
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

    $("#documentImageInput").on('change', function () {
        var MaxSizeInBytes = 5242880; //5 MB
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
                        document.getElementById('profileImage').src = '/img/no-user.png';
                        document.getElementById('documentImageInput').value = '';
                        $.alert(
                            {
                                title: " Image UPLOAD issue !",
                                content: " <i class='fa fa-upload'></i> Upload Image size limit exceeded. <br />Max file size is 5 MB!",
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
});