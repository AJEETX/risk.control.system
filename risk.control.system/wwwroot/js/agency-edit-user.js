$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm Edit",
            content: "Are you sure to edit ?",
            columnClass: 'medium',
            icon: 'fas fa-user-plus',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Edit",
                    btnClass: 'btn-warning',
                    action: function () {
                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('.btn btn-warning').attr('disabled', 'disabled');
                        $('.btn btn-warning').html("<i class='fas fa-spinner' aria-hidden='true'></i> Edit User");

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
});