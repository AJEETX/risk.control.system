$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit",
                content: "Are you sure to edit agency?",
                icon: 'fas fa-building',
                columnClass: 'medium',
                type: 'orange',
                closeIcon: true,
                typeAnimated: true,
                buttons: {
                    confirm: {
                        text: "Edit agency",
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