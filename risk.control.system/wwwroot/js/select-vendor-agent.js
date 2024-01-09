$(document).ready(function () {
    var askConfirmation = true;
    $('#radioButtons').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Allocation",
                content: "Are you sure to allocate?",
                columnClass: 'medium',
                icon: 'fas fa-external-link-alt',
                type: 'red',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Allocate",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = false;
                            $('#radioButtons').submit();
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