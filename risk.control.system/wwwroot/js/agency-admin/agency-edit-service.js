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
                        // Disable all buttons, submit inputs, and anchors
                        $('button, input[type="submit"], a').prop('disabled', true);

                        // Add a class to visually indicate disabled state for anchors
                        $('a').addClass('disabled-anchor').on('click', function (e) {
                            e.preventDefault(); // Prevent default action for anchor clicks
                        });
                        $('button#editservice.btn.btn-warning').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Service");
                        
                        form.submit();
                        var createForm = document.getElementById("edit-form");
                        if (createForm) {
                            var nodes = createForm.getElementsByTagName('*');
                            for (var i = 0; i < nodes.length; i++) {
                                nodes[i].disabled = true;
                            }
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
    $("#edit-form").validate();

    // Highlight selected options in the multi-select dropdown
    $('#PinCodeId').on('change', function () {
        // Apply styles to selected options
        $('#PinCodeId option:selected').each(function () {
            $(this).css('background-color', '#007bff'); // Blue background for selected options
            $(this).css('color', 'white'); // White text color
        });

        // Remove styles from unselected options
        $('#PinCodeId option').not(':selected').each(function () {
            $(this).css('background-color', ''); // Reset background color
            $(this).css('color', ''); // Reset text color
        });
    });

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