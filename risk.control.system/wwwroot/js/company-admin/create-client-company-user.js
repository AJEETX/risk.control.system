$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Add New",
            content: "Are you sure to add ?",

            icon: 'fas fa-user-plus',
            type: 'green',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: " Add New",
                    btnClass: 'btn-success',
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
