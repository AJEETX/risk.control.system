$(document).ready(function () {
    $('a.create-agency-service').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('a.create-agency-service').attr('disabled', 'disabled');
        $('a.create-agency-service').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Service");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/Company/AllServices?id=' + $('#vendorId').val(),
            dataSrc: ''
        },
        order: [[11, 'desc'], [12, 'desc']], // Sort by `isUpdated` and `lastModified`,
        columnDefs: [
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 1                      // Index of the column to style
            },
            {
                className: 'max-width-column', // Apply the CSS class,
                targets: 7                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 8                      // Index of the column to style
            }],
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            /* Name of the keys from
            data file source */
            {
                "data": "id", "name": "Id", "bVisible": false
            },
            {
                "data": "caseType",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.caseType + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "serviceType",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.serviceType + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "rate",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.rate + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "district",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.district + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "state",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.state + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "country",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.country + '" data-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-toggle="tooltip"/>' + data + '</span>';
                }
            },
            {
                "data": "pincodes",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.rawPincodes + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "updatedBy",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.updatedBy + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "updated",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.updated + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id=edit' + row.id + ' href="/Company/EditService?id=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    buttons += '<a id=delete' + row.id + ' href="/Company/DeleteService?id=' + row.id + '"  class="btn btn-xs btn-danger"><i class="fas fa-trash"></i> Delete</a>'
                    return buttons;
                }
            },
            {
                "data": "isUpdated",
                bVisible: false
            },
            {
                "data": "lastModified",
                bVisible: false
            }
        ],
        drawCallback: function () {
            // Event delegation for .btn-danger elements
            $('#customerTable tbody').on('click', '.btn-danger', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('details', ''); // Extract the ID from the button's ID attribute
                getdetails(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
            $('#customerTable tbody').on('click', '.btn-warning', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('edit', ''); // Extract the ID from the button's ID attribute
                showedit(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the edit page
            });
        },
        error: function (xhr, status, error) { alert('err ' + error) }
    });

    table.on('draw', function () {
        table.rows().every(function () {
            var data = this.data(); // Get row data
            console.log(data); // Debug row data

            if (data.isUpdated) { // Check if the row should be highlighted
                var rowNode = this.node();

                // Highlight the row
                $(rowNode).addClass('highlight-new-user');

                // Scroll the row into view
                rowNode.scrollIntoView({ behavior: 'smooth', block: 'center' });

                // Optionally, remove the highlight after a delay
                setTimeout(function () {
                    $(rowNode).removeClass('highlight-new-user');
                }, 3000);
            }
        });
    });

    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'bottom',
            html: true
        });
    });
});

function showdetails(id) {
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

    var detailbtn = $('a#details' + id + '.btn.btn-xs.btn-info')
    detailbtn.html("<i class='fas fa-sync fa-spin'></i> Detail");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}
function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    var editbtn = $('a#edit' + id + '.btn.btn-xs.btn-warning');
    // Disable all buttons, submit inputs, and anchors
    $('button, input[type="submit"], a').prop('disabled', true);

    // Add a class to visually indicate disabled state for anchors
    $('a').addClass('disabled-anchor').on('click', function (e) {
        e.preventDefault(); // Prevent default action for anchor clicks
    });
    editbtn.html("<i class='fas fa-sync fa-spin'></i> Edit");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}
function getdetails(id) {
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
    var _delete = $('a#delete' + id + '.btn.btn-xs.btn-danger');
    _delete.html("<i class='fas fa-sync fa-spin'></i> Delete");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}