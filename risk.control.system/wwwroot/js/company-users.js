﻿$(document).ready(function () {

    $("#customerTable").DataTable({
        ajax: {
            url: '/api/Company/AllUsers',
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
            { "data": "phone" },
            { "data": "addressline", bSortable: false },
            { "data": "pincode" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<span class="checkbox">';
                    if (row.active) {
                        buttons += '<i class="fa fa-toggle-on"></i>';
                    } else {
                        buttons += '<i class="fa fa-toggle-off"></i>';
                    }
                    buttons += '</span>';
                    return buttons;
                }
            },
            { "data": "roles" },
            { "data": "updated" },
            { "data": "updateBy" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id=edit' + row.id + ' onclick="showedit(' + row.id + ')" href="/Company/EditUser?userId=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    //buttons += '<a id=role' + row.id + ' onclick="showroles(' + row.id + ')" href="/Company/UserRoles?userId=' + row.id + '"  class="btn btn-xs btn-info"><i class="fas fa-pen"></i> Role</a>'
                    return buttons;
                }
            }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });

    $('a.create-user').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $(this).attr('disabled', 'disabled');
        $(this).html("<i class='fas fa-sync fa-spin'></i> Add User");

        var nodes = document.getElementById("article").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });
});

function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    var editbtn = $('a#edit' + id +'.btn.btn-xs.btn-warning')
    $('.btn.btn-xs.btn-warning').attr('disabled', 'disabled');
    editbtn.html("<i class='fas fa-sync fa-spin'></i> Edit");

    var nodes = document.getElementById("article").getElementsByTagName('*');
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
    var rolebtn = $('a#role' + id +'.btn.btn-xs.btn-info')
    $('a.btn.btn-xs.btn-info').attr('disabled', 'disabled');
    rolebtn.html("<i class='fas fa-sync fa-spin'></i> Role");

    var nodes = document.getElementById("article").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}