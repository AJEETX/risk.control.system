$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit User",
            content: "Are you sure to save?",

            icon: 'fas fa-user-plus',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Save User",
                    btnClass: 'btn-warning',
                    action: function () {
                        
                        $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Save User");

                        form.submit();
                        disableAllInteractiveElements();

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
$(document).ready(function () {
    $("#edit-form").validate();
});