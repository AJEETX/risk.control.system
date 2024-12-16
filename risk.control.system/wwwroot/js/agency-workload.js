$(document).ready(function () {
    $("#customerTable").DataTable({
        ajax: {
            url: '/api/Agency/GetAgentLoad',
            dataSrc: ''
        },
        columnDefs: [
            {
                className: 'max-width-column-email', // Apply the CSS class,
                targets: 2                      // Index of the column to style
            },
        {
            className: 'max-width-column', // Apply the CSS class,
            targets: 4                      // Index of the column to style
            },
            {
                className: 'max-width-column-claim', // Apply the CSS class,
                targets: 7                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 8                      // Index of the column to style
            }],
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
                "sDefaultContent": '<i class="fa fa-toggle-on"></i>',
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img;
                    if (row.agentOnboarded && row.active) {
                        img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.photo + '" class="table-profile-image" data-toggle="tooltip"/>';
                    }
                    else if (!row.agentOnboarded) {
                        img = '<img alt="' + row.name + '" title="Onboarding incomplete !!! ' + row.name + '" src="' + row.photo + '" class="table-profile-image-agent-onboard" data-toggle="tooltip"/>';
                    }
                    else {
                        img = '<img alt="' + row.name + '" title="Inactive !!! ' + row.name + '" src="' + row.photo + '" class="table-profile-image-agent-onboard" data-toggle="tooltip"/>';
                    }
                    return img;
                }
            },
            {
                "data": "email",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.rawEmail + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            { "data": "phone" },
            {
                "data": "addressline",
                bSortable: false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.addressline + '" data-toggle="tooltip">' + row.addressline + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<span class="checkbox">'
                    if (row.active) {
                        buttons += '<i class="fa fa-toggle-on"></i>';
                    } else {
                        buttons += '<i class="fa fa-toggle-off"></i>';
                    }
                    buttons += '</span>'
                    return buttons;
                }
            },
            { "data": "roles" },
            { "data": "count" },
            { "data": "updateBy" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id=edit' + row.id + ' onclick="showedit(' + row.id + ')"  href="/Agency/EditUser?userId=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;';
                    //buttons += '<a href="/Agency/UserRoles?userId=' + row.id + '"  class="btn btn-xs btn-info"><i class="fas fa-pen"></i> Roles</a>'

                    if (row.role != "AGENCY_ADMIN") {
                        buttons += '<a id="details' + row.id + '" onclick="getdetails(`' + row.id + '`)" href="/Agency/DeleteUser?userId=' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete </a>'
                    } else {
                        buttons += '<button disabled class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete </a>'
                    }
                    return buttons;
                }
            }
        ],
        "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {
            if (!aData.agentOnboarded) {
                $('td', nRow).css('background-color', '#FCFCEF');
                $('td', nRow).css('color', 'lightgrey');
            }
            if (!aData.active) {
                $('td', nRow).css('background-color', '#FCFCEF');
                $('td', nRow).css('color', 'lightgrey');
            }
        },
        error: function (xhr, status, error) { alert('err ' + error) }
    });

    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });
});
function getdetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn *').attr('disabled', 'disabled');
    $('a#details' + id + '.btn.btn-xs.btn-danger').html("<i class='fas fa-sync fa-spin'></i> Delete");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}
function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn').attr('disabled', 'disabled');
    $('a#edit' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}
