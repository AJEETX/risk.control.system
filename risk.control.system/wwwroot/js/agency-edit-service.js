$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit",
            content: "Are you sure to edit?",
            icon: 'fas fa-truck',
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

    $('.selectPincode').attr('data-live-search', true);

    // Enable multiple select.
    $('.selectPincode').attr('multiple', true);
    $('.selectPincode').attr('data-selected-text-format', 'count');

    $('.selectPincode').selectpicker(
        {
            width: '100%',
            title: '- [Choose Multiple Pincodes] -',
            style: 'btn-info',
            size: 6,
            iconBase: 'fa',
            tickIcon: 'fa-check'
        });
    $('#PinCodeId').attr('multiple', true);
});