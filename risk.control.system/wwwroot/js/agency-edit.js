$(document).ready(function () {
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit Agency",
                content: "Are you sure to edit?",
                icon: 'fas fa-building',
                columnClass: 'medium',
                type: 'orange',
                closeIcon: true,
                typeAnimated: true,
                buttons: {
                    confirm: {
                        text: "Edit Agency",
                        btnClass: 'btn-warning',
                        action: function () {
                            askConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('.btn.btn-warning').attr('disabled', 'disabled');
                            $('.btn.btn-warning').html("<i class='fas fa-spinner' aria-hidden='true'></i> Edit Agency");

                            $('#create-form').submit();
                            var nodes = document.getElementById("create-form").getElementsByTagName('*');
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