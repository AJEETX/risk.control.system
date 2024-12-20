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
            url: '/api/Manager/GetReport',
            dataSrc: ''
        },
        columnDefs: [
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 2                      // Index of the column to style
            },
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 3                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 5                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 7                      // Index of the column to style
            }],
        order: [[11, 'asc']],
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
                "data": "pincode",
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.personMapAddressUrl + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '<img src="' + row.personMapAddressUrl + '" class="full-map" title="' + row.pincodeName + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '</div>';
                    return img;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.policyId + '" title="' + row.policyId + '" src="' + row.document + '"class="profile-image doc-profile-image" data-toggle="tooltip"/>';
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
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.customer + '" class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            }, {
                "data": "name",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.name + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.beneficiaryName + '" title="' + row.beneficiaryName + '" src="' + row.beneficiaryPhoto + '" class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            {
                "data": "beneficiaryName",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.beneficiaryName + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "serviceType",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.serviceType + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "service",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.service + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "agency",
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.agency + '" title="' + row.agency + '" src="' + row.ownerDetail + '" class="profile-image doc-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
                ///<button type="button" class="btn btn-lg btn-danger" data-toggle="popover" title="Popover title" data-content="And here's some amazing content. It's very engaging. Right?">Click to toggle popover</button>
            },
            {
                "data": "created",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.created + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            { "data": "timePending" },
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
                    buttons += '<a id="details' + row.id + '" onclick="getdetails(`' + row.id + '`)" href="ApprovedDetail?Id=' + row.id + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;'
                    buttons += '<a href="/Report/PrintPdfReport?Id=' + row.id + '" class="btn btn-xs btn-danger"><i class="far fa-file-pdf"></i> PDF</a>'
                    return buttons;
                }
            }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    $('#customerTable').on('mouseenter', '.map-thumbnail', function () {
        $(this).find('.full-map-title').show(); // Show full map
        $(this).find('.full-map').show(); // Show full map
    }).on('mouseleave', '.map-thumbnail', function () {
        $(this).find('.full-map-title').hide(); // Hide full map
        $(this).find('.full-map').hide(); // Hide full map
    });
    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });
    //initMap("/api/CompanyCompletedClaims/GetReportMap");
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

