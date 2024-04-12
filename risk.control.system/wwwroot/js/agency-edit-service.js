$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit Service",
            content: "Are you sure to edit?",
            icon: 'fas fa-truck',

            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Edit Service",
                    btnClass: 'btn-warning',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('.btn.btn-warning').attr('disabled', 'disabled');
                        $('button#editservice.btn.btn-warning').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Service");
                        $('html a *, html button *').css('pointer-events', 'none');

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