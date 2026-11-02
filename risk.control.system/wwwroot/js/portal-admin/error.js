$(document).ready(function () {
    $(document).on('submit', '.delete-form', function (e) {
        e.preventDefault(); // Stop the form from submitting immediately

        var form = this;

        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete this log file?',
            type: 'red',
            typeAnimated: true,
            buttons: {
                confirm: {
                    text: 'Delete',
                    btnClass: 'btn-danger',
                    action: function () {
                        form.submit(); // Submit the form if user confirms
                    }
                },
                cancel: function () {
                    // Do nothing on cancel
                }
            }
        });
    });
});