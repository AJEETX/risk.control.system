$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm <span class='fas fa-thumbtack'></span> <b> <u><i> Lock </i></u></b> Details",
                content: "Are you sure?",
                columnClass: 'medium',
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: "<span class='fas fa-thumbtack'></span> <u><i> Lock </i></u></b> Details",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('.card-footer a').attr('disabled', 'disabled');
                            $('.card-footer a').html("<i class='fas fa-sync' aria-hidden='true'></i> .......");

                            var nodes = document.getElementById("article").getElementsByTagName('*');
                            for (var i = 0; i < nodes.length; i++) {
                                nodes[i].disabled = true;
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