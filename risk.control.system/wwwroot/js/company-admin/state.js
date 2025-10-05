$(document).ready(function () {

    var preloadedCountryId = $("#CountryId").val(); // Get the hidden field value

    if (preloadedCountryId) {
        $.ajax({
            url: '/api/Company/GetCountryName', // Endpoint to fetch PinCodeName
            type: 'GET',
            data: { countryId: preloadedCountryId },
            success: function (response) {
                if (response && response.countryName) {
                    $("#CountryName").val(response.countryName); // Populate input with name
                }
            },
            error: function () {
                console.error('Failed to fetch PinCodeName');
            }
        });
    }
    $("#CountryName").autocomplete({
        source: function (request, response) {
            $.ajax({
                url: '/api/Company/SearchCountry',
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
            $("#CountryName").val(ui.item.label); // Set the display name
            $("#CountryId").val(ui.item.id); // Set the hidden country ID
            return false;
        }
    });

    // Clear hidden field if the user clears the input manually
    $("#CountryName").on('input', function () {
        if (!$(this).val()) {
            $("#CountryId").val('');
        }
    });

    $('#customerTable').DataTable({
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
            { data: 'updated'},
            {
                data: 'stateId',
                render: function (data, type, row) {
                    return `
                                <a class="btn btn-xs btn-warning" href="/State/Edit/${data}">
                                    <i class="fas fa-map-marker-alt"></i> Edit
                                </a>
                                &nbsp;
                                <a class="btn btn-xs btn-danger" href="/State/Delete/${data}">
                                    <i class="fas fa-trash"></i> Delete
                                </a>`;
                }
            }
        ]
    });

    $('.auto-dropdown').on('focus', function () {
        $(this).select();
    });
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
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
                            $('#create').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add State");

                            $('#create-form').submit();
                            var createForm = document.getElementById("create-form");
                            if (createForm) {

                                var nodes = createForm.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
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
        }
    });

    var askEditConfirmation = true;
    $('#edit-form').submit(function (e) {
        if (askEditConfirmation) {
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
                            $('#edit').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Edit State");
                            $('#edit-form').submit();

                            var createForm = document.getElementById("edit-form");
                            if (createForm) {

                                var nodes = createForm.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
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
        }
    })
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

    $('a.create').html("<i class='fas fa-sync fa-spin'></i> Add State");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
});
var state = $('#Name');
if (state) {
    state.focus();
}