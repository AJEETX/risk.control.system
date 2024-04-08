$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Set To Ready",
                content: "Are you sure?",
    
                icon: 'fas fa-thumbtack',
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: "Set Ready [to Assign]",
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