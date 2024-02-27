$.validator.setDefaults({
    submitHandler: function (form) {
        $.confirm({
            title: "Confirm  Edit Company",
            content: "Are you sure to edit?",
            icon: 'fas fa-building',
            columnClass: 'medium',
            type: 'orange',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Edit Company",
                    btnClass: 'btn-warning',
                    action: function () {

                        $("body").addClass("submit-progress-bg");
                        // Wrap in setTimeout so the UI
                        // can update the spinners
                        setTimeout(function () {
                            $(".submit-progress").removeClass("hidden");
                        }, 1);
                        $('.btn.btn-warning').attr('disabled', 'disabled');
                        $('#edit-company.btn.btn-warning').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Company");

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