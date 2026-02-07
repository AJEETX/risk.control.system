$(document).ready(function () {

    $('#CurrentPassword').focus();

    let askConfirmation = true;

    $('#create-form').on('submit', function (e) {

        if (!askConfirmation) return;

        e.preventDefault();

        $.confirm({
            title: "Confirm Password Update",
            content: "Are you sure you want to update your password?",
            icon: 'fa fa-key',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Update",
                    btnClass: 'btn-warning',
                    action: function () {

                        askConfirmation = false;

                        // Disable submit button only
                        $('#create-form button[type="submit"]').prop('disabled', true);

                        // Show spinner
                        $('.submit-progress').removeClass('hidden');

                        // Submit form WITHOUT touching HTML
                        $('#create-form')[0].submit();
                    }
                },
                cancel: {
                    text: "Cancel",
                    btnClass: 'btn-secondary'
                }
            }
        });
    });
});
