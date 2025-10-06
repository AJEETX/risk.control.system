$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit",
            content: "Are you sure to save?",

            icon: 'fas fa-building',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Save",
                    btnClass: 'btn-warning',
                    action: function () {
                        askConfirmation = false;
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('#edit').attr('disabled', 'disabled');
                        $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Save Agency");

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
    $('#Name').focus();

    $("#edit-form").validate();   
});