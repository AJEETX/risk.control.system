﻿$(document).ready(function () {

    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/Company/AllUsers',
            dataSrc: '',
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Account/Login'; // Or session timeout handler
                }
            }
        },
        order: [[1, 'desc'],[12, 'desc'], [13, 'desc']], // Sort by `isUpdated` and `lastModified`,
        columnDefs: [{
            'targets': 0,
            'searchable': false,
            'orderable': false,
            'className': 'dt-body-center',
            'render': function (data, type, full, meta) {
                return '<input type="checkbox" name="id[]" value="' + $('<div/>').text(data).html() + '">';
            }
        },
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
                "data": "onlineStatus",  // This data can be used to determine online status
                "sDefaultContent": '<i class="fas fa-circle text-lightgray"></i>',
                "bSortable": false,
                "mRender": function (data, type, row) {
                    // Get the appropriate class for the online status icon
                    var iconClass = row.onlineStatusIcon || 'fas fa-circle'; // Default to 'fa-circle' if no icon class is available
                    var colorClass = getColorClass(data); // A function that returns a color class (e.g., 'text-success' for online)
                    var tooltip = row.onlineStatusName || 'User status unknown'; // Tooltip text for the status

                    // Render the online status icon
                    var onlineStatusIcon = `<i class="${iconClass} ${colorClass}" title="${tooltip}" data-toggle="tooltip"></i>`;

                    // Render the user profile image
                    var img;
                    if (row.active) {
                        img = '<div class="image-container"><img alt="' + row.name + '" title="' + row.name + '" src="' + row.photo + '" class="table-profile-image" data-toggle="tooltip"/>';
                    } else {
                        img = '<div class="image-container"><img alt="' + row.name + '" title="Inactive!!! ' + row.name + '" src="' + row.photo + '" class="table-profile-image-user-inactive" data-toggle="tooltip"/>';
                    }

                    // Add login verification icon
                    var buttons = '<span class="user-verified">';
                    if (row.loginVerified) {
                        buttons += '<i class="fa fa-check-circle text-light-green" title="User Login Verified" data-toggle="tooltip"></i>';  // Green for verified
                    } else {
                        buttons += '<i class="fa fa-check-circle text-lightgray" title="User Login Not Verified" data-toggle="tooltip"></i>';  // Grey for unverified
                    }
                    buttons += '</span>';

                    // Combine the online status icon, profile image, and login verification button in one column
                    img += ' ' + buttons + '</div>'; // Close image container

                    // Return both the online status and the user profile in a single column
                    return onlineStatusIcon + ' ' + img;
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
                "data": "addressline",
                bSortable: false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.addressline + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "state",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.stateName + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "pincode",
                bSortable: false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.pincodeName + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<span class="checkbox">';
                    if (row.active) {
                        buttons += '<i class="fa fa-toggle-on" title="ACTIVE" data-toggle="tooltip"></i>';
                    } else {
                        buttons += '<i class="fa fa-toggle-off" title="IN-ACTIVE" data-toggle="tooltip"></i>';
                    }
                    buttons += '</span>';
                    return buttons;
                }
            },
            {
                "data": "role",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "updated",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "updatedBy",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id=edit' + row.id + '  href="/Company/EditUser?userId=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    if (row.role != "COMPANY_ADMIN") {
                        buttons += '<a id="details' + row.id + '" href="/Company/DeleteUser?userId=' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete </a>'
                    } else {
                        buttons += '<button disabled class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete </a>'
                    }
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
        "rowCallback": function (row, data, index) {
            if (!data.active || !data.loginVerified) {
                $('td', row).addClass('lightgrey');
            } else {
                $('td', row).removeClass('lightgrey');
            }
        },
        "drawCallback": function (settings, start, end, max, total, pre) {

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
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });

    // Event delegation for 'Edit' button click
    $(document).on('click', 'a[id^="edit"]', function (e) {
        e.preventDefault(); // Prevent the default link behavior
        var id = $(this).attr('id').replace('edit', ''); // Get the ID from the button's ID
        showedit(id); // Call the function with the ID
        window.location.href = $(this).attr('href'); // Navigate to the edit page
    });

    // Event delegation for 'Delete' button click
    $(document).on('click', 'a[id^="details"]', function (e) {
        e.preventDefault(); // Prevent the default link behavior
        var id = $(this).attr('id').replace('details', ''); // Get the ID from the button's ID
        getdetails(id); // Call the function with the ID
        window.location.href = $(this).attr('href'); // Navigate to the delete page
    });

    $('a.create').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $(this).attr('disabled', 'disabled');
        $(this).html("<i class='fas fa-sync fa-spin'></i> Add User");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
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
function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    var editbtn = $('a#edit' + id +'.btn.btn-xs.btn-warning')
    $('.btn.btn-xs.btn-warning').attr('disabled', 'disabled');
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

    $('a#details' + id + '.btn.btn-xs.btn-danger').html("<i class='fas fa-sync fa-spin'></i> Delete");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}