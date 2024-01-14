$("#LineOfBusinessId").focus();

$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Service Creation",
            content: "Are you sure?",
            icon: 'fas fa-truck',
            type: 'green',
            columnClass: 'medium',
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

    $('#PinCodeId').attr('data-live-search', true);

    //// Enable multiple select.
    $('#PinCodeId').attr('multiple', true);
    $('#PinCodeId').attr('data-selected-text-format', 'count');
});