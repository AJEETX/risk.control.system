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
        if (report != '') {
            review = false;
            $('#assessorRemarkType').val('OK');
        } else {
            review = true;
        }
    });

    $('#create-form').on('submit', function (e) {
        var report = $('#assessorRemarks').val();

        if (report == '') {
            e.preventDefault();
            $.alert({
                title: "Claim Assessment !!!",
                content: "Please enter comments?",
                icon: 'fas fa-exclamation-triangle',
                columnClass: 'medium',
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
                title: "Confirm Report approval",
                content: "Are you sure?",
                icon: 'far fa-thumbs-up',
                columnClass: 'medium',
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
                            $('#create-form').submit();

                            $('button#approve-case.btn.btn-success').attr('disabled', 'disabled');
                            $('button#approve-case.btn.btn-success').html("<i class='fas fa-spinner' aria-hidden='true'></i> Approve");

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
                title: "Confirm Report review",
                content: "Are you sure?",
                icon: 'fas fa-sync',
                columnClass: 'medium',
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
                            $('#create-form').submit();

                            $('button#review-case.btn.btn-danger').attr('disabled', 'disabled');
                            $('button#review-case.btn.btn-danger').html("<i class='fas fa-spinner' aria-hidden='true'></i> Review");

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