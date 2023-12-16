$("#contractnum").focus();
$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Policy Edit",
            content: "Are you sure?",
            columnClass: 'medium',
            icon: 'fas fa-pen-alt',
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
dateContractId.max = new Date().toISOString().split("T")[0];
dateIncidentId.max = new Date().toISOString().split("T")[0];