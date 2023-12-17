$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm User Profile update",
            content: "Are you sure?",
            icon: 'fas fa-user-plus',
            columnClass: 'small',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Update",
                    btnClass: 'btn-warning',
                    action: function () {
                        form.submit();
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
    $("#create-form").validate();
});