$(document).ready(function () {
    var table = $("#customerTable").DataTable();

    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Add",

                content: "Are you sure to add?",
                icon: 'fas fa-user-tie',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Add ",
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
                icon: 'fas fa-user-tie',
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Edit",
                        btnClass: 'btn-warning',
                        action: function () {
                            askEditConfirmation = false;
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
});