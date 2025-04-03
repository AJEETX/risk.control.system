$(document).ready(function () {
    $('a.create-policy').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('a.create-policy').html("<i class='fas fa-sync fa-spin'></i> Add");

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
            { "data": "id" },
            { "data": "name" },
            { "data": "description" },
            { "data": "fileType" },
            { "data": "createdOn" },
            { "data": "uploadedBy" },
            {
                "data": "status",
                "mRender": function (data, type, row) {
                    return '<i title="' + row.message + '" class="' + data + '" data-toggle="tooltip"></i>';
                }
            },
            {
                "data": null,
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
        ]
    });
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
                                    type: 'red'
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
        $('#checkall').prop('checked', false);
    });

    var refreshInterval = 5000; // 3 seconds interval
    var maxAttempts = 5; // Prevent infinite loop
    var attempts = 0;
    var initialCount = sessionStorage.getItem("InitialUploadCount");

    // Check if a refresh is needed after upload
    var refreshDatatble = sessionStorage.getItem("RefreshDataTableCount");
    if (sessionStorage.getItem("RefreshDataTableCount") == "RefreshDataTableCount") {
        if (initialCount === null) {
            initialCount = table.data().count(); // Save the current record count
            sessionStorage.setItem("InitialUploadCount", initialCount);
        } else {
            initialCount = parseInt(initialCount, 10); // Convert to number
        }
        pollForNewData();
        sessionStorage.removeItem("RefreshDataTableCount"); // Clear the refresh flag
    }

    // Function to check if there are any "Pending" rows
    function hasPendingRows() {
        var table = $("#customerTableAuto").DataTable();
        var pendingExists = false;

        table.rows().every(function () {
            var data = this.data();
            if (data.status === "Processing") {
                pendingExists = true;
                return false; // Stop iterating once a "Pending" row is found
            }
        });

        return pendingExists;
    }
    function pollForNewData() {
        attempts++;

        if (attempts > maxAttempts) {
            console.log("Max attempts reached, stopping refresh.");
            sessionStorage.removeItem("InitialUploadCount"); // Clean up
            return;
        }

        console.log("Refreshing DataTable... Attempt: " + attempts);

        table.ajax.reload(function () {
            var newCount = table.data().count(); // Get updated row count

            if (newCount == initialCount && !hasPendingRows()) {
                console.log("No New records detected! Stopping refresh.");
                sessionStorage.removeItem("InitialUploadCount"); // Clean up
            } else {
                setTimeout(pollForNewData, refreshInterval);
                initialCount= newCount;
            }
        }, false);
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
                                setRefreshCountFlag();
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

function setRefreshCountFlag() {
    var table = $('#customerTableAuto').DataTable();
    sessionStorage.setItem("InitialUploadCount", table.data().count()); // Save count before reload
    sessionStorage.setItem("RefreshDataTableCount", 'RefreshDataTableCount');
    console.log('stored data')
}
// Function to format time elapsed
function formatTimeElapsed(seconds) {
    if (seconds < 60) return `${seconds} sec ago`;
    let minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes} min ago`;
    let hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours} hr ago`;
    let days = Math.floor(hours / 24);
    return `${days} day${days > 1 ? 's' : ''} ago`;
}
