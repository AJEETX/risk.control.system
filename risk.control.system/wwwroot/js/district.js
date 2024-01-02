$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Create",
                columnClass: 'medium',
                content: "Are you sure to create?",
                icon: 'fas fa-city',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Create item",
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