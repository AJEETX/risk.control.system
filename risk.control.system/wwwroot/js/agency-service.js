$(document).ready(function () {
    $('a#back-button').attr("href", "/Dashboard/Index");
    $('a#back').attr("href", "/Dashboard/Index");
    $('a.create-agency-service').attr("href", "/Agency/CreateService");


    $('a.create-agency-service').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('a.create-agency-service').attr('disabled', 'disabled');
        $('a.create-agency-service').html("<i class='fas fa-truck' aria-hidden='true'></i> Add New...");

        var nodes = document.getElementById("body").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });



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
                    //buttons += '<a href="/Agency/ServiceDetail?id=' + row.id + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Details</a>&nbsp;'
                    buttons += '<a onclick="showedit()" href="/Agency/EditService?id=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    buttons += '<a onclick="getdetails()" href="/Agency/DeleteService?id=' + row.id + '"  class="btn btn-xs btn-danger"><i class="fas fa-trash"></i> Delete</a>'
                    return buttons;
                }
            }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
});

function showedit() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn.btn-warning').attr('disabled', 'disabled');
    $('a.btn.btn-warning').html("<i class='fas fa-building'></i> Edit Agency...");

    var nodes = document.getElementById("body").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}
function getdetails() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn.btn-danger').attr('disabled', 'disabled');
    $('a.btn.btn-danger').html("<i class='fa fa-trash'></i> Delete...");

    var nodes = document.getElementById("body").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}