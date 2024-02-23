$("#LineOfBusinessId").focus();

$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Add New",
            content: "Are you sure to add?",
            columnClass: 'medium',
            icon: 'fas fa-truck',
            type: 'green',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Add New",
                    btnClass: 'btn-success',
                    action: function () {
                        askConfirmation = false;
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('#create-pincode').attr('disabled', 'disabled');
                        $('#create-pincode').html("<i class='fas fa-spinner' aria-hidden='true'></i> Add Service");

                        form.submit();
                        var nodes = document.getElementById("create-form").getElementsByTagName('*');
                        for (var i = 0; i < nodes.length; i++) {
                            nodes[i].disabled = true;
                        }
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