$(document).ready(function () {
    var askConfirmation = true;
    $('#delete-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm DELETE",
                content: "Are you sure to delete?",
                icon: 'fas fa-trash',
    
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: "DELETE",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#delete-user').attr('disabled', 'disabled');
                            $('#delete-user').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Delete");

                            $('#delete-form').submit();
                            var createForm = document.getElementById("delete-form");
                            if (createForm) {

                                var nodes = createForm.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
                                }
                            }
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