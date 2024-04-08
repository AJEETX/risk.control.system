$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Withdrawl",
                content: "Are you sure to withdraw?",
    
                icon: 'fa fa-window-close',
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: " Withdraw ",
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