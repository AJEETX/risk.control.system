$(document).ready(function () {
    $("#postedFile").on('change', function () {
        var MaxSizeInBytes = 1097152;
        //Get count of selected files
        var countFiles = $(this)[0].files.length;

        var imgPath = $(this)[0].value;
        var extn = imgPath.substring(imgPath.lastIndexOf('.') + 1).toLowerCase();

        if (extn == "zip") {
            if (typeof (FileReader) != "undefined") {

                //loop for each file selected for uploaded.
                for (var i = 0; i < countFiles; i++) {
                    var fileSize = $(this)[0].files[i].size;
                    if (fileSize > MaxSizeInBytes) {
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
                    content: "Pls only select file with extension zip ! ",
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
    let askFileUploadConfirmation = true;
    let askConfirmation = false;

    function handleUploadConfirmation(formId, buttonId) {
        $(formId).on('submit', function (event) {
            if (askFileUploadConfirmation) {
                event.preventDefault();
                $.confirm({
                    title: "Confirm File Upload",
                    content: "Are you sure to Upload?",
                    icon: 'fas fa-upload',
                    type: 'green',
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: "File Upload",
                            btnClass: 'btn-success',
                            action: function () {
                                askFileUploadConfirmation = false;

                                $("body").addClass("submit-progress-bg");
                                setTimeout(function () {
                                    $(".submit-progress").removeClass("hidden");
                                }, 1);

                                $(buttonId).html("<i class='fas fa-sync fa-spin'></i> Uploading");
                                disableAllInteractiveElements();
                                $(formId).submit();

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
    }

    // Apply confirmation to both forms
    handleUploadConfirmation("#upload-claims", "#UploadFileButton");
});
