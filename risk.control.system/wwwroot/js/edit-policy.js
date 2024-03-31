$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit Policy",
            content: "Are you sure to edit?",
            columnClass: 'medium',
            icon: 'far fa-file-powerpoint',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Edit Policy",
                    btnClass: 'btn-warning',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('#create-policy').attr('disabled', 'disabled');
                        $('#create-policy').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Policy");

                        form.submit();
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
$(document).ready(function () {
    $("#create-form").validate();
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
                        $.alert(
                            {
                                title: "Image size limit exceeded !",
                                content: "Image size limit exceeded. Max file size is " + MaxSizeInBytes + ' bytes!',
                                icon: 'fas fa-exclamation-triangle',
                                columnClass: 'medium',
                                type: 'red',
                                closeIcon: true,
                                buttons: {
                                    cancel: {
                                        text: "OK",
                                        btnClass: 'btn-danger',
                                        action: function () {
                                            document.getElementById('profileImage').src = '/img/no-policy.jpg';
                                            document.getElementById('documentImageInput').value = '';
                                            $.alert({
                                                title: 'FILE UPLOAD !',
                                                content: 'Max image size allowed :' + MaxSizeInBytes + ' bytes!',
                                                icon: 'fa fa-upload',
                                                columnClass: 'medium',
                                                closeIcon: true,
                                            });
                                        }
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
                        columnClass: 'medium',
                        type: 'red',
                        closeIcon: true,
                        buttons: {
                            cancel: {
                                text: "OK",
                                btnClass: 'btn-danger', action: function () {
                                    $.alert('Try on modern browser!');
                                }
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
                            text: "OK",
                            btnClass: 'btn-danger',
                            action: function () {
                                document.getElementById('policyImage').src = '/img/no-policy.jpg';
                                document.getElementById('documentImageInput').value = '';
                                $.alert({
                                    title: 'FILE UPLOAD TYPE!',
                                    content: 'Pls select only image with extension jpg, png,gif !',
                                    icon: 'fa fa-upload',
                                    columnClass: 'medium',
                                    closeIcon: true,
                                });
                            }
                        }
                    }
                }
            );
        }
    });
});
dateContractId.max = new Date().toISOString().split("T")[0];
dateIncidentId.max = new Date().toISOString().split("T")[0];