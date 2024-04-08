$(document).ready(function () {
    var askConfirmation = true;

    var report = $('#remarks').val();
    if ($(this).is(':checked') && report != '') {
        //Enable the submit button.
        $('#submit-case').attr("disabled", false);
    } else {
        //If it is not checked, disable the button.
        $('#submit-case').attr("disabled", true);
    }

    $('#remarks').on('keydown', function () {
        var report = $('#remarks').val();
        if (report != '') {
            //Enable the submit button.
            $('#submit-case').attr("disabled", false);
        } else {
            //If it is not checked, disable the button.
            $('#submit-case').attr("disabled", true);
        }
    })
    $('#remarks').on('blur', function () {
        var report = $('#remarks').val();
        if (report != '') {
            //Enable the submit button.
            $('#submit-case').attr("disabled", false);
        } else {
            //If it is not checked, disable the button.
            $('#submit-case').attr("disabled", true);
        }
    })

    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm withdrwal",
                content: "Are you sure?",
    
                icon: 'fas fa-undo',
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: "Withdraw case",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = false;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#submit-case').attr('disabled', 'disabled');
                            $('#submit-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Withdraw");

                            $('#create-form').submit();
                            var nodes = document.getElementById("body").getElementsByTagName('*');
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
