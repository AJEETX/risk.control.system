$(document).ready(function () {
    var table = $("#dataTable").DataTable({
        ajax: {
            url: '/api/Company/GetAvailableVendors',
            type: 'GET',
            dataType: 'json',
            dataSrc: function (json) {
                return json; // Return table data
            },
            data: function (result) {
                console.log("Data before sending:", result); // Debugging
            },
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
                else if (xhr.status === 500) {
                    $.confirm({
                        title: 'Server Error!',
                        content: 'An unexpected server error occurred. You will be redirected to Available Agencies page.',
                        type: 'orange',
                        typeAnimated: true,
                        buttons: {
                            Ok: function () {
                                window.location.href = '/AvailableAgency/Agencies';
                            }
                        },
                        onClose: function () {
                            window.location.href = '/AvailableAgency/Agencies';
                        }
                    });
                }
                else if (xhr.status === 400) {
                    $.confirm({
                        title: 'Agencies!',
                        content: 'Try with valid data. You will be redirected to Available Agencies page.',
                        type: 'orange',
                        typeAnimated: true,
                        buttons: {
                            Ok: function () {
                                window.location.href = '/AvailableAgency/Agencies';
                            }
                        },
                        onClose: function () {
                            window.location.href = '/AvailableAgency/Agencies';
                        }
                    });
                }
            }
        },
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
            targets: 3                      // Index of the column to style
        },
        {
            className: 'max-width-column', // Apply the CSS class,
            targets: 5                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 10                      // Index of the column to style
        }],
        order: [[1, 'asc']],
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
                "sDefaultContent": "<span class='i-orangered'><i class='fas fa-exclamation-triangle' data-bs-toggle='tooltip' title='Incomplete/Inactive'></i></span>",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    if (row.canOnboard) {
                        var img = '<input class="vendors" name="vendors" type="checkbox" id="' + row.id + '"  value="' + row.id + '" data-bs-toggle="tooltip" title="Select Agency to empanel" />';
                        return img;
                    }
                }
            },
            {
                "data": "id", "name": "Id", "bVisible": false
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.document + '" class="profile-image doc-profile-image" data-bs-toggle="tooltip"/>';
                    return img;
                }
            },
            {
                "data": "domain",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.vendorName + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            //{
            //    "data": "name",
            //    "mRender": function (data, type, row) {
            //        return '<span title="' + row.name + '" data-bs-toggle="tooltip">' + data + '</span>'
            //    }
            //},
            {
                "data": "phone",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-bs-toggle="tooltip"/>' + data + '</span>'
                }
            },
            {
                "data": "address",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.address + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "district",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "state",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "country",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.country + '" data-bs-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-bs-toggle="tooltip"/>' + data + '</span>';
                }
            },
            {
                "data": "updated",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "updateBy",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.updateBy + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": '<button disabled class="btn btn-xs btn-danger"><i class="fas fa-trash"></i> Delete</a>',
                "data": "deletable",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += `<a data-id="${row.id}" class="btn btn-xs btn-warning" data-bs-toggle="tooltip" title="Edit"><i class="fas fa-edit"></i> Edit</a> &nbsp;` ;
                    if (data) {
                        buttons += '<button id="' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash "></i> Delete </button>';
                    }
                    else {
                        buttons += '<button disabled class="btn btn-xs btn-danger" data-bs-toggle="tooltip" title="Delete Disabled"><i class="fa fa-trash"></i> Delete</a>';
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
            var rowCount = (this.fnSettings().fnRecordsTotal()); // total number of rows
            if (rowCount > 0) {
                $('#depanel-vendors').prop('disabled', false);
            }
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

        const url = `/AvailableAgency/Details?Id=${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = url;
        }, 1000);
    }
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }


    $('#dataTable tbody').on('click', '.btn-danger', function (e) {
        e.preventDefault();
        var $btn = $(this);
        var $spinner = $(".submit-progress"); // global spinner (you already have this)

        var id = $(this).attr('id');
        var url = '/AvailableAgency/Delete'; // Replace with your actual API URL

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
                                id: id
                            },
                            success: function (response) {
                                if (response.success) {
                                    $.alert({
                                        title: 'Deleted!',
                                        content: response.message,
                                        type: 'red',
                                        icon: 'fas fa-trash'
                                    });

                                    $('#dataTable').DataTable().ajax.reload(null, false);
                                } else {
                                    $.alert({
                                        title: 'Error!',
                                        content: response.message,
                                        type: 'red'
                                    });
                                }
                            },
                            error: function (xhr, status, error) {
                                console.error("Delete failed:", xhr.responseText);
                                $.alert({
                                    title: 'Error!',
                                    content: 'Failed to delete the Agency.',
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
    $('#dataTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
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
    // Handle click on "Select all" control
    $('#checkall').on('click', function () {
        // Get all rows with search applied
        var rows = table.rows({ 'search': 'applied' }).nodes();
        // Check/uncheck checkboxes for all rows in the table
        $('input[type="checkbox"]', rows).prop('checked', this.checked);
    });

    // Handle click on checkbox to set state of "Select all" control
    $('#dataTable tbody').on('change', 'input[type="checkbox"]', function () {
        // If checkbox is not checked
        if (!this.checked) {
            var el = $('#checkall').get(0);
            // If "Select all" control is checked and has 'indeterminate' property
            if (el && el.checked && ('indeterminate' in el)) {
                // Set visual state of "Select all" control
                // as 'indeterminate'
                el.indeterminate = true;
            }
        }
    });

    let askConfirmation = false;
    // Handle form submission event
    $('#checkboxes').on('submit', function (e) {
        var form = this;

        // Iterate over all checkboxes in the table
        table.$('input[type="checkbox"]').each(function () {
            // If checkbox doesn't exist in DOM
            if (!$.contains(document, this)) {
                // If checkbox is checked
                if (this.checked) {
                    // Create a hidden element
                    $(form).append(
                        $('<input>')
                            .attr('type', 'hidden')
                            .attr('name', this.name)
                            .val(this.value)
                    );
                }
            }
        });

        var checkboxes = $("input[type='checkbox'].vendors");
        var anyChecked = checkIfAnyChecked(checkboxes);
        if (!anyChecked) {
            e.preventDefault();
            $.alert({
                title: "Agency Empanelment !!!",
                content: "Please select agency to empanel?",
                icon: 'fas fa-exclamation-triangle',

                type: 'green',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "SELECT",
                        btnClass: 'btn-success'
                    }
                }
            });
        }
        else if (anyChecked && !askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Agency Empanel",
                content: "Are you sure?",
                icon: 'fas fa-handshake',

                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Submit",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = true;
                            $("body").addClass("submit-progress-bg");
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $(this).attr('disabled', 'disabled');
                            $(this).html("<i class='fas fa-sync fa-spin'></i> Submit");

                            $('#checkboxes').submit();
                            $('html *').css('cursor', 'not-allowed');
                            $('html a *, html button *').attr('disabled', 'disabled');
                            $('html a *, html button *').css('pointer-events', 'none')
                            $('#depanel-vendors').attr('disabled', 'disabled');
                            $('#depanel-vendors').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Empanel");

                            var article = document.getElementById("article");
                            if (article) {
                                var nodes = article.getElementsByTagName('*');
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
});

function handleDeleteButtonClick(e) {
    e.preventDefault(); // Prevent the default anchor behavior
    var id = $(this).attr('id').replace('delete', ''); // Extract the ID from the button's ID attribute
    getdetails(id); // Call the getdetails function with the ID
    window.location.href = $(this).attr('href'); // Navigate to the delete page
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

    $('a#delete' + id + '.btn.btn-xs.btn-danger').html("<i class='fas fa-sync fa-spin'></i> Delete");
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

    editbtn.html("<i class='fas fa-sync fa-spin'></i> Edit ");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}