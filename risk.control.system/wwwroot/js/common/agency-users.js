$(document).ready(function () {
 
    $('a.create-agency-user').on('click', function () {
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
        $('a.create-agency-user').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add User");

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
            url: '/api/Agency/GetCompanyAgencyUser?id=' + $('#vendorId').val(),
            dataSrc: ''
        },
        order: [[11, 'desc'], [12, 'desc']], // Sort by `isUpdated` and `lastModified`,
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
                targets: 9                      // Index of the column to style
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
                "data": "onlineStatus",
                "sDefaultContent": '<i class="fas fa-circle text-lightgray"></i> ',
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var iconClass = row.onlineStatusIcon || 'fas fa-circle';; // Class for the icon
                    var colorClass = getColorClass(data); // Class for the color
                    var tooltip = row.onlineStatusName || 'User status unknown'; // Tooltip text
                    var onlineStatusIcon = `<i class="${iconClass} ${colorClass}" title="${tooltip}" data-toggle="tooltip"></i>`;
                    
                    var img;
                    if (row.agentOnboarded) {
                        img = '<div class="image-container"><img alt="' + row.name + '" title="' + row.name + '" src="' + row.photo + '" class="table-profile-image" data-toggle="tooltip"/>';
                    }
                    else {
                        img = '<div class="image-container"><img alt="' + row.name + '" title="Onboarding incomplete !!! ' + row.name + '" src="' + row.photo + '" class="table-profile-image-agent-onboard" data-toggle="tooltip"/>';
                    }
                    var buttons = "";
                    buttons += '<span class="user-verified">';
                    if (row.loginVerified) {
                        buttons += '<i class="fa fa-check-circle text-light-green" title="User Login verified" data-toggle="tooltip"></i>';  // Green for checked
                    } else {
                        buttons += '<i class="fa fa-check-circle text-lightgray" title="User Login not verified" data-toggle="tooltip"></i>';  // Grey for unchecked
                    }
                    buttons += '</span>';
                    img += ' ' + buttons + '</div>';  // Close image container
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
                "data": "updatedBy",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.updatedBy + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id="edit' + row.id + '" href="/Company/EditAgencyUser?userId=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    if (row.role != "AGENCY_ADMIN") {
                        buttons += '<a id="details' + row.id + '" href="/Company/DeleteAgencyUser?userId=' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete </a>'
                    } else {
                        buttons += '<button disabled class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete </a>'
                    }

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
        },
        "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {
            if (!aData.agentOnboarded || !aData.active || !aData.loginVerified) {
                $('td', nRow).addClass('lightgrey');
            } else {
                $('td', nRow).removeClass('lightgrey');
            }
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
function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn').attr('disabled', 'disabled');
    var editbtn = $('a#edit' + id + '.btn.btn-xs.btn-warning')

    editbtn.html("<i class='fas fa-sync fa-spin'></i> Edit");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}

function showroles(id) {
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
    var rolebtn = $('a#role' + id +'.btn.btn-xs.btn-info')
    rolebtn.html("<i class='fas fa-sync fa-spin'></i> Role");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}