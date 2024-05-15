$(document).ready(function () {
    let askConfirmation = false;
    let approve= false;
    let review = false;
    let reject = false;

    var currentImageElement = document.getElementById('documentImage0');
    var currentImage;
    if (currentImageElement) {
        currentImage = currentImageElement.src;
    }

    $("#document").on('change', function () {
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
                            document.getElementById('documentImage0').src = '/img/no-image.png';
                            document.getElementById('document').value = '';
                        }
                        $.alert(
                            {
                                title: " File UPLOAD issue !",
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
                        document.getElementById('documentImage0').src = window.URL.createObjectURL($(this)[0].files[i]);
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


    $('#create-form').on('submit', function (e) {
        var report = $('#assessorRemarks').val();

        if (report == '') {
            e.preventDefault();
            $.alert({
                title: "Claim Remarks !!!",
                content: "Please enter remarks?",
                icon: 'fas fa-exclamation-triangle',
    
                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "OK",
                        btnClass: 'btn-danger', action: function () {
                            $.alert('Canceled!');
                            $('#assessorRemarks').focus();
                        }
                    }
                }
            });
        }
        else if (!askConfirmation && approve && $('#assessorRemarkType').val() == 'OK') {
            e.preventDefault();
            $.confirm({
                title: "Confirm APPROVE",
                content: "Are you sure?",
                icon: 'far fa-thumbs-up',
    
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "APPROVE",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = true;
                            approve = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#approve-case').attr('disabled', 'disabled');
                            $('#approve-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> APPROVE");
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
        else if (!askConfirmation && review && $('#assessorRemarkType').val() == 'REVIEW') {
            e.preventDefault();
            $.confirm({
                title: "Confirm REVIEW",
                content: "Are you sure?",
                icon: 'fas fa-sync',
    
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "REVIEW",
                        btnClass: 'btn-warning',
                        action: function () {
                            askConfirmation = true;
                            review = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#approve-case').attr('disabled', 'disabled');
                            $('#approve-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i>  REVIEW");
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
        else if (!askConfirmation && reject && $('#assessorRemarkType').val() == 'REJECT') {
            e.preventDefault();
            $.confirm({
                title: "Confirm REJECT",
                content: "Are you sure?",
                icon: 'far fa-thumbs-down',

                type: 'red',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "REJECT",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = true;
                            reject = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#approve-case').attr('disabled', 'disabled');
                            $('#approve-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i>  REJECT");
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

    $('#query-form').on('submit', function (e) {
        var enquiry = $('#description').val();

        if (enquiry == '') {
            e.preventDefault();
            $.alert({
                title: "Enquiry Detail !!!",
                content: "Please enter enquiry information?",
                icon: 'fas fa-exclamation-triangle',

                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "OK",
                        btnClass: 'btn-danger', action: function () {
                            $.alert('Canceled!');
                            $('#description').focus();
                        }
                    }
                }
            });
        }
        else if (!askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Enquiry",
                content: "Are you sure?",
                icon: 'far fa-thumbs-up',

                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Send Enquiry",
                        btnClass: 'btn-warning',
                        action: function () {
                            askConfirmation = true;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#send-query').attr('disabled', 'disabled');
                            $('#send-query').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Enquiring...");
                            $('#query-form').submit();

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

    $('#approve-case').click(function () {
        //If the checkbox is checked.
        var report = $('#assessorRemarks').val();
        var rejectChecked = $('#flexRadioDefault1').is(':checked');
        var reviewChecked = $('#flexRadioDefault2').is(':checked');
        var approvedChecked = $('#flexRadioDefault3').is(':checked');

        if (report != '' && approvedChecked) {
            $('#assessorRemarkType').val('OK');
            approve = true;
        }
        else if (report != '' && reviewChecked) {
            $('#assessorRemarkType').val('REVIEW');
            review = true;
        }
        else if (report != '' && rejectChecked) {
            reject = true;
            $('#assessorRemarkType').val('REJECT');
        }
    });

});

function showenquiry() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('body').attr('disabled', 'disabled');
    $('#enquire-case').html("<i class='fas fa-sync fa-spin'></i> ENQUIRE");

    $('html *').css('cursor', 'not-allowed');
    $('#enquire-case').css('pointer-events', 'none');

    var nodes = document.getElementById("create-form").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}