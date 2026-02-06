$(document).ready(function () {
    var table = $("#dataTable").DataTable({
        ajax: {
            url: '/api/Company/AllUsers',
            dataSrc: '',
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    $.confirm({
                        title: 'Session Expired!',
                        content: 'Your session has expired or you are unauthorized. You will be redirected to the login page.',
                        type: 'red',
                        typeAnimated: true,
                        buttons: {
                            Ok: {
                                text: 'Login',
                                btnClass: 'btn-red',
                                action: function () {
                                    window.location.href = '/Account/Login';
                                }
                            }
                        },
                        onClose: function () {
                            window.location.href = '/Account/Login';
                        }
                    });
                }
            }
        },
        order: [[1, 'desc'], [12, 'desc'], [13, 'desc']], // Sort by `isUpdated` and `lastModified`,
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
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 2                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 4                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 8                      // Index of the column to style
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
                    var onlineStatusIcon = `<i class="${iconClass} ${colorClass}" title="${tooltip}" data-bs-toggle="tooltip"></i>`;

                    // Render the user profile image
                    var img;
                    if (row.active) {
                        img = '<div class="image-container"><img alt="' + row.name + '" title="' + row.name + '" src="' + row.photo + '" class="table-profile-image" data-bs-toggle="tooltip"/>';
                    } else {
                        img = '<div class="image-container"><img alt="' + row.name + '" title="Inactive!!! ' + row.name + '" src="' + row.photo + '" class="table-profile-image-user-inactive" data-bs-toggle="tooltip"/>';
                    }

                    // Add login verification icon
                    var buttons = '<span class="user-verified">';
                    if (row.loginVerified) {
                        buttons += '<i class="fa fa-check-circle text-light-green" title="User Login Verified" data-bs-toggle="tooltip"></i>';  // Green for verified
                    } else {
                        buttons += '<i class="fa fa-check-circle text-lightgray" title="User Login Not Verified" data-bs-toggle="tooltip"></i>';  // Grey for unverified
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
                    return '<span title="' + row.rawEmail + '" data-bs-toggle="tooltip">' + row.rawEmail + '</span>'
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
                    buttons += '<span class="checkbox">';
                    if (row.active) {
                        buttons += '<i class="fa fa-toggle-on" title="ACTIVE" data-bs-toggle="tooltip"></i>';
                    } else {
                        buttons += '<i class="fa fa-toggle-off" title="IN-ACTIVE" data-bs-toggle="tooltip"></i>';
                    }
                    buttons += '</span>';
                    return buttons;
                }
            },
            {
                "data": "role",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "updated",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "updatedBy",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += `<a data-id="${row.id}" class="btn btn-xs btn-warning"><i class="fas fa-edit"></i> Edit</a> &nbsp;`;
                    if (row.role !== "COMPANY_ADMIN") {
                        buttons += '<button class="btn btn-xs btn-danger btn-delete" data-id="' + row.id + '"><i class="fa fa-trash"></i> Delete</button>';
                    } else {
                        buttons += '<button disabled class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete</button>';
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
    function showedit(id, element) {
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        showSpinnerOnButton(element, "Edit");

        const url = `/ManageCompanyUser/Edit?userId=${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = url;
        }, 1000);
    }
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }

    $(document).on('click', '.btn-delete', function (e) {
        e.preventDefault();
        var $btn = $(this);
        var $spinner = $(".submit-progress"); // global spinner (you already have this
        const userId = $(this).data('id');
        const url = '/ManageCompanyUser/Delete';
        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete this case?',
            type: 'red',
            icon: 'fas fa-trash',
            buttons: {
                confirm: {
                    text: 'Yes, delete it',
                    btnClass: 'btn-red',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Delete');

                        $.ajax({
                            url: url,
                            type: 'POST',
                            data: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                userId: userId
                            },
                            success: function (response) {
                                // Show success message
                                $.alert({
                                    title: 'Deleted!',
                                    content: response.message,
                                    closeIcon: true,
                                    type: 'red',
                                    icon: 'fas fa-trash',
                                    buttons: {
                                        ok: {
                                            text: 'Close',
                                            btnClass: 'btn-default',
                                        }
                                    }
                                });

                                // Reload the DataTable
                                $('#dataTable').DataTable().ajax.reload(null, false); // false = don't reset paging
                            },
                            error: function (xhr, status, error) {
                                console.error("Delete failed:", xhr.responseText);
                                $.alert({
                                    title: 'Error!',
                                    content: 'Failed to delete the case.',
                                    type: 'red'
                                });
                                if (xhr.status === 401 || xhr.status === 403) {
                                    window.location.href = '/Account/Login';
                                } else {
                                    $.alert({
                                        title: 'Error!',
                                        content: 'Unexpected error occurred.',
                                        type: 'red'
                                    });
                                }
                            },
                            complete: function () {
                                $spinner.addClass("hidden");
                                // ✅ Re-enable button and restore text
                                $btn.prop("disabled", false).html('<i class="fas fa-trash"></i> Delete');
                            }
                        });
                    }
                },
                cancel: function () {
                    // Do nothing
                }
            }
        });
    });
    table.on('draw', function () {
        table.rows().every(function () {
            var data = this.data(); // Get row data

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
    var editbtn = $('a#edit' + id + '.btn.btn-xs.btn-warning')
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