$(document).ready(function () {
    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/Company/GetAvailableVendors',
            dataSrc: ''
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
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 4                      // Index of the column to style
            },
            {
                className: 'max-width-column', // Apply the CSS class,
                targets: 7                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 12                      // Index of the column to style
            }],
        order: [[14, 'desc'], [15, 'desc']], // Sort by `isUpdated` and `lastModified`,
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
                "sDefaultContent": "<span><i class='far fa-edit' data-toggle='tooltip' title='NOT ACTIVE and/or No service available'></i></span>",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    if (row.canOnboard){
                        var img = '<input class="vendors" name="vendors" type="checkbox" id="' + row.id + '"  value="' + row.id + '"  />';
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
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.document + '" class="profile-image doc-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            {
                "data": "domain",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.vendorName + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "name",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.name + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "code",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "phone",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-toggle="tooltip"/>' + data + '</span>'
                }
            },
            {
                "data": "address",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.address + '" data-toggle="tooltip">' + data + '</span>'
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
                "data": "updated",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "updateBy",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.updateBy + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    //buttons += '<a id=details' + row.id + ' onclick="showdetails(' + row.id + ')" href="/Vendors/Details?Id=' + row.id + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Details</a>&nbsp;'
                    //buttons += '<a onclick="showedit()" href="/Vendors/Edit?Id=' + row.id + '"  class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    buttons += '<a id=delete' + row.id + ' onclick="getdetails(' + row.id + ')" href="/Vendors/Delete?Id=' + row.id + '"  class="btn btn-xs btn-danger"><i class="fas fa-trash"></i></i> Delete</a>'
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
                var id = $(this).attr('id').replace('delete', ''); // Extract the ID from the button's ID attribute
                getdetails(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });

        },
        "drawCallback": function (settings, start, end, max, total, pre) {
            var rowCount = (this.fnSettings().fnRecordsTotal()); // total number of rows
            if (rowCount > 0) {
                $('#depanel-vendors').prop('disabled', false);
            }
        },
        error: function (xhr, status, error) { alert('err ' + error) }
    });

    $('#customerTable').on('draw.dt', function () {
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

                // Scroll the row into view
                rowNode.scrollIntoView({ behavior: 'smooth', block: 'center' });

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
    $('#customerTable tbody').on('change', 'input[type="checkbox"]', function () {
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
