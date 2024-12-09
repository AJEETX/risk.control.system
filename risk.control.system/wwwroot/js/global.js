$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit global-settings",
            content: "Are you sure to edit?",

            icon: 'fas fa-wrench',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Edit global-settings",
                    btnClass: 'btn-warning',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('#edit').attr('disabled', 'disabled');
                        $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit User");

                        form.submit();
                        var createForm = document.getElementById("create-form");
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