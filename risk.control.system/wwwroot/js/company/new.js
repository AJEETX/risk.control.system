$(document).ready(function () {
    $('a.create-policy').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('a.create-policy').html("<i class='fas fa-sync fa-spin'></i> Add New");

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
            url: window.location.origin + '/api/Investigation/GetAuto',
            type: 'GET',
            dataType: 'json',
            dataSrc: function (json) {
                // Check extraField boolean value from server response
                if (json.autoAllocatopn) {
                    $('#allocatedcase').show();  // Show the button
                } else {
                    $('#allocatedcase').hide();  // Hide the button
                }

                return json.data; // Return table data
            },
            data: function (d) {
                console.log("Data before sending:", d); // Debugging

                return {
                    draw: d.draw || 1,
                    start: d.start || 0,
                    length: d.length || 10,
                    caseType: $('#caseTypeFilter').val() || "",  // Send selected filter value
                    search: d.search?.value || "", // Instead of empty string, send "all"
                    orderColumn: d.order?.[0]?.column ?? 14, // Default to column 15
                    orderDir: d.order?.[0]?.dir || "desc"
                };
            },
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Account/Login'; // Or session timeout handler
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
            },
            {
                'targets': 15, // Index for the "Case Type" column
                'name': 'policy' // Name for the "Case Type" column
            }],
        order: [[14, 'asc']],
        responsive: true,
        fixedHeader: true,
        processing: true,
        serverSide: true,
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
                    if (!row.ready2Assign) {
                        return '<i class="fas fa-exclamation-triangle" data-toggle="tooltip" title="Incomplete"></i>';
                    }
                    else {
                        var img = '<input class="vendors" name="claims" type="checkbox" id="' + row.id + '"  value="' + row.id + '"  data-toggle="tooltip" title="Assign/delete" />';
                        return img;
                    }
                }
            },
            {
                "data": "policyNum",
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
                "sDefaultContent": "<i class='fa-map-marker' data-toggle='tooltip' title='No address'></i>",
                "data": "pincode",
                "mRender": function (data, type, row) {
                    if (row.pincodeName != '...') {
                        const formattedUrl = row.personMapAddressUrl
                            .replace("{0}", "500")
                            .replace("{1}", "500");

                        return `
                        <div class="map-thumbnail profile-image doc-profile-image">
                            <img src="${formattedUrl}" 
                                 class="thumbnail profile-image doc-profile-image preview-map-image" 
                                 data-toggle="modal" 
                                 data-target="#mapModal" 
                                 data-img='${formattedUrl}' 
                                 data-title='${row.pincodeName}' />
                        </div>`;
                    }
                    else {
                        return '<img src="/img/no-map.jpeg" class="profile-image doc-profile-image" title="No address" data-toggle="tooltip" />';
                    }
                }
            },
            {
                "sDefaultContent": "<i class='fa-map-marker' data-toggle='tooltip' title='No address'></i>",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.document + '" class="full-map" title="' + row.policyId + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '<img src="' + row.document + '" class="profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
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
                        img += '<img src="' + row.customer + '" class="table-profile-image" />'; // Thumbnail image with class 'thumbnail'
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
                        img += '<img src="' + row.beneficiaryPhoto + '" class="table-profile-image" />'; // Thumbnail image with class 'thumbnail'
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
                    var buttons = "";
                    console.log(row.status);
                    if (row.ready2Assign) {
                        buttons += '<a id="assign' + row.id + '" href="/Investigation/EmpanelledVendors?Id=' + row.id + '" class="btn btn-xs btn-info refresh-btn" data-id="' + row.id + '">';
                        buttons += '<i class="fas fa-external-link-alt"></i> Assign</a>&nbsp;';
                    } else {
                        buttons += '<button disabled class="btn btn-xs btn-info"><i class="fas fa-external-link-alt"></i> Assign</button>&nbsp;';
                    }

                    buttons += '<a id="edit' + row.id + '" href="Details?Id=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pencil-alt"></i> Edit</a>&nbsp;';

                    buttons += '<button id="details' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash "></i> Delete </button>';

                    return buttons;
                }
            },
            { "data": "timeElapsed", bVisible: false },
            { "data": "policy", bVisible: false }
        ],
        rowCallback: function (row, data, index) {
            if (data.isNew) {
                $('td', row).addClass('isNewAssigned');
                // Remove the class after 3 seconds
                setTimeout(function () {
                    $('td', row).removeClass('isNewAssigned');
                }, 3000);
            }
        },
        "drawCallback": function (settings) {
            var api = this.api();
            var rowCount = (this.fnSettings().fnRecordsTotal()); // total number of rows
            if (rowCount > 0 && hasAssignedRows()) {
                $('#deletecase').prop('disabled', false);
                $('.top-info').prop('disabled', false);
                    $('#allocatedcase').prop('disabled', false);
            }
            else {
                $('.top-info').prop('disabled', true);
                $('#deletecase').prop('disabled', true);
                $('#allocatedcase').prop('disabled', true);
            }
            
            $('#customerTableAuto tbody').on('click', '.btn-info', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('assign', ''); // Extract the ID from the button's ID attribute
                assign(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
            $('#customerTableAuto tbody').on('click', '.btn-warning', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('edit', ''); // Extract the ID from the button's ID attribute
                showedit(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the edit page
            });
            //checkUploadJobStatus();
        }
    });

    $('#caseTypeFilter').on('change', function () {
        table.ajax.reload(); // Reload the table when the filter is changed
    });

    table.on('preDraw.dt', function () {
        $('input[name="select_all"]').prop('checked', false); // Uncheck checkboxes before rendering new data
    });

    table.on('length.dt', function () {
        $('input[name="select_all"]').prop('checked', false);
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
   
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });

    $('#customerTableAuto tbody').on('click', '.btn-danger', function (e) {
        e.preventDefault();

        var id = $(this).attr('id').replace('details', '');
        var url = '/InvestigationPost/Delete/' + id; // Replace with your actual API URL

        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete this case?',
            type: 'red',
            buttons: {
                confirm: {
                    text: 'Yes, delete it',
                    btnClass: 'btn-red',
                    action: function () {
                        $.ajax({
                            url: url,
                            type: 'POST',
                            data: {
                                icheckifyAntiforgery: $('input[name="icheckifyAntiforgery"]').val(),
                                id: id
                            },
                            success: function (response) {
                                // Show success message
                                $.alert({
                                    title: 'Deleted!',
                                    content: response.message,
                                    type: 'red'
                                });

                                // Reload the DataTable
                                $('#customerTableAuto').DataTable().ajax.reload(null, false); // false = don't reset paging
                            },
                            error: function (xhr, status, error) {
                                console.error("Delete failed:", xhr.responseText);
                                $.alert({
                                    title: 'Error!',
                                    content: 'Failed to delete the case.',
                                    type: 'red'
                                });
                            }
                        });
                    }
                },
                cancel: function () {
                    // No action on cancel
                }
            }
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
            content: "Are you sure you want to <span class='badge badge-light'>delete</span> selected case(s) ?",
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
            url: "/InvestigationPost/DeleteCases", // Update with your actual delete endpoint
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
        var MaxSizeInBytes = 5242880; //5 MB
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
                                content: " <i class='fa fa-upload'></i> Upload File size limit exceeded. <br />Max file size is 5 MB!",
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
    $(document).on('show.bs.modal', '#mapModal', function (event) {
        var trigger = $(event.relatedTarget); // The <img> clicked
        var imageUrl = trigger.data('img');
        var title = trigger.data('title');

        var modal = $(this);
        modal.find('#modalMapImage').attr('src', imageUrl);
        modal.find('.modal-title').text(title || 'Map Preview');
    });

    
});

function checkUploadJobStatus() {
    $.ajax({
        url: '/CreatorPost/GetJobStatus',
        type: 'GET',
        success: function (response) {
            console.log("Job Status:", response.status);

            if (response.status === "Processing" || response.status === "Enqueued") {
                setTimeout(function () {
                    checkUploadJobStatus();
                }, 2000); // Check every 5 seconds
            } else if (response.status === "Completed") {
                console.log("Job Completed:", response.status);
                $('#refreshTable').click(); // Refresh the table after completion
            }
            else {
                console.error("Errored job status:", response.status);
            }
        },
        error: function (xhr, status, error) {
            console.error("Error checking job status:", error);
        }
    });
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