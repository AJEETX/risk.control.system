$(document).ready(function () {

    if ($(".selected-case:checked").length) {
        $("#allocate-case").prop('disabled', false);
    }
    else {
        $("#allocate-case").prop('disabled', true);
    }

    // When user checks a radio button, Enable submit button
    $(".selected-case").change(function (e) {
        if ($(this).is(":checked")) {
            $("#allocate-case").prop('disabled', false);
        }
        else {
            $("#allocate-case").prop('disabled', true);
        }
    });

    $('#reselect-case').click(function (e) {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $(this).html("<i class='fas fa-sync fa-spin'></i> REVIEW");
        disableAllInteractiveElements();

        var section = document.getElementById("section");
        if (section) {
            var nodes = section.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $("#documentImageInput").on('change', function () {
        var MaxSizeInBytes = 5242880; //5 MB
        //Get count of selected files
        var countFiles = $(this)[0].files.length;
        var inputElement = $(this);
        var imgPath = $(this)[0].value;
        var extn = imgPath.substring(imgPath.lastIndexOf('.') + 1).toLowerCase();
        const imageElement = $('#policyImage');
        

        var reader = typeof (FileReader);
        if (extn == "png" || extn == "jpg" || extn == "jpeg") {
            if (reader != "undefined") {
                const fileReader = new FileReader();
                var fileSize = $(this)[0].files[0].size;
                if (fileSize > MaxSizeInBytes) {
                    imageElement.attr('src', '/img/no-policy.jpg');
                    inputElement.val('');
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
                else {
                    imageElement.attr('src',window.URL.createObjectURL(this.files[0]));
                    imageElement.attr('data-bs-original-title', 'Additional Document'); // Set the preview image source
                    imageElement.attr('data-original-title', 'Additional Document'); // Set the preview image source
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
            if (countFiles == 0 && extn =='') {
                imageElement.attr('src', '/img/no-policy.jpg');
                inputElement.val('');
                imageElement.attr('data-bs-original-title', 'No Additional Document'); // Set the preview image source
                imageElement.attr('data-original-title', 'No Additional Document'); // Set the preview image source
                $.alert(
                    {
                        title: "Image removed !!",
                        content: "Pls select  image with extension jpg,jpeg, png to upload ! ",
                        icon: 'fas fa-exclamation-triangle',

                        type: 'blue',
                        closeIcon: true,
                        buttons: {
                            cancel: {
                                text: "CLOSE",
                                btnClass: 'btn-info'
                            }
                        }
                    }
                );
            }
            else {
                if (extn != "png" && extn != "jpg" && extn != "jpeg") {
                    $.alert(
                        {
                            title: "FILE UPLOAD TYPE !!",
                            content: "Pls select only image with extension jpg, jpeg, png ! ",
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
                    imageElement.attr('src', '/img/no-policy.jpg');
                    inputElement.val('');
                    imageElement.attr('data-bs-original-title', 'No Additional Document'); // Set the preview image source
                    imageElement.attr('data-original-title', 'No Additional Document'); // Set the preview image source
                }
            }
        }
    });

    let askConfirmation = false;
    $('#supervisor-form').on('submit', function (e) {
        var report = $('#supervisorRemarks').val();

        if (report == '') {
            e.preventDefault();
            $.alert({
                title: "Comments !!!",
                content: "Please enter comments ?",
                icon: 'fas fa-exclamation-triangle',
    
                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "OK",
                        btnClass: 'btn-danger',
                        action: function () {
                            $.alert('Canceled!');
                            $('#supervisorRemarks').focus();
                        }
                    },
                }
            });
        }
        else if (!askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Submit",
                content: "Are you sure?",
                icon: 'far fa-file-alt',
    
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "SUBMIT",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = true;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                           
                            $('#allocate-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> SUBMIT");
                            disableAllInteractiveElements();

                            $('#supervisor-form').submit();

                            var createForm = document.getElementById("supervisor-form");
                            if (createForm) {

                                var nodes = createForm.getElementsByTagName('*');
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