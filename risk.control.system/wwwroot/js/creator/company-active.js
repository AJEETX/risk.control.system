$(document).ready(function () {

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

    $("#customerTable").DataTable({
        ajax: {
            url: '/api/Creator/GetActive',
            dataSrc: ''
        },
        columnDefs: [
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 1                      // Index of the column to style
        },
        {
            className: 'max-width-column-number', // Apply the CSS class,
            targets: 2                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 4                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 6                      // Index of the column to style
        }],
        order: [[15, 'asc']],
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
                    var img = '<img alt="' + row.policyId + '" title="' + row.policyId + '" src="' + row.document + '" class="profile-image doc-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            {
                "data": "policyNum",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.policyId + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "amount",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.amount + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.customerFullName + '" title="' + row.customerFullName + '" src="' + row.customer + '" class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            {
                "data": "name",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.name + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.beneficiaryFullName + '" title="' + row.beneficiaryFullName + '" src="' + row.beneficiaryPhoto + '" class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            {
                "data": "beneficiaryName",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.beneficiaryName + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            { "data": "serviceType" },
            {
                "data": "pincode",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.pincodeName + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "location",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.status + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            { "data": "created" },
            { "data": "timePending" },
            {
                "data": "agent",
                "mRender": function (data, type, row) {
                    var img = '';
                    if (row.caseWithPerson) {
                        img = '<img alt="' + row.agent + '" title="' + row.agent + '" src="' + row.ownerDetail + '" class="table-profile-image" data-toggle="tooltip"/>';
                    }
                    else {
                        img = '<img alt="' + row.agent + '" title="' + row.agent + '" src="' + row.ownerDetail + '" class="profile-image doc-profile-image" data-toggle="tooltip"/>';
                    }
                    return img;
                }
                ///<button type="button" class="btn btn-lg btn-danger" data-toggle="popover" title="Popover title" data-content="And here's some amazing content. It's very engaging. Right?">Click to toggle popover</button>
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<span class="checkbox">';
                    if (row.autoAllocated) {
                        buttons += '<i class="fas fa-cog fa-spin" title="AUTO ALLOCATION" data-toggle="tooltip"></i>';
                    } else {
                        buttons += '<i class="fas fa-user-tag" title="MANUAL ALLOCATION" data-toggle="tooltip"></i>';
                    }
                    buttons += '</span>';
                    
                    return buttons;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id="details' + row.id + '" onclick="getdetails(`' + row.id + '`)"  href="ActiveDetail?Id=' + row.id + '" class="active-claims btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;'

                    if (row.autoAllocated) {

                    }
                    //if (row.withdrawable) {
                    //    buttons += '<a href="withdraw?Id=' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Withdraw</a>&nbsp;'
                    //}
                    return buttons;
                }
            },
            { "data":"timeElapsed", bVisible: false}
        ],
        "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {
            if (aData.isNewAssigned) {
                $('td', nRow).css('background-color', '#ffa');
            }
        },
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'bottom',
            html: true
        });
    });
    //$('[data-toggle="tooltip"]').tooltip({
    //    animated: 'fade',
    //    placement: 'bottom',
    //    html: true
    //});
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
    $('a#details' + id + '.btn.btn-xs.btn-info').html("<i class='fas fa-sync fa-spin'></i> Detail");

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
    $('a.btn *').attr('disabled', 'disabled');
    $('a#edit' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}

