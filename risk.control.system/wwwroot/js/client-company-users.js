$(document).ready(function () {
    var id = $('#companyId').val();
    $("#customerTable").DataTable({
        ajax: {
            url: '/api/Company/CompanyUsers?id=' + id,
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
                    var img = '<img src="' + row.photo + '" src height="30" width="30" />';
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
                    buttons += '<a href="/CompanyUser/Edit?userId=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;'
                    buttons += '<a href="/CompanyUserRoles/Index?userId=' + row.id + '"  class="btn btn-xs btn-info"><i class="fas fa-pen"></i> Roles</a>'
                    return buttons;
                }
            }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
});