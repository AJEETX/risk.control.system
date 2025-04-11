$(document).ready(function () {
    // Initially disable the upload button
    $("#UploadFileButton").prop("disabled", true);

    // Enable button only when a file is selected
    $("#postedFile").on("change", function () {
        if ($(this).val()) {
            $("#UploadFileButton").prop("disabled", false);
        } else {
            $("#UploadFileButton").prop("disabled", true);
        }
    });
    var uploadId = $('#uploadId').val();
    var table = $('#customerTableAuto').DataTable({
        "ajax": {
            "url": '/api/Creator/GetFilesData',
            "type": "GET",
            "dataSrc": function (json) {
                if (!json.maxAssignReadyAllowed) {
                    $("#uploadAssignCheckbox, #postedFile, #UploadFileButton").prop("disabled", true);
                    $.confirm({
                        title: 'Information',
                        content: 'You have reached the maximum allowed assignments.',
                        type: 'red',
                        buttons: {
                            ok: {
                                text: 'OK',
                                btnClass: 'btn-danger',
                                action: function () {
                                    // Do nothing, just close the alert
                                }
                            }
                        }
                    });
                }
                else {
                    $("#uploadAssignCheckbox, #postedFile, #UploadFileButton").prop("disabled", false);
                }
                return json.data;
            }
        },
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        "columns": [
            { "data": "id", "bVisible": false },
            { "data": "sequenceNumber" },
            {
                "data": "icon",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<i title="' + row.message + '" class="' + data + '" data-toggle="tooltip"></i>';
                }
            },
            { "data": "name" },
            { "data": "fileType" },
            { "data": "createdOn" },
            { "data": "uploadedBy" },
            {
                "data": "message",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" class="badge badge-light" data-toggle="tooltip"> ' + data + '</span>';
                }
            },
            {
                "data": null,
                "bSortable": false,
                "render": function (data, type, row) {
                    var img = '<a href="/Uploads/DownloadLog/' + row.id + '" class="btn btn-xs btn-primary"><i class="nav-icon fa fa-download"></i> Download</a> ';
                    if (row.isManager || row.status != 'Completed') {
                        img += '<button class="btn btn-xs btn-danger delete-file" data-id="' + row.id + '"><i class="fas fa-trash"></i> Delete</button>';
                    } else {
                        img += '<button class="btn btn-xs btn-danger disabled" disabled><i class="fas fa-trash"></i> Delete</button>';
                    }
                    return img;
                }
            },
            { "data": "isManager", "bVisible": false }
        ],
        "order": [[5, "desc"]],  // ✅ Sort by 'createdOn' (index 5) in descending order
        "columnDefs": [
            {
                "targets": 4,   // ✅ Apply sorting to 'createdOn' column
                "type": "date"
            },
            {
                className: 'max-width-column-name', // ✅ Apply CSS class
                targets: 5
            },
            {
                className: 'max-width-column-name', // ✅ Apply CSS class
                targets: 6
            }
        ],
        rowCallback: function (row, data) {
            var $row = $(row);
            if (data.status === "Processing" && data.id == uploadId) {
                startPolling(data.id);
            }
        },  // ✅ Added missing comma before `initComplete`
        initComplete: function () {
            var api = this.api();

            // ✅ Correct index for `isManager` column is `9`
            var isManager = api.column(9).data().toArray().every(function (value) {
                return value === true;
            });

            if (!isManager) {
                api.column(6).visible(false); // ✅ Hide 'uploadedBy' if all are managers
            }

            var tableData = api.rows().data().toArray(); // ✅ Get all rows' data

            // ✅ Check if any row has status "Error" and matches uploadId
            var hasError = tableData.some(function (row) {
                return row.status === "Error" && row.id == uploadId;
            });
            var errorRow = tableData.find(function (row) {
                return row.status === "Error" && row.id == uploadId;
            });

            if (errorRow) {
                var title = errorRow.directAssign ? "Upload & Assign" : "Upload"; // ✅ Dynamically set title

                $.confirm({
                    title: title,
                    content: `${title} completed with error.`,
                    type: 'red',
                    buttons: {
                        ok: {
                            text: 'OK',
                            btnClass: 'btn-danger',
                            action: function () {
                                // Do nothing, just close the alert
                            }
                        }
                    }
                });
            }
        }
    });

    var pollingTimer;
    var alerted = false;

    // Function to start polling the status
    function startPolling(uploadId) {
        pollingTimer = setInterval(function () {
            $.ajax({
                url: `/api/Creator/GetFileById/${uploadId}`, // Call the API to check status
                type: 'GET',
                success: function (updatedRowData) {
                    var title = updatedRowData.directAssign ? "Upload & Assign" : "Upload";

                    if (!alerted && updatedRowData.data.status === 'Error') {
                        console.log("Status is Completed, stopping polling and updating row.");
                        clearInterval(pollingTimer); // Stop polling
                        updateProcessingRow(uploadId, updatedRowData.data); // Update the row with completed data

                        if (!updatedRowData.maxAssignReadyAllowed) {
                            $.confirm({
                                title: 'Max allowed Error',
                                content: 'Maximum allowed assignments reached.',
                                type: 'red',
                                buttons: {
                                    ok: {
                                        text: 'OK',
                                        btnClass: 'btn-danger',
                                        action: function () {
                                            // Do nothing, just close the alert
                                        }
                                    }
                                }
                            });
                        }
                        else {
                            $.confirm({
                                title: `${title} Error`,
                                content: ` ${title} completed with error`,
                                type: 'red',
                                buttons: {
                                    ok: {
                                        text: 'OK',
                                        btnClass: 'btn-danger',
                                        action: function () {
                                            // Do nothing, just close the alert
                                        }
                                    }
                                }
                            });
                        }
                        

                    }
                    // If status is Processing, keep polling
                    else if (!alerted && updatedRowData.data.status === "Processing") {
                        console.log("Status is still Processing, continuing to poll...");
                    }

                    // If status is Completed, stop polling and update the row
                    else if (!alerted && updatedRowData.data.status === "Completed") {
                        console.log("Status is Completed, stopping polling and updating row.");
                        clearInterval(pollingTimer); // Stop polling
                        updateProcessingRow(uploadId, updatedRowData.data); // Update the row with completed data
                        if (!updatedRowData.maxAssignReadyAllowed) {
                            $("#uploadAssignCheckbox, #postedFile, #UploadFileButton").prop("disabled", true);

                            $.confirm({
                                title: 'Max allowed reached',
                                content: 'Maximum allowed assignments reached.',
                                type: 'blue',
                                buttons: {
                                    ok: {
                                        text: 'OK',
                                        btnClass: 'btn-primary',
                                        action: function () {
                                            // Do nothing, just close the alert
                                        }
                                    }
                                }
                            });
                        }
                        else {
                            $.confirm({
                                title: 'Information',
                                content: `${title}  completed .`,
                                type: 'green',
                                buttons: {
                                    ok: {
                                        text: 'OK',
                                        btnClass: 'btn-success',
                                        action: function () {
                                            // Do nothing, just close the alert
                                        }
                                    }
                                }
                            });
                        }
                        
                    }
                },
                error: function (err) {
                    console.error('Error fetching file data:', err);
                    clearInterval(pollingTimer); // Stop polling on error
                }
            });
        }, 1000); // Poll every 5 seconds
    }

    // Function to update the row with new data
    function updateProcessingRow(uploadId, updatedRowData) {
        let row = table.row(function (idx, data, node) {
            return data.id === uploadId; // Find the row by ID
        });

        if (row) {
            row.data(updatedRowData).draw(false); // Update the row with the new data
        }
    }

    table.on('xhr.dt', function () {
        $('#refreshIcon').removeClass('fa-spin');
    });
    // Enable tooltips
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });

    // Delete file with jConfirm
    $('#customerTableAuto tbody').on('click', '.delete-file', function () {
        var fileId = $(this).data('id');

        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete this file?',
            type: 'red',
            buttons: {
                confirm: {
                    text: 'Yes, Delete it!',
                    btnClass: 'btn-red',
                    action: function () {
                        $.ajax({
                            url: '/Uploads/DeleteLog/' + fileId,
                            type: 'POST',
                            success: function (response) {
                                $.alert({
                                    title: 'File has been Deleted!',
                                    content: response.message,
                                    type: 'red',
                                    icon: 'fas fa-exclamation-triangle',
                                    closeIcon: true,
                                    buttons: {
                                        cancel: {
                                            text: "CLOSE",
                                            btnClass: 'btn-danger'
                                        }
                                    }
                                });
                                table.ajax.reload(null, false); // Reload table without changing the page
                            },
                            error: function (xhr) {
                                $.alert({
                                    title: 'Error',
                                    content: xhr.responseJSON?.message || "Error deleting file.",
                                    type: 'red'
                                });
                            }
                        });
                    }
                },
                cancel: {
                    text: 'Cancel',
                    action: function () {
                        // Do nothing on cancel
                    }
                }
            }
        });
    });

    // Refresh button
    $('#refreshTable').click(function () {
        var $icon = $('#refreshIcon');
        if ($icon) {
            $icon.addClass('fa-spin');
        }
        table.ajax.reload(null, false);
    });

    $('#uploadAssignCheckbox').on('change', function () {
        let isChecked = $(this).is(':checked');
        $('#UploadFileButton').toggleClass('btn-info btn-danger');
        // Toggle the button text (including HTML content)
        if (isChecked) {
            $('#UploadFileButton').html('<i class="fas fa-random"></i> Assign Directly');
        } else {
            $('#UploadFileButton').html('<i class="nav-icon fa fa-upload"></i> File Upload');
        }
    });
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

    function handleUploadConfirmation(formId, buttonId, checkboxId) {
        $(formId).on('submit', function (event) {
            if (askFileUploadConfirmation) {
                event.preventDefault();

                // Check the state of the checkbox
                let isChecked = $(checkboxId).is(':checked');



                // Customize the confirm dialog dynamically
                $.confirm({
                    title: isChecked ? "Confirm Direct Assign" : "Confirm File Upload",  // Dynamic title based on checkbox
                    content: isChecked ? "Are you sure you want to Upload -> Assign?" : "Are you sure you want to Upload?",  // Dynamic content
                    icon: isChecked ? 'fas fa-random' : 'fas fa-upload',  // Dynamic icon based on checkbox
                    type: isChecked ? '#dc3545' : '#17a2b8',  // Dynamic color type ('blue' for Upload & Assign, 'green' for just Upload)
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: isChecked ? "Assign Directly" : "File Upload",  // Dynamic button text
                            btnClass: isChecked ? 'btn-danger' : 'btn-success',  // Customize button class
                            action: function () {
                                askFileUploadConfirmation = false;

                                $("body").addClass("submit-progress-bg");
                                setTimeout(function () {
                                    $(".submit-progress").removeClass("hidden");
                                }, 1);

                                // Update the button text while uploading (already set above dynamically)
                                disableAllInteractiveElements();
                                // Customize the button text before the submission
                                if (isChecked) {
                                    $(buttonId).html('<i class="fas fa-sync fa-spin"></i> Assigning...');
                                } else {
                                    $(buttonId).html('<i class="fas fa-sync fa-spin"></i> Uploading...');
                                }
                                $(formId).submit();

                                // Disable elements during progress (optional, you can modify this part as needed)
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
                            btnClass: 'btn-default',  // Customize cancel button style
                        }
                    }
                });
            }
        });
    }


    // Apply confirmation to both forms
    handleUploadConfirmation("#upload-claims", "#UploadFileButton", "#uploadAssignCheckbox");
});