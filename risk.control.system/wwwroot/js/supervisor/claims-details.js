$(document).ready(function () {
    var askConfirmation = true;
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
                           
                            $('#submit-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> DECLINE");
                            disableAllInteractiveElements();

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

    $('#withdraw-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm WITHDRAW",
                content: "Are you sure?",

                icon: 'fas fa-undo',
                closeIcon: true,
                type: 'red',
                buttons: {
                    confirm: {
                        text: "WITHDRAW ",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = false;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $('#withdraw-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Withdraw");
                            disableAllInteractiveElements();

                            $('#withdraw-form').submit();
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

