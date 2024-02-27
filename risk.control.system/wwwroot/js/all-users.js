$(document).ready(function () {
    $("#customerTable").DataTable({
        ajax: {
            url: '/api/User/AllUsers',
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
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.photo + '" class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "email" },
            { "data": "name" },
            { "data": "phone" },
            { "data": "addressline" },
            { "data": "district" },
            { "data": "state" },
            { "data": "country" },
            { "data": "pincode" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<span class="checkbox">'
                    if (row.active) {
                        buttons += '<input type="checkbox" checked disabled />'
                    } else {
                        buttons += '<input type="checkbox" disabled/>'
                    }
                    buttons += '</span>'
                    return buttons;
                }
            },
            { "data": "roles" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a onclick="showedit()" href="/User/Edit?userId=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-user-minus"></i> Edit</a>&nbsp;'
                    buttons += '<a onclick="showroles()" href="/UserRoles/Index?userId=' + row.id + '"  class="btn btn-xs btn-info"><i class="fas fa-user-plus"></i> Roles</a>'
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

function showroles() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn.btn-info').attr('disabled', 'disabled');
    $('a.btn.btn-info').html("<i class='fas fa-sync fa-spin'></i> Roles");

    var nodes = document.getElementById("body").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}
function showedit() {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn.btn-warning').attr('disabled', 'disabled');
    $('a.btn.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    var nodes = document.getElementById("body").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}
