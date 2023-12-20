$("#BeneficiaryName").focus();
BeneficiaryDateOfBirthId.max = new Date().toISOString().split("T")[0];

$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Beneficary Edit",
            content: "Are you sure?",
            icon: 'fas fa-user-tie',
            columnClass: 'medium',
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