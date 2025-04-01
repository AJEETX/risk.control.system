$(document).ready(function () {
    var table = $('#file-table').DataTable({
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

    // Enable tooltips
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });

    // Delete file with jConfirm
    $('#file-table tbody').on('click', '.delete-file', function () {
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
        table.ajax.reload(null, false);
    });
});
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
