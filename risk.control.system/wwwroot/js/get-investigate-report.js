$(document).ready(function () {
    $('#reselect-case').click(function (e) {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('body').attr('disabled', 'disabled');
        $(this).html("<i class='fas fa-sync fa-spin'></i> REVIEW");

        $('html *').css('cursor', 'not-allowed');
        $('html a *, html button *').css('pointer-events', 'none');

        var nodes = document.getElementById("section").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });

    let askConfirmation = false;
    $('#create-form').on('submit', function (e) {
        var report = $('#supervisorRemarks').val();

        if (report == '') {
            e.preventDefault();
            $.alert({
                title: "Comments !!!",
                content: "Please enter comments ?",
                icon: 'fas fa-exclamation-triangle',
    
                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "OK",
                        btnClass: 'btn-danger',
                        action: function () {
                            $.alert('Canceled!');
                            $('#supervisorRemarks').focus();
                        }
                    },
                }
            });
        }
        else if (!askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Submit",
                content: "Are you sure?",
                icon: 'far fa-file-alt',
    
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "SUBMIT",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = true;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#allocate-case').attr('disabled', 'disabled');
                            $('#allocate-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> SUBMIT");

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
    });
});