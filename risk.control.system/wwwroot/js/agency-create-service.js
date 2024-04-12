$("#LineOfBusinessId").focus();

$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Add Service",
            content: "Are you sure to add?",
            icon: 'fas fa-truck fa-sync',
            type: 'green',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Add Service",
                    btnClass: 'btn-success',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('html a *, html button *').css('pointer-events', 'none');
                        $('#create-pincode').attr('disabled', 'disabled');
                        $('#create-pincode').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Service");

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