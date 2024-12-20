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
            url: '/api/agency/Supervisor/GetOpen',
            dataSrc: ''
        },
        columnDefs: [
            
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 3                      // Index of the column to style
            }, {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 4                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 6                      // Index of the column to style
            }],
        order: [[13, 'asc']],
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
                "data": "agent",
                "mRender": function (data, type, row) {
                    if (row.caseWithPerson) {
                        var img = '<div class="map-thumbnail-customer table-profile-image">';
                        img += '<img src="' + row.ownerDetail + '" class="thumbnail table-profile-image" />'; // Thumbnail image with class 'thumbnail'
                        img += '<img src="' + row.ownerDetail + '" class="full-map-customer" title="' + row.agent + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                        img += '</div>';
                        return img;
                    }
                    else {
                        var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                        img += '<img src="' + row.ownerDetail + '" class="full-map" title="' + row.agent + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                        img += '<img src="' + row.ownerDetail + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                        img += '</div>';
                        return img;
                    }
                }
            },
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
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.document + '" class="full-map" title="' + row.policyId + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '<img src="' + row.document + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
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
                    var img = '<div class="map-thumbnail-customer table-profile-image">';
                    img += '<img src="' + row.customer + '" class="full-map-customer" title="' + row.name + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '<img src="' + row.customer + '" class="thumbnail table-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
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
                "data": "location",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.location + '" data-toggle="tooltip">' + data + '</span>'
                }
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
                    buttons += '<a id="details' + row.id + '" onclick="showdetails(`' + row.id + '`)" href="Detail?Id=' + row.id + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;'
                    return buttons;
                }
            },
            { "data": "timeElapsed", "bVisible": false }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    $('#customerTable').on('mouseenter', '.map-thumbnail-customer', function () {
        $(this).find('.full-map-customer').show(); // Show full map
    }).on('mouseleave', '.map-thumbnail-customer', function () {
        $(this).find('.full-map-customer').hide(); // Hide full map
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
    //initMap("/api/ClaimsVendor/GetOpenMap");
});

function showdetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn *').attr('disabled', 'disabled');
    $('a#details' + id + '.btn.btn-xs.btn-info').html("<i class='fas fa-sync fa-spin'></i> Detail");

    var nodes = document.getElementById("article").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}
