$(document).ready(function () {
    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/User/AllUsers',
            dataSrc: '',
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Account/Login'; // Or session timeout handler
                }
            }
        },
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columnDefs: [
            {
                className: 'max-width-column', // Apply the CSS class,
                targets: 2                      // Index of the column to style
            },
            {
                className: 'max-width-column', // Apply the CSS class,
                targets: 4                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 10                      // Index of the column to style
            }],
        order: [[11, 'desc'], [12, 'desc']], // Sort by `isUpdated` and `lastModified`,
        columns: [
            /* Name of the keys from
            data file source */
            {
                "data": "id", "name": "Id", "bVisible": false
            },
            {
                "data": "onlineStatus",
                "sDefaultContent": '<i class="fas fa-circle text-lightgray"></i> ',
                "bSortable": false,
                "mRender": function (data, type, row) {
                    // Get the appropriate class for the online status icon
                    var iconClass = row.onlineStatusIcon || 'fas fa-circle'; // Default to 'fa-circle' if no icon class is available
                    var colorClass = getColorClass(data); // A function that returns a color class (e.g., 'text-success' for online)
                    var tooltip = row.onlineStatusName || 'User status unknown'; // Tooltip text for the status

                    // Render the online status icon
                    var onlineStatusIcon = `<i class="${iconClass} ${colorClass}" title="${tooltip}" data-toggle="tooltip"></i>`;
                    
                    var img = '<div class="image-container"><img alt="' + row.name + '" title="' + row.name + '" src="' + row.photo + '" class="table-profile-image" data-toggle="tooltip"/>';
                    var buttons = "";
                    buttons += '<span class="user-verified">';
                    if (row.loginVerified) {
                        buttons += '<i class="fa fa-check-circle text-light-green" title="User Login verified" data-toggle="tooltip"></i>';  // Green for checked
                    } else {
                        buttons += '<i class="fa fa-check-circle text-lightgray" title="User Login not verified" data-toggle="tooltip"></i>';  // Grey for unchecked
                    }
                    buttons += '</span>';
                    img += ' ' + buttons + '</div>';  // Close image container
                    return onlineStatusIcon + ' '+img;
                }
            },
            {
                "data": "email",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.rawEmail + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "phone",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-toggle="tooltip"/>' + data + '</span>'
                }
            },
            {
                "data": "addressline", bSortable: false,
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "district",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "state",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "country",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.country + '" data-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-toggle="tooltip"/>' + data + '</span>';
                }
            },
            {
                "data": "pincode",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<span class="checkbox">'
                    if (row.active) {
                        buttons += '<i class="fa fa-toggle-on" title="ACTIVE" data-toggle="tooltip"></i>';
                    } else {
                        buttons += '<i class="fa fa-toggle-off" title="IN-ACTIVE" data-toggle="tooltip"></i>';
                    }
                    buttons += '</span>'
                    return buttons;
                }
            },
            {
                "data": "roles",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id=edit' + row.id + ' href="/User/Edit?userId=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-user-minus"></i> Edit</a>&nbsp;'
                    return buttons;
                }
            },
            {
                "data": "isUpdated",
                "bVisible": false
            },
            {
                "data": "lastModified",
                bVisible: false
            }
        ],
        "drawCallback": function (settings, start, end, max, total, pre) {
            // Event delegation for .btn-danger elements
            
            $('#customerTable tbody').on('click', '.btn-warning', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('edit', ''); // Extract the ID from the button's ID attribute
                showedit(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the edit page
            });
        }
    });
    table.on('draw', function () {
        table.rows().every(function () {
            var data = this.data(); // Get row data
            console.log(data); // Debug row data

            if (data.isUpdated) { // Check if the row should be highlighted
                var rowNode = this.node();

                // Highlight the row
                $(rowNode).addClass('highlight-new-user');

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
            placement: 'top',
            html: true
        });
    });
});

function getColorClass(color) {
    switch (color.toLowerCase()) {
        case "green":
            return "online-status-green";

        case "orange":
            return "online-status-orange";
        default:
            return "online-icon-default"; // Fallback class
    }
}
function showroles() {
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

    $('a.btn.btn-info').html("<i class='fas fa-sync fa-spin'></i> Roles");

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
    var editbtn = $('a#edit' + id + '.btn.btn-xs.btn-warning')
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
