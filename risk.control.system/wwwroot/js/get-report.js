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
                        text: "Submit",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = true;
                            $('#create-form').submit();
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
                            $('#create-form').submit();
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