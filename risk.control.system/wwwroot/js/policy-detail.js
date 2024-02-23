$(document).ready(function () {
    $('#edit-policy').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#edit-policy').attr('disabled', 'disabled');
        $('#edit-policy').html("<i class='fas fa-spinner' aria-hidden='true'></i> Edit  Policy");

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
        $('#add-customer').html("<i class='fas fa-spinner' aria-hidden='true'></i> Add Customer");

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
        $('#edit-customer').html("<i class='fas fa-spinner' aria-hidden='true'></i> Edit Customer");

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
        $('#add-beneficiary').html("<i class='fas fa-spinner' aria-hidden='true'></i> Add Beneficiary");

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
        $('#edit-beneficiary').html("<i class='fas fa-spinner' aria-hidden='true'></i> Edit Beneficiary");

        var nodes = document.getElementById("article").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });

    //$('.btn.btn-warning').on('click', function () {
    //    $("body").addClass("submit-progress-bg");
    //    // Wrap in setTimeout so the UI
    //    // can update the spinners
    //    setTimeout(function () {
    //        $(".submit-progress").removeClass("hidden");
    //    }, 1);
    //    $('.btn.btn-warning').attr('disabled', 'disabled');
    //    $('.btn.btn-warning').html("<i class='fas fa-spinner' aria-hidden='true'></i> Assign List <i class='fas fa-arrow-right'></i>");

    //    var nodes = document.getElementById("body").getElementsByTagName('*');
    //    for (var i = 0; i < nodes.length; i++) {
    //        nodes[i].disabled = true;
    //    }
    //});

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