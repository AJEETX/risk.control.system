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
                        form.submit();
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