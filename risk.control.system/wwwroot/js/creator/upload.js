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
            { "data": "name" },
            { "data": "fileType" },
            { "data": "createdOn" },
            { "data": "uploadedBy" },
            
            {
                "data": "message",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.message + '" class=badge badge-light" data-toggle="tooltip"> '+ data +'</span>';
                }
            },
            {
                "data": "icon",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<i title="' + row.message + '" class="' + data + '" data-toggle="tooltip"></i>';
                }
            },
            {
                "data": null,
                "bSortable": false,
                "render": function (data, type, row) {
                    return '<a href="/Uploads/DownloadLog/' + row.id + '" class="btn btn-xs btn-primary"><i class="nav-icon fa fa-download"></i> Download</a> ' +
                        '<button class="btn btn-xs btn-danger delete-file" data-id="' + row.id + '"><i class="fas fa-trash"></i> Delete</button>';
                }
            }
        ],
        "order": [[4, "desc"]],  // ✅ Sort by 'createdOn' (5th column, index 4) in descending order
        "columnDefs": [
            {
                "targets": 4,   // ✅ Apply sorting to 'createdOn' column
                "type": "date"  // ✅ Ensure it is treated as a date
            }
        ],
        rowCallback: function (row, data) {
            var $row = $(row);

            if (data.status === "Processing" && data.id == uploadId) {
                // Disable the anchor tags for this row
                $(row).find('a').on('click', function (e) {
                    e.preventDefault();
                    $(this).addClass('disabled'); // Disable pointer events
                });
                $(row).find('button').on('click', function (e) {
                    $(this).prop('disabled', true); // Disables the button
                });
                $row.addClass('row-opacity-50 watermark-row'); // Make row semi-transparent with watermark

                startPolling(data.id);  // Pass the uploadId or row ID

            } else {
                $row.removeClass('row-opacity-50 watermark-row'); // Remove styling for other statuses

            }
        }
    });
    var pollingTimer;

    // Function to start polling the status
    function startPolling(uploadId) {
        pollingTimer = setInterval(function () {
            $.ajax({
                url: `/api/Creator/GetFileById/${uploadId}`, // Call the API to check status
                type: 'GET',
                success: function (updatedRowData) {
                    // If status is Processing, keep polling
                    if (updatedRowData.data.status === "Processing") {
                        console.log("Status is still Processing, continuing to poll...");
                    }
                    // If status is Completed, stop polling and update the row
                    else if (updatedRowData.data.status === "Completed" || updatedRowData.data.status === 'Error') {
                        console.log("Status is Completed, stopping polling and updating row.");
                        clearInterval(pollingTimer); // Stop polling
                        updateProcessingRow(uploadId, updatedRowData.data); // Update the row with completed data
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
                                //setRefreshCountFlag();
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