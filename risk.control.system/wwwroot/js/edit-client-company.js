$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm  Edit",
            content: "Are you sure to edit?",
            icon: 'fas fa-building',
            columnClass: 'medium',
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
                        $('#create-user').attr('disabled', 'disabled');
                        $('#create-user').html("<i class='fas fa-spinner' aria-hidden='true'></i> Edit");

                        form.submit();
                        var nodes = document.getElementById("article").getElementsByTagName('*');
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