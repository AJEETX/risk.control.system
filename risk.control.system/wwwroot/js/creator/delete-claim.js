$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
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
                            $('#create-user').attr('disabled', 'disabled');
                            $('body').attr('disabled', 'disabled');
                            $('html *').css('cursor', 'not-allowed');
                            $('button').prop('disabled', true);
                            $('a.btn *').removeAttr('href');
                            $('html a *, html button *').css('pointer-events', 'none');
                            $('#create-user').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Delete");

                            $('#create-form').submit();
                            var createForm = document.getElementById("create-form");
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