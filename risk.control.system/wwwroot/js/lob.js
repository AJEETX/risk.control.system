﻿$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Create",
                content: "Are you sure to create?",
                icon: 'fas fa-business-time',
                columnClass: 'medium',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Create item",
                        btnClass: 'btn-success',
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
    })
});