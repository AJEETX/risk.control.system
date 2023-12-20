$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm User Edit",
            content: "Are you sure?",
            columnClass: 'medium',
            icon: 'fas fa-user-plus',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Edit",
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