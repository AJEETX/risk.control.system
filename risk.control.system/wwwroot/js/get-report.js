$(document).ready(function () {
    let askConfirmation = false;
    let approve= false;
    let review = false;
    let reject = false;

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
});