$(document).ready(function () {
    $("#customerTable").DataTable({
        ajax: {
            url: '/api/Agency/AllServices',
            dataSrc: ''
        },
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
                "data": "id", "name": "Id", "bVisible": false
            },
            { "data": "caseType" },
            { "data": "serviceType" },
            { "data": "rate" },
            { "data": "district" },
            { "data": "state" },
            { "data": "country" },
            { "data": "pincodes" },
            { "data": "updatedBy" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a href="/Agency/ServiceDetail?id=' + row.id + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Details</a>&nbsp;'
                    buttons += '<a href="/Agency/EditService?id=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    buttons += '<a href="/Agency/DeleteService?id=' + row.id + '"  class="btn btn-xs btn-danger"><i class="fas fa-trash"></i> Delete</a>'
                    return buttons;
                }
            }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
});