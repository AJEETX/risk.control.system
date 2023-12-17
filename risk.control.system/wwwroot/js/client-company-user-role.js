$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit",
                columnClass: 'small',
                content: "Are you sure to edit user role?",
                icon: 'fas fa-user-plus',
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Edit user role",
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