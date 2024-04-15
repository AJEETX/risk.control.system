$(document).ready(function () {
    let askConfirmation = false;
    let review = false;

    $('#review-case').click(function () {
        //If the checkbox is checked.
        var report = $('#assessorRemarks').val();
        if (report != '') {
            review = true;
            $('#assessorRemarkType').val('REVIEW');
        } else {
            review = false;
        }
    });
    $('#approve-case').click(function () {
        //If the checkbox is checked.
        var report = $('#assessorRemarks').val();
        var reviewChecked = $('#flexRadioDefault2').is(':checked');
        var approvedChecked = $('#flexRadioDefault3').is(':checked');
        if (report != '' && reviewChecked) {
            $('#assessorRemarkType').val('REVIEW');
            review = true;
        } else if (report != '' && approvedChecked){
            review = false;
            $('#assessorRemarkType').val('OK');
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
        else if (!askConfirmation && !review) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Approve",
                content: "Are you sure?",
                icon: 'far fa-thumbs-up',
    
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Approve",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = true;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#approve-case.btn.btn-success').attr('disabled', 'disabled');
                            $('#approve-case.btn.btn-success').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Approve");
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
        else if (!askConfirmation && review) {
            e.preventDefault();
            $.confirm({
                title: "Confirm review",
                content: "Are you sure?",
                icon: 'fas fa-sync',
    
                type: 'red',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Review",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = true;
                            review = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#review-case.btn.btn-danger').attr('disabled', 'disabled');
                            $('#review-case.btn.btn-danger').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Review");
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
});