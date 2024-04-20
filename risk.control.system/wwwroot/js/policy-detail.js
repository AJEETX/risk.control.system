$(document).ready(function () {

    var askConfirmation = true;
    $('#create-form').on('submit', function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm ASSIGN<span class='badge badge-light'>(auto)</span>",
                content: "Are you sure to ASSIGN<span class='badge badge-light'>(auto)</span> ?",
                icon: 'fas fa-random',
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "ASSIGN <span class='badge badge-warning'>(auto)</span>",
                        btnClass: 'btn-warning',
                        action: function () {
                            askConfirmation = false;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#assign-list').attr('disabled', 'disabled');
                            $('#assign-list').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> ASSIGN<span class='badge badge-light'>(auto)</span>");

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

    $('#edit-policy').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#edit-policy').attr('disabled', 'disabled');
        $('#edit-policy').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit  Policy");

        var nodes = document.getElementById("article").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });

    $('a#add-customer').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#add-customer').attr('disabled', 'disabled');
        $('#add-customer').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Customer");

        var nodes = document.getElementById("article").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });

    $('#edit-customer').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#edit-customer').attr('disabled', 'disabled');
        $('#edit-customer').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Customer");

        var nodes = document.getElementById("article").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });

    $('#add-beneficiary').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#add-beneficiary').attr('disabled', 'disabled');
        $('#add-beneficiary').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Beneficiary");

        var nodes = document.getElementById("article").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });

    $('#edit-beneficiary').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#edit-beneficiary').attr('disabled', 'disabled');
        $('#edit-beneficiary').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit Beneficiary");

        var nodes = document.getElementById("article").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });

    $('#assign-manual-list').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#assign-manual-list').attr('disabled', 'disabled');
        $('#assign-manual-list').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> ASSIGN");

        var nodes = document.getElementById("body").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });

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
    var withdrawAskConfirmation = true;
    $('#withdraw-form').submit(function (e) {
        if (withdrawAskConfirmation) {
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
                            withdrawAskConfirmation = false;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#submit-case').attr('disabled', 'disabled');
                            $('#submit-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Withdraw");

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