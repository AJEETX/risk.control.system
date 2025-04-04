﻿$(document).ready(function () {
    $('a.create-policy').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('a.create-policy').html("<i class='fas fa-sync fa-spin'></i> Add ");

        // Disable all buttons, submit inputs, and anchors
        $('button, input[type="submit"], a').prop('disabled', true);

        // Add a class to visually indicate disabled state for anchors
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });
        $('a').attr('disabled', 'disabled');
        $('button').attr('disabled', 'disabled');
        $('html button').css('pointer-events', 'none');
        $('html a').css({ 'pointer-events': 'none' }, { 'cursor': 'none' });
        $('.text').css({ 'pointer-events': 'none' }, { 'cursor': 'none' });

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });
    var table = $("#customerTableAuto").DataTable({
        ajax: {
            url: '/api/Creator/GetAuto',
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
            targets: 1                      // Index of the column to style
        },
        {
            className: 'max-width-column-number', // Apply the CSS class,
            targets: 2                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 6                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 8                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 9                      // Index of the column to style
        }],
        order: [[14, 'asc']],
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
                "sDefaultContent": "<i class='far fa-edit' data-toggle='tooltip' title='Incomplete'></i>",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var isPending = row.status === "PENDING"; // Check if status is "READY"
                    if (isPending) {
                        return '<i class="fas fa-exclamation-triangle" data-toggle="tooltip" title="Processing ,,,"></i>';
                    }
                    if (row.ready2Assign && row.autoAllocated) {
                        var img = '<input class="vendors" name="claims" type="checkbox" id="' + row.id + '"  value="' + row.id + '"  data-toggle="tooltip" title="Ready to assign(auto)" />';
                        return img;
                    } else if (row.ready2Assign && !row.autoAllocated) {
                        var img = '<input class="vendors" name="claims" type="checkbox" id="' + row.id + '"  value="' + row.id + '"  data-toggle="tooltip" title="Assign manually" />';
                        return img;
                    }
                }
            },
            {
                "data": "policyNum",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.policyId + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "amount",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.amount + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "pincode",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    if (row.pincodeName != '...') {
                        var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                        img += '<img src="' + row.personMapAddressUrl + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                        img += '<img src="' + row.personMapAddressUrl + '" class="full-map" title="' + row.pincodeName + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                        img += '</div>';
                        return img;
                    }
                    else {

                        return '<img src="/img/no-map.jpeg" class="profile-image doc-profile-image" title="No address" data-toggle="tooltip" />'
                    }
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.document + '" class="full-map" title="' + row.policyId + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '<img src="' + row.document + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
                    return img;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    if (row.customerFullName == "?") {
                        var img = '<img alt="' + row.customerFullName + '" title="' + row.customerFullName + '" src="' + row.customer + '" class="table-profile-image-no-user" data-toggle="tooltip"/>';
                        return img;
                    }
                    else {
                        var img = '<div class="map-thumbnail table-profile-image">';
                        img += '<img src="' + row.customer + '" class="full-map" title="' + row.customerFullName + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                        img += '<img src="' + row.customer + '" class="thumbnail table-profile-image" />'; // Thumbnail image with class 'thumbnail'
                        img += '</div>';
                        return img;
                    }
                }
            },
            {
                "data": "name",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.customerFullName + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    if (row.beneficiaryFullName == "?") {
                        var img = '<img alt="' + row.beneficiaryFullName + '" title="' + row.beneficiaryFullName + '" src="' + row.beneficiaryPhoto + '" class="table-profile-image-no-user" data-toggle="tooltip"/>';
                        return img;
                    }
                    else {
                        var img = '<div class="map-thumbnail table-profile-image">';
                        img += '<img src="' + row.beneficiaryPhoto + '" class="thumbnail table-profile-image" />'; // Thumbnail image with class 'thumbnail'
                        img += '<img src="' + row.beneficiaryPhoto + '" class="full-map" title="' + row.beneficiaryFullName + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                        img += '</div>';
                        return img;
                    }
                }
            },
            {
                "data": "beneficiaryName",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.beneficiaryFullName + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "serviceType",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.serviceType + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "location",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.location + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "created",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.created + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "timePending"
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var isPending = row.status === "PENDING"; // Check if status is "READY"
                    var disabled = isPending ? "disabled" : "";  // Disable buttons if pending
                    var spinClass = isPending ? "fa-spin" : ""; // Add spin class if pending
                    var buttons = "";
                    console.log(row.status);
                    if (isPending) {
                        buttons += '<button disabled class="btn btn-xs btn-info"><i class="fa fa-sync fa-spin"></i> ' + row.status+'</button>&nbsp;';
                        buttons += '<button disabled class="btn btn-xs btn-warning"><i class="fa fa-sync fa-spin"></i> Edit</button>&nbsp;';
                        buttons += '<button disabled class="btn btn-xs btn-danger"><i class="fa fa-sync fa-spin"></i> Delete</button>&nbsp;';
                    }

                    else {
                        if (row.ready2Assign) {
                            buttons += '<a id="assign' + row.id + '" href="/CreatorAuto/EmpanelledVendors?Id=' + row.id + '" class="btn btn-xs btn-info refresh-btn ' + disabled + '" data-id="' + row.id + '">';
                            buttons += '<i class="fas fa-external-link-alt ' + spinClass + '"></i> Assign</a>&nbsp;';
                        } else {
                            buttons += '<button disabled class="btn btn-xs btn-info"><i class="fas fa-external-link-alt"></i> Assign</button>&nbsp;';
                        }

                        buttons += '<a id="edit' + row.id + '" href="Details?Id=' + row.id + '" class="btn btn-xs btn-warning ' + disabled + '"><i class="fas fa-pencil-alt ' + disabled + '"></i> Edit</a>&nbsp;';

                        buttons += '<a id="details' + row.id + '" href="Delete?Id=' + row.id + '" class="btn btn-xs btn-danger ' + disabled + '"><i class="fa fa-trash ' + disabled + '"></i> Delete </a>';
                    }

                    return buttons;
                }
            },
            { "data": "timeElapsed", bVisible: false },
        ],
        rowCallback: function (row, data) {
            if (data.isNewAssigned) {
                $('td', row).addClass('isNewAssigned');
                // Remove the class after 3 seconds
                setTimeout(function () {
                    $('td', row).removeClass('isNewAssigned');
                }, 3000);
            }
            var $row = $(row);

            if (data.status === "PENDING") {
                // Disable the anchor tags for this row
                $(row).find('a.disabled').on('click', function (e) {
                    e.preventDefault();
                });
                $row.addClass('row-opacity-50 watermarked'); // Make row semi-transparent with watermark
            } else if (data.status === "COMPLETED") {
                $row.remove(); // Remove the row if status is "COMPLETED"
            } else {
                $row.removeClass('row-opacity-50 watermarked'); // Remove styling for other statuses
            }
        },
        "drawCallback": function (settings) {
            var api = this.api();
            var rowCount = (this.fnSettings().fnRecordsTotal()); // total number of rows
            if (rowCount > 0 && hasAssignedRows()) {
                $('.top-info').prop('disabled', false);
                $('#allocatedcase').prop('disabled', false);
                $('#deletecase').prop('disabled', false);
                var pendingRows = hasPendingRows();
                if (pendingRows) {
                    table.ajax.reload(null, false);
                    $('#checkall').prop('checked', false);
                }
            }
            else {
                $('.top-info').prop('disabled', true);
                $('#allocatedcase').prop('disabled', true);
                $('#deletecase').prop('disabled', true);
            }
            $('#customerTableAuto tbody').on('click', '.btn-info', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('assign', ''); // Extract the ID from the button's ID attribute
                assign(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
            $('#customerTableAuto tbody').on('click', '.btn-danger', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('details', ''); // Extract the ID from the button's ID attribute
                getdetails(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
            $('#customerTableAuto tbody').on('click', '.btn-warning', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('edit', ''); // Extract the ID from the button's ID attribute
                showedit(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the edit page
            });
        },
        error: function (xhr, status, error) { alert('err ' + error) }
    });

    function hasAssignedRows() {
        var table = $("#customerTableAuto").DataTable();
        var assignedExists = false;

        table.rows().every(function () {
            var data = this.data();
            if (data.ready2Assign === true) {
                assignedExists = true;
                return false; // Stop iterating once an "assigned" row is found
            }
        });

        return assignedExists;
    }
        // Function to check if there are any "Pending" rows
    function hasPendingRows() {
        var table = $("#customerTableAuto").DataTable();
        var pendingExists = false;

        table.rows().every(function () {
            var data = this.data();
            if (data.status === "PENDING") {
                pendingExists = true;
                return false; // Stop iterating once a "Pending" row is found
            }
        });

        return pendingExists;
    }

    function refreshPendingRows() {
        var table = $("#customerTableAuto").DataTable();
        table.rows().every(function () {
            var data = this.data();
            if (data) {
                console.log(data.status);
                if (data.status === "PENDING") {
                    var row = this.node();
                    refreshRowData(data.id, row); // Refresh each pending row
                }
                else if (data.status === "COMPLETED") {
                    this.remove(); // Remove the row
                }
            }
        });
    }
    // Function to refresh data for a specific row
    function refreshRowData(rowId, rowElement) {
        $.ajax({
            url: '/api/Creator/RefreshData', // URL for the refresh data endpoint
            type: 'GET',
            data: { id: rowId },
            success: function (data) {
                // Update the row's data with the refreshed data
                var table = $("#customerTableAuto").DataTable();
                if (data) {
                    table.row(rowElement).data(data).draw();
                }
            },
            error: function (xhr, status, error) {
                console.error('Error refreshing row data:', error);
            }
        });
    }

    // Function to refresh data for a specific row
    function refreshRowData(rowId, rowElement) {
        $.ajax({
            url: '/api/Creator/RefreshData', // URL for the refresh data endpoint
            type: 'GET',
            data: { id: rowId },
            success: function (data) {
                // Update the row's data with the refreshed data
                var table = $("#customerTableAuto").DataTable();
                if (data) {
                    // Find the row in DataTable using rowId
                    var row = table.row(`#row_${rowId}`); // Assuming row ID is set as `id="row_123"`

                    if (row.any()) {  // Check if the row exists
                        row.data(data).draw();
                    } else {
                        console.warn(`Row with ID ${rowId} not found in DataTable.`);
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error('Error refreshing row data:', error);
            }
        });
    }
    // Call refreshPendingRows() periodically but only if there are pending rows
    var refreshInterval = setInterval(function () {
        if (!hasPendingRows()) {
            clearInterval(refreshInterval); // Stop refreshing if no rows are "Pending"
        } else {
            refreshPendingRows(); // Refresh pending rows
        }
    }, 5000); // Check every 5 seconds (adjust interval as needed)

    $('#refreshTable').click(function () {
        var $icon = $('#refreshIcon');
        if ($icon) {
            $icon.addClass('fa-spin');
        }
        table.ajax.reload(null, false);
        $('#checkall').prop('checked', false);
    });
    table.on('xhr.dt', function () {
        $('#refreshIcon').removeClass('fa-spin');
    });
    var refreshInterval = 3000; // 3 seconds interval
    var maxAttempts = 3; // Prevent infinite loop
    var attempts = 0;
    var initialCount = sessionStorage.getItem("InitialRecordCount");

    // Check if a refresh is needed after upload
    var refreshDatatble = sessionStorage.getItem("RefreshDataTable");
    if (sessionStorage.getItem("RefreshDataTable") == "RefreshDataTable") {
        if (initialCount === null) {
            initialCount = table.data().count(); // Save the current record count
            sessionStorage.setItem("InitialRecordCount", initialCount);
        } else {
            initialCount = parseInt(initialCount, 10); // Convert to number
        }
        pollForNewData();
        sessionStorage.removeItem("RefreshDataTable"); // Clear the refresh flag
    }

    function pollForNewData() {
        attempts++;

        if (attempts > maxAttempts) {
            console.log("Max attempts reached, stopping refresh.");
            sessionStorage.removeItem("InitialRecordCount"); // Clean up
            return;
        }

        console.log("Refreshing DataTable... Attempt: " + attempts);

        table.ajax.reload(function () {
            var newCount = table.data().count(); // Get updated row count

            if (newCount > initialCount) {
                console.log("New records detected! Stopping refresh.");
                sessionStorage.removeItem("InitialRecordCount"); // Clean up
            } else {
                setTimeout(pollForNewData, refreshInterval);
            }
        }, false);
    }
    
    table.on('mouseenter', '.map-thumbnail', function () {
            const $this = $(this); // Cache the current element

            // Set a timeout to show the full map after 1 second
            hoverTimeout = setTimeout(function () {
                $this.find('.full-map').show(); // Show full map
            }, 1000); // Delay of 1 second
        })
        .on('mouseleave', '.map-thumbnail', function () {
            const $this = $(this); // Cache the current element

            // Clear the timeout to cancel showing the map
            clearTimeout(hoverTimeout);

            // Immediately hide the full map
            $this.find('.full-map').hide();
        });

    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });

    $('#customerTableAuto tbody').hide();
    $('#customerTableAuto tbody').fadeIn(2000);

    // Handle click on "Select all" control
    $('#checkall').on('click', function () {
        // Get all rows with search applied
        var rows = table.rows({ 'search': 'applied' }).nodes();
        // Check/uncheck checkboxes for all rows in the table
        $('input[type="checkbox"]', rows).prop('checked', this.checked);
    });

    // Handle click on checkbox to set state of "Select all" control
    $('#customerTableAuto tbody').on('change', 'input[type="checkbox"]', function () {
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
                title: "ASSIGN !",
                content: "Please select Case<span class='badge badge-light'>(s)</span> to Assign",
                icon: 'fas fa-random fa-sync',
                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "OK",
                        btnClass: 'btn-danger'
                    }
                }
            });
        }
        else if (!askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Assign",
                content: "Are you sure to Assign ?",
                icon: 'fas fa-random',
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Assign ",
                        btnClass: 'btn-warning',
                        action: function () {
                            askConfirmation = true;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                           
                            $('#allocatedcase').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Assign");

                            disableAllInteractiveElements();

                            $('#checkboxes').submit();
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

    $('#deletecase').on('click', function (e) {
        e.preventDefault(); // Prevent form submission

        var selectedCases = [];
        $("input[type='checkbox'].vendors:checked").each(function () {
            selectedCases.push($(this).val()); // Get checked case IDs
        });

        if (selectedCases.length === 0) {
            $.alert({
                title: "Delete !",
                content: "Please select Case(s) to delete.",
                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "OK",
                        btnClass: 'btn-danger'
                    }
                }
            });
            return;
        }

        $.confirm({
            title: "Confirm Delete",
            content: "Are you sure you want to delete selected case(s) <span class='badge badge-light'>max 10 </span>?",
            icon: 'fas fa-trash',
            type: 'red',
            closeIcon: true,
            buttons: {
                confirm: {
                    text: "Delete",
                    btnClass: 'btn-danger',
                    action: function () {
                        deleteSelectedCases(selectedCases);
                    }
                },
                cancel: {
                    text: "Cancel",
                    btnClass: 'btn-default'
                }
            }
        });
    });
    function deleteSelectedCases(claims) {
        $.ajax({
            url: "/CreatorPost/DeleteCases", // Update with your actual delete endpoint
            type: "POST",
            data: JSON.stringify({ claims: claims }),
            contentType: "application/json",
            success: function (response) {
                if (response.success) {
                    $.alert({
                        title: "Deleted!",
                        content: "Selected case(s) have been deleted.",
                        type: 'red',
                        buttons: {
                            ok: {
                                text: "OK",
                                btnClass: 'btn-danger'
                            }
                        }
                    });

                    // Refresh DataTable
                    $('#customerTableAuto').DataTable().ajax.reload(null, false);
                    $('#checkall').prop('checked', false);

                } else {
                    $.alert({
                        title: "Error!",
                        content: response.message || "Failed to delete cases.",
                        type: 'red'
                    });
                }
            },
            error: function () {
                $.alert({
                    title: "Error!",
                    content: "Something went wrong. Please try again.",
                    type: 'red'
                });
            }
        });
    }

    $("#postedFile").on('change', function () {
        var MaxSizeInBytes = 1097152;
        //Get count of selected files
        var countFiles = $(this)[0].files.length;

        var imgPath = $(this)[0].value;
        var extn = imgPath.substring(imgPath.lastIndexOf('.') + 1).toLowerCase();

        if (extn == "zip") {
            if (typeof (FileReader) != "undefined") {

                //loop for each file selected for uploaded.
                for (var i = 0; i < countFiles; i++) {
                    var fileSize = $(this)[0].files[i].size;
                    if (fileSize > MaxSizeInBytes) {
                        $.alert(
                            {
                                title: " UPLOAD issue !",
                                content: " <i class='fa fa-upload'></i> Upload File size limit exceeded. <br />Max file size is 1 MB!",
                                icon: 'fas fa-exclamation-triangle',
                                type: 'red',
                                closeIcon: true,
                                buttons: {
                                    cancel: {
                                        text: "CLOSE",
                                        btnClass: 'btn-danger'
                                    }
                                }
                            }
                        );
                    }
                }

            } else {
                $.alert(
                    {
                        title: "Outdated Browser !",
                        content: "This browser does not support FileReader. Try on modern browser!",
                        icon: 'fas fa-exclamation-triangle',

                        type: 'red',
                        closeIcon: true,
                        buttons: {
                            cancel: {
                                text: "CLOSE",
                                btnClass: 'btn-danger'
                            }
                        }
                    }
                );
            }
        } else {
            $.alert(
                {
                    title: "FILE UPLOAD TYPE !!",
                    content: "Pls only select file with extension zip ! ",
                    icon: 'fas fa-exclamation-triangle',

                    type: 'red',
                    closeIcon: true,
                    buttons: {
                        cancel: {
                            text: "CLOSE",
                            btnClass: 'btn-danger'
                        }
                    }
                }
            );
        }
    });
    let askFileUploadConfirmation = true;
    let askConfirmation = false;
    function handleUploadConfirmation(formId, buttonId) {
        $(formId).on('submit', function (event) {
            if (askFileUploadConfirmation) {
                event.preventDefault();
                $.confirm({
                    title: "Confirm File Upload",
                    content: "Are you sure to Upload?",
                    icon: 'fas fa-upload',
                    type: 'green',
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: "File Upload",
                            btnClass: 'btn-success',
                            action: function () {
                                askFileUploadConfirmation = false;

                                $("body").addClass("submit-progress-bg");
                                setTimeout(function () {
                                    $(".submit-progress").removeClass("hidden");
                                }, 1);

                                $(buttonId).html("<i class='fas fa-sync fa-spin'></i> Uploading");
                                disableAllInteractiveElements();
                                setRefreshFlag();
                                $(formId).submit();

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
    }

    // Apply confirmation to both forms
    handleUploadConfirmation("#upload-claims", "#UploadFileButton");
});

function setRefreshFlag() {
    var table = $('#customerTableAuto').DataTable();
    sessionStorage.setItem("InitialRecordCount", table.data().count()); // Save count before reload
    sessionStorage.setItem("RefreshDataTable", 'RefreshDataTable');
    console.log('stored data')
}
function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    
    $('a#edit' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    disableAllInteractiveElements();

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
   

    $('a#details' + id + '.btn.btn-xs.btn-danger').html("<i class='fas fa-sync fa-spin'></i> Delete");
    disableAllInteractiveElements();
    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}
function assign(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);


    $('a#assign' + id + '.btn.btn-xs.btn-info').html("<i class='fas fa-sync fa-spin'></i> Assign");
    disableAllInteractiveElements();
    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}