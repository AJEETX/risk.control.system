$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Customer Add",
            content: "Are you sure?",
            icon: 'fas fa-user-plus',
            columnClass: 'medium',
            type: 'green',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Add",
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
$("#customer-name").focus();

dateCustomerId.max = new Date().toISOString().split("T")[0];