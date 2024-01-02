$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm <span class='fas fa-thumbtack'></span> <b> <u><i> Question </i></u></b> Details",
                content: "Are you sure?",
                columnClass: 'medium',
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: "<span class='fas fa-thumbtack'></span> <u><i> Question </i></u></b> Details",
                        btnClass: 'btn-danger',
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