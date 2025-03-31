$(document).ready(function () {
    // Initialize the DataTable
    var table = $('#file-table').DataTable({
        "ajax": {
            "url": '/api/Creator/GetFilesData', // The controller action to fetch data
            "type": "GET", // or "POST" based on your needs
            "dataSrc": function (json) {
                // You can manipulate the data if needed before rendering
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
                    return '<a href="/Uploads/DownloadLog/' + row.id + '" class="btn btn-xs btn-primary"><i class="nav-icon fa fa-download"></i> Download</a>' +
                        '<a href="/Uploads/DeleteLog/' + row.id + '" class="btn btn-xs btn-danger"><i class="fas fa-trash"></i> Delete</a>';
                }
            }
        ]
    });
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });
    

    $('#refreshTable').click(function () {
        table.ajax.reload(null, false); // false => Retains current page
    });

    // Refresh every 30 seconds (adjust as needed)
    //setInterval(function () {
    //    table.ajax.reload(null, false); // false retains the current page
    //    $('#checkall').prop('checked', false);
    //}, 5000); // 30000ms = 30 seconds
});