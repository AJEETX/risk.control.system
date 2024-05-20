$(document).ready(function () {
    $("#customerTable").DataTable({
        ajax: {
            url: '/api/Company/AllCompanies',
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
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.document + '" class="doc-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "domain" },
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
                    buttons += '<span class="checkbox">'
                    if (row.active == 'ACTIVE') {
                        buttons += '<i class="fa fa-toggle-on"></i>';
                    } else {
                        buttons += '<i class="fa fa-toggle-off"></i>';
                    }
                    buttons += '</span>'
                    return buttons;
                }
            },
            { "data": "updated" },
            { "data": "updatedBy" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id=detail' + row.id + ' onclick="showdetails(' + row.id + ')" href="/ClientCompany/Details?Id=' + row.id + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;'
                    //buttons += '<a id=edit' + row.id + ' onclick="showedit(' + row.id + ')"  href="/ClientCompany/Edit?Id=' + row.id + '"  class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    buttons += '<a id=delete' + row.id + ' onclick="getdetails(' + row.id + ')" href="/ClientCompany/Delete?Id=' + row.id + '"  class="btn btn-xs btn-danger"><i class="fas fa-trash"></i></i> Delete</a>'
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

function getdetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn.btn-xs.btn-danger').attr('disabled', 'disabled');
    $('a#delete' + id + '.btn.btn-xs.btn-danger').html("<i class='fas fa-sync fa-spin'></i> Delete");

    var nodes = document.getElementById("customerTable").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}
function showdetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn.btn-xs.btn-info').attr('disabled', 'disabled');
    $('a#detail' + id + '.btn.btn-xs.btn-info').html("<i class='fas fa-sync fa-spin'></i> Detail");

    var nodes = document.getElementById("customerTable").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}
function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn.btn-warning').attr('disabled', 'disabled');
    $('a#edit ' +id + '.btn.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    var nodes = document.getElementById("customerTable").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}