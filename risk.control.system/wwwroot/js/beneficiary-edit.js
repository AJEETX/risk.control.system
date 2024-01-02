$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit",
                columnClass: 'medium',
                content: "Are you sure to edit?",
                icon: 'fas fa-user-tie',
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Edit Item",
                        btnClass: 'btn-warning',
                        action: function () {
                            askConfirmation = false;
                            $('#create-form').submit();
                        }
                    },
                    cancel: {
                        text: "Cancel",
                        btnClass: 'btn-default'
                    }
                }
            });
        }
    })
});