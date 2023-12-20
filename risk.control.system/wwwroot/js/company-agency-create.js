$("#LineOfBusinessId").focus();

$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Create",
            content: "Are you sure to create?",
            columnClass: 'medium',
            icon: 'fas fa-truck',
            type: 'green',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Create",
                    btnClass: 'btn-success',
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

    $('#PinCodeId').attr('data-live-search', true);

    //// Enable multiple select.
    $('#PinCodeId').attr('multiple', true);
    $('#PinCodeId').attr('data-selected-text-format', 'count');
});