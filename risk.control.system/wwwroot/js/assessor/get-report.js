$(document).ready(function () {
    let askConfirmation = false;
    let approve = false;
    let review = false;
    let reject = false;

    var currentImageElement = document.getElementById('documentImage0');
    var currentImage;
    if (currentImageElement) {
        currentImage = currentImageElement.src;
    }

    var descriptioninput = document.getElementById('description');
    if (descriptioninput) {
        descriptioninput.value = "Please provide brief information about the person previous claim. Attached document shall be duly completed and sent in the reply."
        descriptioninput.focus();
    }

    var answerinput = document.getElementById('answer');
    if (answerinput) {
        answerinput.value = "The person detailed information is that there is no previous claim. Attached is the duly completed and signed document."
        answerinput.focus();
    }
    $("#document").on('change', function () {
        var MaxSizeInBytes = 5242880;
        //Get count of selected files
        var countFiles = $(this)[0].files.length;

        var imgPath = $(this)[0].value;
        var extn = imgPath.substring(imgPath.lastIndexOf('.') + 1).toLowerCase();

        if (extn == "png" || extn == "jpg" || extn == "jpeg") {
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
                    title: "Upload Error !!",
                    content: "Pls select only image with extension jpg, png",
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

                            $('#approve-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Submit");
                            disableAllInteractiveElements();

                            $('#create-form').submit();

                            var createForm = document.getElementById("create-form");
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

                            $('#approve-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i>  Submit");
                            disableAllInteractiveElements();

                            $('#create-form').submit();

                            var createForm = document.getElementById("create-form");
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

    $("#add-question").on("click", function () {
        var questionIndex = $(".question-card").length; // ensure unique index each time
        let template = `
        <div class="card bg-light mb-3 p-3 question-card">
            <div class="d-flex justify-content-between align-items-center mb-2">
                <span class="input-group-label">Multiple choice question${questionIndex + 1}:</span>
                <button type="button" class="btn btn-sm btn-outline-danger remove-question">
                    <i class="fas fa-trash"></i> Delete
                </button>
            </div>

            <div class="form-group">
                <div class="input-group mb-3">
                    <div class="input-group-prepend">
                        <span class="input-group-text">
                            <i class="far fa-comment-alt"></i><i class="fa fa-asterisk asterik-style"></i>
                        </span>
                    </div>
                    <input class="form-control remarks"
                           name="InvestigationReport.EnquiryRequests[${questionIndex}].MultipleQuestionText"
                           placeholder="Enter Enquiry subject detail" required value="Sample question ${questionIndex + 1}?">
                </div>
            </div>

            <div class="row">
                <div class="col-md-6">
                    <div class="form-group">
                        <span class="input-group-label">Choice 1:</span>
                        <div class="input-group mb-3">
                            <div class="input-group-prepend">
                                <span class="input-group-text">
                                    <i class="far fa-comment-alt"></i><i class="fa fa-asterisk asterik-style"></i>
                                </span>
                            </div>
                            <input class="form-control remarks"
                                   name="InvestigationReport.EnquiryRequests[${questionIndex}].AnswerA"
                                   placeholder="Answer A" required value="Answer A.">
                        </div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="form-group">
                        <span class="input-group-label">Choice 2:</span>
                        <div class="input-group mb-3">
                            <div class="input-group-prepend">
                                <span class="input-group-text">
                                    <i class="far fa-comment-alt"></i><i class="fa fa-asterisk asterik-style"></i>
                                </span>
                            </div>
                            <input class="form-control remarks"
                                   name="InvestigationReport.EnquiryRequests[${questionIndex}].AnswerB"
                                   placeholder="Answer B" required value="Answer B.">
                        </div>
                    </div>
                </div>
            </div>

            <div class="row">
                <div class="col-md-6">
                    <div class="form-group">
                        <span class="input-group-label">Choice 3:</span>
                        <div class="input-group mb-3">
                            <div class="input-group-prepend">
                                <span class="input-group-text">
                                    <i class="far fa-comment-alt"></i><i class="fa fa-asterisk asterik-style"></i>
                                </span>
                            </div>
                            <input class="form-control remarks"
                                   name="InvestigationReport.EnquiryRequests[${questionIndex}].AnswerC"
                                   placeholder="Answer C" required value="Answer C.">
                        </div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="form-group">
                        <span class="input-group-label">Choice 4:</span>
                        <div class="input-group mb-3">
                            <div class="input-group-prepend">
                                <span class="input-group-text">
                                    <i class="far fa-comment-alt"></i><i class="fa fa-asterisk asterik-style"></i>
                                </span>
                            </div>
                            <input class="form-control remarks"
                                   name="InvestigationReport.EnquiryRequests[${questionIndex}].AnswerD"
                                   placeholder="Answer D" required value="Answer D.">
                        </div>
                    </div>
                </div>
            </div>
        </div>`;
        $("#question-added").append(template);
    });

    // Event delegation for delete button
    $(document).on("click", ".remove-question", function (e) {
        e.preventDefault(); // stop default action if it's a link or button

        let $card = $(this).closest(".question-card");

        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete this question?',
            type: 'red',
            buttons: {
                confirm: {
                    text: 'Yes, Delete',
                    btnClass: 'btn-danger',
                    action: function () {
                        $card.remove(); // delete on confirm
                    }
                },
                cancel: {
                    text: 'Cancel',
                    btnClass: 'btn-default'
                }
            }
        });
    });

    $('#query-form').on('submit', function (e) {
        if (askConfirmation) {
            return true; // ✅ allow normal POST
        }

        e.preventDefault();

        var enquiry = $('#description').val().trim();

        if (!enquiry) {
            $.alert({
                title: "Enquiry Detail !!!",
                content: "Please enter enquiry information?",
                icon: 'fas fa-exclamation-triangle',
                type: 'red',
                closeIcon: true,
                buttons: {
                    ok: {
                        text: "OK",
                        btnClass: 'btn-danger',
                        action: function () {
                            $('#description').focus();
                        }
                    }
                }
            });
            return;
        }

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
                        $(".submit-progress").removeClass("hidden");

                        $('#send-query')
                            .html("<i class='fas fa-sync fa-spin'></i> Send Enquiry")
                            .prop("disabled", true);

                        document.getElementById('query-form').submit();
                        disableAllInteractiveElements();
                    }
                },
                cancel: {
                    text: "Cancel",
                    btnClass: 'btn-default'
                }
            }
        });
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
    $('#enquire-case').on('click', function (e) {
        showenquiry();
    });
});

function showenquiry() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);

    $('#enquire-case').html("<i class='fas fa-sync fa-spin'></i> Enquire");
    disableAllInteractiveElements();

    var createForm = document.getElementById("create-form");
    if (createForm) {
        var nodes = createForm.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}