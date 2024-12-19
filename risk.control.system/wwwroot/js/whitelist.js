$(document).ready(function () {
    $('a#back').attr("href", "/Dashboard/Index");
    $('a.create').attr("href", "/IpAddress/Create");
    $("#customerTable").DataTable({
        ajax: {
            url: '/api/MasterData/GetIpAddress',
            dataSrc: ''
        },
        columnDefs: [
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 7                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 9                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 10                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 11                      // Index of the column to style
            }],
        order: [[11, 'desc']],
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
                "data": "mapUrl",
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.mapUrl + '" class="thumbnail" />'; // Thumbnail image with class 'thumbnail'
                    img += '<img src="' + row.mapUrl + '" class="full-map" />'; // Full map image with class 'full-map'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "country",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.country + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "regionName",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.regionName + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "city",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.city + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "zip",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.zip + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "lat",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.lat + '" data-toggle="tooltip">' + data + '</span>'
                }

            },
            {
                "data": "lon",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.lon + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "isp",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.isp + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "query",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.query + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "user",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.user + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "page",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.page + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "dated",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.dated + '" data-toggle="tooltip">' + data + '</span>'
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

    // Show the full map on hover and hide it when the mouse leaves
    $('#customerTable').on('mouseenter', '.map-thumbnail', function () {
        $(this).find('.full-map').show(); // Show full map
    }).on('mouseleave', '.map-thumbnail', function () {
        $(this).find('.full-map').hide(); // Hide full map
    });
});

