$(document).ready(function () {
    $('#Name').focus();

    $('#customerTable').DataTable({
        ajax: {
            url: '/BeneficiaryRelation/GetBeneficiaryRelations',
            type: 'GET',
            datatype: 'json'
        },
        responsive: true,
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            { data: 'name' },
            { data: 'code' },
            { data: 'updated' },
            {
                data: 'beneficiaryRelationId',
                render: function (data) {
                    return `
                        <a id="edit${data}" class="btn btn-xs btn-warning" href="/BeneficiaryRelation/Edit/${data}">
                            <i class="fas fa-user-tie"></i> Edit
                        </a>
                        &nbsp;
                        <button type="button" class="btn btn-xs btn-danger delete-item" data-id="${data}">
                            <i class="fas fa-trash"></i> Delete
                        </button>`;
                }
            }
        ],
        "drawCallback": function (setting) {
            $('#customerTable tbody').on('click', '.btn-warning', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('edit', ''); // Extract the ID from the button's ID attribute
                showedit(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
        }
    });

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
                            $('button#create').html("<i class='fas fa-sync fa-spin'></i> Add New");
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
                            $('button#edit').html("<i class='fas fa-sync fa-spin'></i> Edit");
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

    $(document).on("click", ".delete-item", function () {
        var id = $(this).data("id");
        var row = $(this).closest("tr");
        var table = $('#customerTable').DataTable();

        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete ?',
            type: 'red',
            buttons: {
                confirm: {
                    text: 'Yes, Delete',
                    btnClass: 'btn-red',
                    action: function () {
                        $.ajax({
                            url: '/BeneficiaryRelation/Delete',
                            type: 'POST',
                            data: {
                                icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
                                id: id
                            },
                            success: function (response) {
                                if (response.success) {
                                    table.row(row).remove().draw(false); // correct DataTable removal
                                    $.alert({
                                        title: 'Deleted',
                                        content: response.message,
                                        type: 'red'
                                    });
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
function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);

    $('a#edit' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    disableAllInteractiveElements();

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}