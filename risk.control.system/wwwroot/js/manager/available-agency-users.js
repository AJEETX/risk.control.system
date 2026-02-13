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

    var table = $("#dataTable").DataTable({
        ajax: {
            url: '/api/Agency/GetCompanyAgencyUser?id=' + $('#Id').val(),
            dataSrc: '',
            error: DataTableErrorHandler
        },
        order: [[11, 'desc'], [12, 'desc']], // Sort by `isUpdated` and `lastModified`,
        columnDefs: [
            {
                className: 'max-width-column-name', // Apply the CSS class,
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
                    if (row.active) {
                        img = '<div class="image-container"><img alt="' + row.name + '" title="' + row.name + '" src="' + row.photo + '" class="table-profile-image" data-bs-toggle="tooltip"/>';
                    }
                    else {
                        img = '<div class="image-container"><img alt="' + row.name + '" title="Inactive !!! ' + row.name + '" src="' + row.photo + '" class="table-profile-image-user-inactive" data-bs-toggle="tooltip"/>';
                    }
                    var buttons = "";
                    buttons += '<span class="user-verified">';
                    if (row.loginVerified) {
                        buttons += '<i class="fa fa-check-circle text-light-green" title="User Login verified" data-bs-toggle="tooltip"></i>';  // Green for checked
                    } else {
                        buttons += '<i class="fa fa-check-circle text-lightgray" title="User Login not verified" data-bs-toggle="tooltip"></i>';  // Grey for unchecked
                    }
                    buttons += '</span>';
                    img += ' ' + buttons + '</div>';  // Close image container
                    return onlineStatusIcon + ' ' + img;
                }
            },
            {
                "data": "email",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.rawEmail + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "phone",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-bs-toggle="tooltip"/>' + data + '</span>'
                }
            },
            {
                "data": "addressline",
                bSortable: false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.addressline + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "state",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.stateName + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "pincode",
                bSortable: false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.pincodeName + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<span class="checkbox">'
                    if (row.active) {
                        buttons += '<i class="fa fa-toggle-on" title="ACTIVE" data-bs-toggle="tooltip"></i>';
                    } else {
                        buttons += '<i class="fa fa-toggle-off" title="IN-ACTIVE" data-bs-toggle="tooltip"></i>';
                    }
                    buttons += '</span>'
                    return buttons;
                }
            },
            {
                "data": "roles",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "updatedBy",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.updatedBy + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += `<a data-id="${row.id}" class="btn btn-xs btn-warning"><i class="fas fa-edit"></i> Edit</a> &nbsp;`;
                    if (row.role != "AGENCY_ADMIN") {
                        buttons += `<a data-id="${row.id}" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete</a>`;
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
        "rowCallback": function (row, data, index) {
            if (!data.agentOnboarded || !data.active || !data.loginVerified) {
                $('td', row).addClass('lightgrey');
            } else {
                $('td', row).removeClass('lightgrey');
            }
            $('.btn-warning', row).addClass('btn-black-color');
            $('.btn-danger', row).addClass('btn-white-color');
        },
        drawCallback: function (settings, start, end, max, total, pre) {
            // Reinitialize Bootstrap 5 tooltips
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (el) {
                return new bootstrap.Tooltip(el, {
                    html: true,
                    sanitize: false   // ⬅⬅⬅ THIS IS THE FIX
                });
            });
        }
    });
    $('body').on('click', 'a.btn-warning', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        showedit(id, this);
    });
    $('body').on('click', 'a.btn-danger', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        showdetail(id, this);
    });
    function showdetail(id, element) {
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        showSpinnerOnButton(element, "Delete");

        const editUrl = `/AvailableAgencyUser/Delete?userId=${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = editUrl;
        }, 1000);
    }
    function showedit(id, element) {
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        showSpinnerOnButton(element, "Edit");

        const url = `/AvailableAgencyUser/Edit?userId=${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = url;
        }, 1000);
    }
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }
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

    $('#dataTable').on('draw.dt', function () {
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