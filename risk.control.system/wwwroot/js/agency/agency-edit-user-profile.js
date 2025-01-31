$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit",
            content: "Are you sure to save?",
            icon: 'fas fa-user-plus',

            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Save",
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
                        $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Save Profile");

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
    
});