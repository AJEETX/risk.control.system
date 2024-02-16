$(document).ready(function () {
    let askConfirmation = false;
    $('#create-form').on('submit', function (e) {
        var report = $('#supervisorRemarks').val();

        if (report == '') {
            e.preventDefault();
            $.alert({
                title: "Supervisor Comments !!!",
                content: "Please enter comments ?",
                icon: 'fas fa-exclamation-triangle',
                columnClass: 'medium',
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
                title: "Confirm Report submission",
                content: "Are you sure?",
                icon: 'far fa-file-alt',
                columnClass: 'medium',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Submit",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = true;

                            $('#create-form').submit();
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#allocate-case').attr('disabled', 'disabled');
                            $('#allocate-case').html("<i class='far fa-file-alt' aria-hidden='true'></i> Submit .....");
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