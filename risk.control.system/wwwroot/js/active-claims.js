$(document).ready(function () {
    $("#customerTable").DataTable({
        ajax: {
            url: '/api/CompanyActiveClaims/GetActive',
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
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.policyId + '" title="' + row.policyId + '" src="' + row.document + '" class="doc-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.customer + '" class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "name" },
            { "data": "policy" },
            { "data": "status" },
            { "data": "serviceType" },
            { "data": "location" },
            { "data": "created" },
            { "data": "timePending" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a onlick="showdetails()" show href="ClaimsInvestigation/Detail?Id=' + row.id + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;'
                    return buttons;
                }
            }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip();
    });
});

function showdetails() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn.btn-info').attr('disabled', 'disabled');
    $('a.btn.btn-info').html("<i class='fas fa-building'></i> Detail...");

    var nodes = document.getElementById("body").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}