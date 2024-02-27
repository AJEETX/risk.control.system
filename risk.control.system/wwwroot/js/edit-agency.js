$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit",
            content: "Are you sure to edit?",
            columnClass: 'medium',
            icon: 'fas fa-building',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Edit",
                    btnClass: 'btn-warning',
                    action: function () {
                        askConfirmation = false;
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('#create-agency').attr('disabled', 'disabled');
                        $('#create-agency').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Agency");

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
    $('#txtInput').on("cut copy paste", function (e) {
        e.preventDefault();
    });

    $("#create-form").validate();
});