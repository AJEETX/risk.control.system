$(document).ready(function () {
    $('a#back').attr("href", "/Dashboard/Index");
    $('a.create').attr("href", "/IpAddress/Create");
    $("#customerTable").DataTable({
        ajax: {
            url: '/api/MasterData/GetIpAddress',
            dataSrc: ''
        },
        order: [[12, 'asc']],
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
            { "data": "ipApiResponseId",visible:false },
            { "data": "country" },
            { "data": "regionName" },
            { "data": "city" },
            { "data": "district" },
            { "data": "zip" },
            { "data": "lat" },
            { "data": "lon" },
            { "data": "isp" },
            { "data": "query" },
            { "data": "user" },
            { "data": "isAuthenticated" },
            { "data": "created" },
     
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip();
    });
});

