$(document).ready(function () {
    $('#Name').focus();

    var table = $("#customerTable").DataTable();

    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Add New",
                content: "Are you sure to add?",

                icon: 'fas fa-money-check-alt',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Add New",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = false;
                            // Disable all buttons, submit inputs, and anchors
                            $('button, input[type="submit"], a').prop('disabled', true);

                            // Add a class to visually indicate disabled state for anchors
                            $('a').addClass('disabled-anchor').on('click', function (e) {
                                e.preventDefault(); // Prevent default action for anchor clicks
                            });

                            $('#create-form').submit();
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

    var askEditConfirmation = true;
    $('#edit-form').submit(function (e) {
        if (askEditConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit",
                content: "Are you sure to edit?",
                icon: 'fas fa-money-check-alt',

                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Edit ",
                        btnClass: 'btn-warning',
                        action: function () {
                            askEditConfirmation = false;
                            // Disable all buttons, submit inputs, and anchors
                            $('button, input[type="submit"], a').prop('disabled', true);

                            // Add a class to visually indicate disabled state for anchors
                            $('a').addClass('disabled-anchor').on('click', function (e) {
                                e.preventDefault(); // Prevent default action for anchor clicks
                            });
                            $('#edit-form').submit();
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

    $(".delete-item").on("click", function () {
        var id = $(this).data("id");
        var row = $(this).closest("tr");

        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete ?',
            type: 'red',
            typeAnimated: true,
            buttons: {
                confirm: {
                    text: 'Yes, Delete',
                    btnClass: 'btn-red',
                    action: function () {
                        $.ajax({
                            url: '/CostCentre/Delete',
                            type: 'POST',
                            data: {
                                icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
                                id: id
                            },
                            success: function (response) {
                                if (response.success) {
                                    row.fadeOut(500, function () {
                                        $(this).remove();
                                    });
                                    $.alert(response.message);
                                } else {
                                    $.alert(response.message);
                                }
                            },
                            error: function (e) {
                                $.alert('Error while deleting.');
                            }
                        });
                    }
                },
                cancel: {
                    text: 'Cancel',
                    btnClass: 'btn-secondary'
                }
            }
        });
    });

    $('a.create').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        // Disable all buttons, submit inputs, and anchors
        $('button, input[type="submit"], a').prop('disabled', true);

        // Add a class to visually indicate disabled state for anchors
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });

        $('a.create').html("<i class='fas fa-sync fa-spin'></i> Add New");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });
});