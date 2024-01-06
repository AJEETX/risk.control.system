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
    });
});