$(document).ready(function () {

    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Add New Policy",
                content: "Are you sure to add ?",
                columnClass: 'medium',
                icon: 'fas fa-thumbtack',
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: "Add New",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = false;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#create-policy').attr('disabled', 'disabled');
                            $('#create-policy').html("<i class='far fa-file-powerpoint' aria-hidden='true'></i> Adding .....");

                            $('#create-form').submit();
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