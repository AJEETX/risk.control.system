$(document).ready(function () {

    $("#Code").on("input", function () {
        this.value = this.value.toUpperCase();
    });

    var isChecking = false; // Flag to prevent recursion

    $("#Code").on("blur", function () {
        var code = $(this).val().trim();
        if (!code || isChecking) {
            $("#Code").removeClass('is-invalid').addClass('is-valid');
            return;
        }

        isChecking = true; // Block further triggers

        var id = $('input[name="StateId"]').val() || null;
        var CountryId = $('#CountryId').val();
        $.ajax({
            url: '/State/CheckDuplicateCode',
            type: 'POST',
            data: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                code: code,
                id: id,
                CountryId: CountryId
            },
            success: function (exists) {
                if (exists) {
                    $.alert({
                        title: '<i class="fas fa-exclamation-triangle text-danger"></i> Duplicate State Code!',
                        content: 'The State Code <b>' + code + '</b> already exists. Please choose another.',
                        type: 'red',
                        buttons: {
                            ok: {
                                action: function () {
                                    $("#Code").val("").focus();
                                    $("#Code").addClass('is-invalid').removeClass('is-valid');

                                }
                            }
                        },
                        // 2. THIS IS THE KEY: Wait for the modal to fully close
                        onClose: function () {
                            setTimeout(function () {
                                $("#Code").focus();
                            }, 400); // 100ms delay ensures the modal is gone
                            $("#Code").addClass('is-invalid').removeClass('is-valid');
                        }
                    });
                    
                } else {
                    $("#Code").removeClass('is-invalid').addClass('is-valid');
                }
            },
            error: function () {
                isChecking = false;
                $.alert({
                    title: 'Error',
                    content: 'Error occurred checking if State Code already exist.',
                    type: 'red',
                });
            }
        });
    });

    var preloadedCountryId = $("#CountryText").val(); // Get the hidden field value

    if (preloadedCountryId) {
        $.ajax({
            url: '/api/MasterData/GetCountryName', // Endpoint to fetch PinCodeName
            type: 'GET',
            data: { countryId: preloadedCountryId },
            success: function (response) {
                if (response && response.countryName) {
                    $("#CountryText").val(response.countryName); // Populate input with name
                }
            },
            error: DataTableErrorHandler
        });
    }
    $("#CountryText").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: '/api/MasterData/SearchCountry',
                data: {
                    term: request.term
                },
                success: function (data) {
                    response(data);
                },
                error: function (ex) {
                    console.error('Failed to fetch country data.' + ex);
                }
            });
        },
        minLength: 1, // Start search after typing 2 characters
        select: function (event, ui) {
            // Set the selected country's name and ID
            $("#CountryText").val(ui.item.label); // Set the display name
            $("#SelectedCountryId").val(ui.item.id); // Set the hidden country ID
            return false;
        }
    });

    // Clear hidden field if the user clears the input manually
    $("#CountryText").on('input', function () {
        if (!$(this).val()) {
            $("#SelectedCountryId").val('');
        }
    });

    $('#dataTable').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/State/GetStates',
            type: 'GET',
            dataType: 'json',
            data: function (d) {
                d.search = d.search.value; // Pass search term to the server
                d.orderColumn = d.order[0]?.column; // Pass the column index being sorted
                d.orderDirection = d.order[0]?.dir; // Pass the sorting directio
            }
        },
        order: [[0, 'asc']],
        fixedHeader: true,
        processing: true,
        paging: true,

        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            { data: 'code', orderable: true }, // Make sortable
            { data: 'name', orderable: true }, // Make sortable
            { data: 'countryName', orderable: true },
            { data: 'updatedBy' },
            {
                "data": "updated",
                "render": function (data, type, row) {
                    if (!data) return "";

                    // 1. Parse UTC string (Assuming format: "2023-10-27T10:00:00Z")
                    var date = new Date(data);

                    // 2. Convert to Local String
                    // You can customize the format: { dateStyle: 'medium', timeStyle: 'short' }
                    var localDate = date.toLocaleString();

                    return `<i title="${localDate}" data-bs-toggle="tooltip">
                    <small><strong>${localDate}</strong></small>
                </i>`;
                }
            },
            {
                data: 'stateId',
                render: function (data, type, row) {
                    return `
                                <a  id="edit${data}" class="btn btn-xs btn-warning" href="/State/Edit/${data}">
                                    <i class="fas fa-map-marker-alt"></i> Edit
                                </a>
                                <button type="button" class="btn btn-xs btn-danger delete-item" data-id="${data}">
                                            <i class="fa fa-trash"></i> Delete
                                        </a>`;
                }
            }
        ],
        "drawCallback": function (setting) {
            $('#dataTable tbody').on('click', '.btn-warning', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('edit', ''); // Extract the ID from the button's ID attribute
                showedit(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
        }
    });

    $('.auto-dropdown').on('focus', function () {
        $(this).select();
    });
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            if ($('#create-form').valid()) {
                e.preventDefault();
                $.confirm({
                    title: "Confirm  Add New",
                    content: "Are you sure to add?",

                    icon: 'fas fa-map-marker-alt',
                    type: 'green',
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: " Add New",
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
                                $('#create').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add New");

                                $('#create-form').submit();
                                var form = document.getElementById("create-form");
                                if (form) {
                                    const formElements = form.getElementsByTagName("*");
                                    for (const element of formElements) {
                                        element.disabled = true;
                                        if (element.hasAttribute("readonly")) {
                                            element.classList.remove("valid", "is-valid", "valid-border");
                                            element.removeAttribute("aria-invalid");
                                        }
                                    }
                                }
                            }
                        },
                        cancel: {
                            text: "Cancel",
                            btnClass: 'btn-default'
                        }
                    }
                });
            } else {
                $.alert({
                    title: 'Incomplete detail',
                    content: 'Complete the required fields',
                    type: 'red'
                })
            }

        }
    });

    var askEditConfirmation = true;
    $('#edit-form').submit(function (e) {
        if (askEditConfirmation) {
            if ($('#edit-form').valid()) {
                e.preventDefault();
                $.confirm({
                    title: "Confirm Edit",
                    content: "Are you sure to edit?",
                    icon: 'fas fa-map-marker-alt',

                    type: 'orange',
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: "Edit ",
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
                                $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit ");
                                $('#edit-form').submit();
                                var form = document.getElementById("edit-form");
                                if (form) {
                                    const formElements = form.getElementsByTagName("*");
                                    for (const element of formElements) {
                                        element.disabled = true;
                                        if (element.hasAttribute("readonly")) {
                                            element.classList.remove("valid", "is-valid", "valid-border");
                                            element.removeAttribute("aria-invalid");
                                        }
                                    }
                                }
                            }
                        },
                        cancel: {
                            text: "Cancel",
                            btnClass: 'btn-default'
                        }
                    }
                });
            } else {
                $.alert({
                    title: 'Incomplete detail',
                    content: 'Complete the required fields',
                    type: 'red'
                })
            }
        }
    })

    $(document).on("click", ".delete-item", function () {
        var $btn = $(this);
        var $spinner = $(".submit-progress"); // global spinner (you already have this)
        var id = $(this).data("id");
        var row = $(this).closest("tr");
        var table = $('#dataTable').DataTable();

        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete ?',
            type: 'red',
            buttons: {
                confirm: {
                    text: 'Yes, Delete',
                    btnClass: 'btn-red',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Delete');
                        $.ajax({
                            url: '/State/Delete',
                            type: 'POST',
                            data: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
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
                                    $.alert({
                                        title: 'Delete Error',
                                        content: response.message,
                                        type: 'red'
                                    });
                                }
                            },
                            error: function (e) {
                                $.alert({
                                    title: 'Delete Error',
                                    content: 'Error while deleting.',
                                    type: 'red'
                                });
                            },
                            complete: function () {
                                $spinner.addClass("hidden");
                                // ✅ Re-enable button and restore text
                                $btn.prop("disabled", false).html('<i class="fas fa-trash"></i> Delete');
                            }
                        });
                    }
                },
                cancel: {
                    text: 'Cancel',
                    btnClass: 'btn-default'
                }
            }
        });
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
$('#Name').focus();
