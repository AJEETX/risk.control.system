$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Add Policy",
            content: "Are you sure?",
            icon: 'far fa-file-powerpoint',
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
$("#ContractNumber").focus();

dateContractId.max = new Date().toISOString().split("T")[0];
dateIncidentId.max = new Date().toISOString().split("T")[0];