$(document).ready(function () {
    var askConfirmation = true;
    var remarksElement = $('#remarks');
    if (remarksElement) {
        remarksElement.focus();
    }
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
    });
    $('#decline-information-popup').on('click', function (e) {
        $.alert(
            {
                title: " Decline Claim !",
                content: "This CLAIM can not be declined as per the status.",
                icon: 'fas fa-info',
                animationBounce: 2.5,
                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "CLOSE",
                        btnClass: 'btn-danger'
                    }
                }
            }
        );
    });

    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm DECLINE",
                content: "Are you sure?",
    
                icon: 'fas fa-undo',
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: "DECLINE ",
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
                            $('body').attr('disabled', 'disabled');
                            $('html *').css('cursor', 'not-allowed');
                            $('button').prop('disabled', true);
                            $('a.btn *').removeAttr('href');
                            $('html a *, html button *').css('pointer-events', 'none');
                            $('#submit-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> DECLINE");

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

