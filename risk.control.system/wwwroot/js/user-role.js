﻿$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit",
            content: "Are you sure to edit user role?",
            icon: 'fas fa-user-plus',
            columnClass: 'medium',
            type: 'orange',
            closeIcon: true,
            typeAnimated: true,

            buttons: {
                confirm: {
                    text: "Edit user role",
                    btnClass: 'btn-warning',
                    action: function () {
                        askConfirmation = false;
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