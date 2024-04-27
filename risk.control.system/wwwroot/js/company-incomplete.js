$(document).ready(function () {
    $('a.create-policy').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('a.create-policy').attr('disabled', 'disabled');
        $('a.create-policy').html("<i class='fas fa-sync fa-spin'></i> Add New");

        var nodes = document.getElementById("article").getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    });
    $('#view-type a').on('click', function () {
        var id = this.id;
        if (this.id == 'map-type') {
            $('#checkboxes').css('display', 'none');
            $('#maps').css('display', 'block');
            $('#map-type').css('display', 'none');
            $('#list-type').css('display', 'block');
        }
        else {
            $('#checkboxes').css('display', 'block');
            $('#maps').css('display', 'none');
            $('#map-type').css('display', 'block');
            $('#list-type').css('display', 'none');
        }
    });

    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/CompanyActiveClaims/GetIncomplete',
            dataSrc: ''
        },
        order: [[10, 'asc']],
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
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.policyId + '" title="' + row.policyId + '" src="' + row.document + '" class="doc-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "policyNum", "bSortable": false },
            {
                "data": "amount"
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.customerFullName + '" title="' + row.customerFullName + '" src="' + row.customer + '" class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "name" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.beneficiaryFullName + '" title="' + row.beneficiaryFullName + '" src="' + row.beneficiaryPhoto + '" class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "beneficiaryName" },
            { "data": "serviceType" },
            { "data": "service" },
            {
                "data": "pincode",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.pincodeName + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            { "data": "location" },
            { "data": "created" },
            { "data": "timePending" },
            { "data": "agent" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    if (row.assignedToAgency) {
                        buttons += '<a id="details' + row.id + '" onclick="getdetails(`' + row.id + '`)"  href="ActiveDetail?Id=' + row.id + '" class="active-claims btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;'
                    }
                    else {
                        buttons += '<a id="edit' + row.id + '" onclick="showedit(`' + row.id + '`)"  href="Details?Id=' + row.id + '" class="active-claims btn btn-xs btn-warning"><i class="fas fa-pencil-alt"></i> Edit</a>&nbsp;'
                    }
                    buttons += '<a id="details' + row.id + '" onclick="getdetails(`' + row.id + '`)" href="/InsurancePolicy/Delete?Id=' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete </a>'

                    if (row.autoAllocated) {

                    }
                    //if (row.withdrawable) {
                    //    buttons += '<a href="withdraw?Id=' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Withdraw</a>&nbsp;'
                    //}
                    return buttons;
                }
            }
        ],
        "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {
            if (aData.isNewAssigned) {
                $('td', nRow).css('background-color', '#ffa');
            }
        },
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip();
    });

    $('#customerTable tbody').hide();
    $('#customerTable tbody').fadeIn(2000);

    //initMap("/api/CompanyActiveClaims/GetActiveMap");
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

    var nodes = document.getElementById("article").getElementsByTagName('*');
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
    $('a.btn *').attr('disabled', 'disabled');
    $('a#edit' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    var nodes = document.getElementById("article").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}

