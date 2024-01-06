$(document).ready(function () {
    $('#create-form').on('submit', function (e) {
        var report = $('#supervisorRemarks').val();

        if (report == '') {
            askConfirmation = false;
            e.preventDefault();
            $.alert({
                title: "Supervisor Comments !!!",
                content: "Please enter comments?",
                icon: 'fas fa-exclamation-triangle',
                columnClass: 'medium',
                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "OK",
                        btnClass: 'btn-danger'
                    }
                }
            });
        }
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm submission",
                content: "Are you sure?",
                columnClass: 'medium',
                icon: 'fas fa-thumbtack',
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: "Submit case",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = false;
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