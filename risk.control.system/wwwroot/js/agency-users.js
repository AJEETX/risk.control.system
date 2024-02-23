$(document).ready(function () {
    $('a#back-button').attr("href", "/Dashboard/Index");
    $('a#back').attr("href", "/Dashboard/Index");
    $('a.create-agency-user').attr("href", "/Agency/CreateUser");


    $('a.create-agency-user').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('a.create-agency-user').attr('disabled', 'disabled');
        $('a.create-agency-user').html("<i class='fas fa-spinner' aria-hidden='true'></i> Add User");

        var nodes = document.getElementById("body").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });



    $("#customerTable").DataTable({
        ajax: {
            url: '/api/Agency/AllUsers',
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
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.photo + '" class="table-profile-image" />';
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
                    buttons += '<a id=edit' + row.id + ' onclick="showedit(' + row.id + ')"  href="/Agency/EditUser?userId=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    buttons += '<a id=role' + row.id + ' onclick="showroles(' + row.id + ')" href="/Agency/UserRoles?userId=' + row.id + '"  class="btn btn-xs btn-info"><i class="fas fa-pen"></i> Role</a>'
                    return buttons;
                }
            }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
});

function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn').attr('disabled', 'disabled');
    $('a#edit' + id +'.btn.btn-xs.btn-warning').html("<i class='fas fa-spinner'></i> Edit");

    var nodes = document.getElementById("body").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}

function showroles(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn').attr('disabled', 'disabled');
    $('a#role' + id +'.btn.btn-info').html("<i class='fas fa-spinner'></i> Role");

    var nodes = document.getElementById("body").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}