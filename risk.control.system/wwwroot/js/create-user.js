$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm User Add",
                icon: 'fas fa-user-plus',
                columnClass: 'medium',
                content: "Are you sure?",
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Add User",
                        btnClass: 'btn-success',
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