$(document).ready(function () {

    $('a.create-agency-user').attr("href", "/Vendors/Create");
    $('a#back-button').attr("href", "/Dashboard/Index");
    $('a#back').attr("href", "/Dashboard/Index");

    $("#customerTable").DataTable({
        ajax: {
            url: '/api/Agency/AllAgencies',
            dataSrc: ''
        },
        order: [[2, 'asc']],
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
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.document + '" class="doc-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "domain" },
            { "data": "name" },
            { "data": "code" },
            { "data": "phone" },
            { "data": "address" },
            { "data": "district" },
            { "data": "state" },
            { "data": "country" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a onclick="showdetails()" href="/Vendors/Details?Id=' + row.id + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Details</a>&nbsp;'
                    //buttons += '<a onclick="showedit()" href="/Vendors/Edit?Id=' + row.id + '"  class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    //buttons += '<a href="Vendors/Delete?Id=' + row.id + '"  class="btn btn-xs btn-danger"><i class="fas fa-trash"></i></i> Delete</a>'
                    return buttons;
                }
            }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip();
    });

    $('a.create-agency-user').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('a.create-agency-user').attr('disabled', 'disabled');
        $('a.create-agency-user').html("<i class='fas fa-spinner'></i> Add Agency");

        var nodes = document.getElementById("body").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
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
    $('a.btn.btn-warning').html("<i class='fas fa-spinner'></i> Edit Agency");

    var nodes = document.getElementById("body").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}

function showdetails() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn.btn-info').attr('disabled', 'disabled');
    $('a.btn.btn-info').html("<i class='fas fa-spinner'></i> Detail");

    var nodes = document.getElementById("body").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}