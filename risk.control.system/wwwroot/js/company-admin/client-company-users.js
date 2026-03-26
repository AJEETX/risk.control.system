$(document).ready(function () {
    $('.btn.btn-success').on('click', function () {
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
        $('.btn.btn-success').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add User");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    var id = $('#companyId').val();
    var table = $("#dataTable").DataTable({
        ajax: {
            url: '/api/PortalCompany/CompanyUsers?id=' + id,
            dataSrc: '',
            error: DataTableErrorHandler,
        },
        fixedHeader: true,
        processing: true,
        paging: true,
        columnDefs: [
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 2                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 3                      // Index of the column to style
            },
            {
                className: 'max-width-column', // Apply the CSS class,
                targets: 4                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 9                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 10                      // Index of the column to style
            }],
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
                    var iconClass = row.onlineStatusIcon; // Class for the icon
                    var colorClass = getColorClass(data); // Class for the color
                    var tooltip = row.onlineStatusName; // Tooltip text
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
                    return onlineStatusIcon + ' ' + img;
                }
            },
            {
                "data": "email",
                "mRender": function (data, type, row) {
                    return '<span class="blue" title="' + data + '" data-toggle="tooltip">' + data + '</span>'
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
                "data": "state",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "pincode",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.pincodeLabel + '" data-toggle="tooltip">' + data + '</span>'
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
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
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
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<button data-id=' + row.id + ' class="btn btn-xs btn-warning"><i class="fas fa-edit"></i> Edit</button>'
                    //buttons += '<a href="/CompanyUserRoles/Index?userId=' + row.id + '"  class="btn btn-xs btn-info"><i class="fas fa-pen"></i> Roles</a>'
                    return buttons;
                }
            }
        ],
        "rowCallback": function (row, data, index) {
            if (!data.active || !data.loginVerified) {
                $('td', row).addClass('lightgrey');
            } else {
                $('td', row).removeClass('lightgrey');
            }
        }
    });
    $('body').on('click', 'button.btn-xs.btn-warning', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        showedit(id, this);
    });
    function showedit(id, element) {
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        showSpinnerOnButton(element, "Edit");

        const url = `/CompanyUser/Edit/${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = url;
        }, 1000);
    }
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }
    $('#dataTable').on('draw.dt', function () {
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
function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    var editbtn = $('button#' + id + '.btn.btn-xs.btn-warning')
    // Disable all buttons, submit inputs, and anchors
    $('button, input[type="submit"], a').prop('disabled', true);

    // Add a class to visually indicate disabled state for anchors
    $('button').addClass('disabled-anchor').on('click', function (e) {
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