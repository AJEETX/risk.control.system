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
    var table = $('#dataTable').DataTable({
        "ajax": {
            "url": `/api/Investigation/GetFilesData/${uploadId}`,
            "type": "GET",
            data: function (d) {
                return {
                    draw: d.draw,
                    start: d.start,
                    length: d.length,
                    orderColumn: d.order[0].column ?? 0,
                    orderDir: d.order[0].dir ?? "asc",
                    searchValue: d.search?.value ?? ""
                };
            },
            "dataSrc": function (json) {
                if (uploadId > 0 && !json.maxAssignReadyAllowed) {
                    $("#uploadAssignCheckbox, #postedFile, #UploadFileButton").prop("disabled", true);
                    $.confirm({
                        title: 'Information',
                        content: 'You have reached the maximum allowed assignments.',
                        icon: 'fas fa-random',  // Dynamic icon based on checkbox
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
                if (json.isManager === true) {
                    table.column(4).visible(true);   // Show UploadedBy
                } else {
                    table.column(4).visible(false);  // Hide UploadedBy
                }
                console.log(json.data[0]);

                return json.data;
            },
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Account/Login'; // Or session timeout handler
                }
                else if (xhr.status === 500) {
                    window.location.href = '/CaseUpload/Uploads'; // Or session timeout handler
                }
            }
        },
        responsive: true,
        fixedHeader: true,
        processing: true,
        autoWidth: false,
        serverSide: true,
        deferRender: true,
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
                "mRender": function (data, type, row) {
                    return '<i class="' + data + '" data-bs-toggle="tooltip"></i>';
                }
            },
            {
                "data": "name",
                "mRender": function (data, type, row) {
                    return '<i title="' + data + '" data-bs-toggle="tooltip">' + data + '</i>';
                }
            },
            {
                "data": "uploadedBy",
                "mRender": function (data, type, row) {
                    return '<i title="' + data + '" data-bs-toggle="tooltip">' + data + '</i>';
                }
            },
            {
                "data": "createdOn",
                "mRender": function (data, type, row) {
                    return '<i title="' + data + '" data-bs-toggle="tooltip"><small><strong>' + data + '</strong></small></i>';
                }
            },
            {
                "data": "timeTakenSeconds",
                "mRender": function (data, type, row) {
                    return '<i>' + row.timeTaken + '</i>';
                }
            },
            {
                "data": "uploadedType",
                "mRender": function (data, type, row) {
                    var title = row.directAssign ? "Assigned" : "Uploaded";
                    return `
                    <span class="custom-message-badge" title="${title}" data-bs-toggle="tooltip">
                        ${data}
                    </span>`;
                }
            },
            {
                data: "message",
                render: function (data, type, row) {
                    if (!data) return "";
                    if (row.completed) {
                        return `
                        <span class="custom-message-badge i-blue" title="${data}" data-toggle="tooltip">
                            <small><strong> ${data}</strong></small>
                        </span>`;
                    } else if (row.status == 'Error') {
                        return `
                        <span class="custom-message-badge i-red" title="${data}" data-toggle="tooltip">
                            <small><strong> ${data}</strong></small>
                        </span>`;
                    } else {
                        return `
                        <span class="custom-message-badge i-grey" title="${data}" data-toggle="tooltip">
                            <small><strong> ${data}</strong></small>
                        </span>`;
                    }
                }
            },
            {
                "data": null,
                "bSortable": false,
                "render": function (data, type, row) {
                    var img = '';
                    var title = row.directAssign ? "Assigned" : "Uploaded";
                    if (row.status == 'Error' && row.recordCount == 0 && row.message != "Error uploading the file") {
                        img += `<div class='btn-xs upload-exceed' title='Limit exceeded' data-bs-toggle='tooltip'> <i class='fas fa-times-circle i-orangered'></i> Limit exceed</div>`;
                    }
                    else if (row.hasError && row.message == "Error uploading the file") {
                        img += `
                            <button class="btn btn-xs btn-danger upload-err"
                                    data-id="${row.id}"
                                    data-url="/CaseUpload/DownloadErrorLog"
                                    data-bs-toggle="tooltip"
                                    title="Download Error file">
                                <i class="fa fa-download"></i> Error File
                            </button>`;
                    }

                    else if (!row.hasError && row.status == 'Completed') {
                        img += `<div class='btn btn-xs i-green upload-success' title='${title} Successfully' data-bs-toggle='tooltip'><i class='fa fa-check'></i> ${title} </div> `;
                    }
                    else if (row.status == 'Processing') {
                        img += `<div class='upload-progress' title='Action in-progress' data-bs-toggle='tooltip'><i class='fas fa-sync fa-spin i-grey'></i> </div>`;
                    }

                    img += `
                            <button class="btn btn-xs btn-primary upload-download"
                                    data-id="${row.id}"
                                    data-url="/CaseUpload/DownloadLog"
                                    data-bs-toggle="tooltip"
                                    title="Download upload file">
                                <i class="fa fa-download"></i> Download
                            </button>`;

                    img += '<button class="btn-xs btn-danger upload-delete" data-id="' + row.id + '" title="Delete" data-bs-toggle="tooltip"><i class="fas fa-trash"></i> Delete </button>';
                    return img;
                }
            }
        ],
        "order": [[1, "desc"]],  // ✅ Sort by 'createdOn' (index 5) in descending order
        "columnDefs": [
            {
                className: 'max-width-column-claim', // ✅ Apply CSS class
                targets: 1
            },
            {
                className: 'max-width-column-claim', // ✅ Apply CSS class
                targets: 2
            },
            {
                className: 'max-width-column-name', // ✅ Apply CSS class
                targets: 3
            },
            {
                className: 'max-width-column-name', // ✅ Apply CSS class
                "targets": 4
            },
            {
                className: 'max-width-column-email', // ✅ Apply CSS class
                targets: 5
            },
            {
                className: 'max-width-column-email', // Apply the CSS class,
                targets: 6                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 7                      // Index of the column to style
            },
            {
                className: 'max-width-column-status', // Apply the CSS class,
                targets: 8                      // Index of the column to style
            }
        ],
        rowCallback: function (row, data, index) {
            var $row = $(row);

            if (data.status === "Processing" && data.id == uploadId) {
                $row.addClass('processing-row');
                startPolling(data.id);
            } else {
                $row.removeClass('processing-row');
            }
        },  // ✅ Added missing comma before `initComplete`
        "drawCallback": function (settings, start, end, max, total, pre) {
            // Reinitialize Bootstrap 5 tooltips
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (el) {
                return new bootstrap.Tooltip(el, {
                    html: true,
                    sanitize: false   // ⬅⬅⬅ THIS IS THE FIX
                });
            });
        },
        initComplete: function () {
            var api = this.api();

            var tableData = api.rows().data().toArray(); // ✅ Get all rows' data

            // ✅ Check if any row has status "Error" and matches uploadId
            var hasError = tableData.some(function (row) {
                return row.status === "Error" && row.id == uploadId;
            });
            var errorRow = tableData.find(function (row) {
                return row.status === "Error" && row.id == uploadId;
            });

            if (errorRow) {
                var title = errorRow.directAssign ? "Direct Assign" : "Upload Only"; // ✅ Dynamically set title
                var icon = errorRow.directAssign ? 'fas fa-random' : 'fas fa-upload';
            }
        }
    });

    var pollingTimer;
    var alerted = false;

    // Function to start polling the status
    function startPolling(uploadId) {
        pollingTimer = setInterval(function () {
            $.ajax({
                url: `/api/Investigation/GetFileById/${uploadId}`, // Call the API to check status
                type: 'GET',
                success: function (updatedRowData) {
                    var icon = updatedRowData.data.result.directAssign ? 'fas fa-random' : 'fas fa-upload';  // Dynamic icon based on checkbox
                    var popType = updatedRowData.data.result.directAssign ? 'red' : 'blue';  // Dynamic color type ('blue' for Upload & Assign, 'green' for just Upload)
                    var title = updatedRowData.data.result.directAssign ? "Assign" : "Upload";
                    var btnClass = updatedRowData.data.result.directAssign ? 'btn-danger' : 'btn-info';
                    if (updatedRowData.data.result.status === 'Error' || updatedRowData.data.result.status === "Completed") {
                        console.log("Status is Completed, stopping polling and updating row.");
                        clearInterval(pollingTimer); // Stop polling
                        updateProcessingRow(uploadId, updatedRowData.data.result); // Update the row with completed data
                    }
                    // If status is Processing, keep polling
                    else if (updatedRowData.data.result.status === "Processing") {
                        console.log("Status is still Processing, continuing to poll...");
                    }

                    //// If status is Completed, stop polling and update the row
                    //else if (updatedRowData.data.result.status === "Completed") {
                    //    console.log("Status is Completed, stopping polling and updating row.");
                    //    clearInterval(pollingTimer); // Stop polling
                    //    updateProcessingRow(uploadId, updatedRowData.data.result); // Update the row with completed data
                    //}
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

    // Delete file with jConfirm
    $('#dataTable tbody').on('click', '.upload-delete', function () {
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
                            url: '/CaseUpload/DeleteLog/' + fileId,
                            type: 'POST',
                            headers: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                            },
                            data: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                id: fileId
                            },
                            success: function (response) {
                                $.alert({
                                    title: 'File has been Deleted!',
                                    content: response.message,
                                    type: 'red',
                                    icon: 'fas fa-trash',
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
                    btnClass: 'btn-default',
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
    table.on('xhr.dt', function () {
        $('#refreshIcon').removeClass('fa-spin');
    });

    $('#dataTable tbody').hide();
    $('#dataTable tbody').fadeIn(2000);

    $('#uploadAssignCheckbox').on('change', function () {
        let isChecked = $(this).is(':checked');
        $('#UploadFileButton').toggleClass('btn-info btn-danger');
        // Toggle the button text (including HTML content)
        if (isChecked) {
            $('#UploadFileButton').html('<i class="fas fa-random"></i> Assign');
        } else {
            $('#UploadFileButton').html('<i class="nav-icon fa fa-upload"></i> Upload');
        }
    });
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

    function handleUploadConfirmation(formId, buttonId, checkboxId) {
        $(formId).on('submit', function (event) {
            if (askFileUploadConfirmation) {
                event.preventDefault();
                // Check the state of the checkbox
                let isChecked = $(checkboxId).is(':checked');
                // Customize the confirm dialog dynamically
                $.confirm({
                    title: isChecked ? "Confirm Assign" : "Confirm Upload",  // Dynamic title based on checkbox
                    content: isChecked ? "Are you sure you want to Assign?" : "Are you sure you want to Upload?",  // Dynamic content
                    icon: isChecked ? 'fas fa-random' : 'fas fa-upload',  // Dynamic icon based on checkbox
                    type: isChecked ? 'red' : 'blue',  // Dynamic color type ('blue' for Upload & Assign, 'green' for just Upload)
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: isChecked ? " Assign " : " Upload ",  // Dynamic button text
                            btnClass: isChecked ? 'btn-danger' : 'btn-info',  // Customize button class
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
                                    $(buttonId).html('<i class="fas fa-sync fa-spin"></i> Assigning ...');
                                } else {
                                    $(buttonId).html('<i class="fas fa-sync fa-spin"></i> Uploading ...');
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
    $(document).on('click', '.upload-download', function (e) {
        e.preventDefault();

        const btn = $(this);
        const id = btn.data('id');
        const url = btn.data('url');
        const token = $('input[name="__RequestVerificationToken"]').val();

        btn.prop('disabled', true);

        $.ajax({
            url: url,
            type: 'POST',
            data: {
                id: id,
                __RequestVerificationToken: token
            },
            headers: {
                __RequestVerificationToken: token,
            },
            xhrFields: {
                responseType: 'blob'   // ⭐ critical
            },
            success: function (blob, status, xhr) {
                // Get filename from response headers
                let fileName =
                    xhr.getResponseHeader('X-File-Name') ||
                    'download.file';

                const url = window.URL.createObjectURL(blob);

                const a = document.createElement('a');
                a.href = url;
                a.download = fileName;
                document.body.appendChild(a);
                a.click();

                a.remove();
                window.URL.revokeObjectURL(url);
            },
            error: function (xhr) {
                let msg = xhr.responseText || 'Download failed';
                alert(msg);
            },
            complete: function () {
                btn.prop('disabled', false);
            }
        });
    });

    $(document).on('click', '.upload-err', function (e) {
        e.preventDefault();

        const btn = $(this);
        const id = btn.data('id');
        const url = btn.data('url');
        const token = $('input[name="__RequestVerificationToken"]').val();

        btn.prop('disabled', true);

        $.ajax({
            url: url,
            type: 'POST',
            data: {
                id: id,
                __RequestVerificationToken: token
            },
            headers: {
                __RequestVerificationToken: token,
            },
            xhrFields: {
                responseType: 'blob'   // ⭐ critical
            },
            success: function (blob, status, xhr) {
                // Get filename from response headers
                let fileName =
                    xhr.getResponseHeader('X-File-Name') ||
                    'download.file';

                const url = window.URL.createObjectURL(blob);

                const a = document.createElement('a');
                a.href = url;
                a.download = fileName;
                document.body.appendChild(a);
                a.click();

                a.remove();
                window.URL.revokeObjectURL(url);
            },
            error: function (xhr) {
                let msg = xhr.responseText || 'Download failed';
                alert(msg);
            },
            complete: function () {
                btn.prop('disabled', false);
            }
        });
    });
});

if (window.location.search.includes("uploadId")) {
    const url = new URL(window.location);
    url.searchParams.delete("uploadId");
    window.history.replaceState({}, document.title, url.pathname);
}