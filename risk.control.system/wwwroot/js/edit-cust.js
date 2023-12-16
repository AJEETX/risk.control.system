$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Customer Edit",
            content: "Are you sure?",
            icon: 'fas fa-user-plus',
            type: 'orange',
            columnClass: 'medium',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Edit",
                    btnClass: 'btn-orange',
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
$("#customer-name").focus();

dateCustomerId.max = new Date().toISOString().split("T")[0];