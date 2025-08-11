$(document).ready(function () {


    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/Assessor/GetReview',
            dataSrc: '',
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Account/Login'; // Or session timeout handler
                }
            }
        },
        columnDefs: [
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 0                      // Index of the column to style
        },
        {
            className: 'max-width-column-number', // Apply the CSS class,
            targets: 1                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 8                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 10                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 11                      // Index of the column to style
            },
            {
                'targets': 17, // Index for the "Case Type" column
                'name': 'policy' // Name for the "Case Type" column
            }],
        order: [[16, 'asc']],
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
                "data": "agent",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.ownerDetail + '" class="full-map" title="' + row.agent + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '<img src="' + row.ownerDetail + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "pincode",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.personMapAddressUrl + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '<img src="' + row.personMapAddressUrl + '" class="full-map" title="' + row.pincodeName + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "distance",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.distance + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "duration",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.duration + '" data-toggle="tooltip">' + data + '</span>'
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
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail table-profile-image">';
                    img += '<img src="' + row.customer + '" class="full-map" title="' + row.customerFullName + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
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
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail table-profile-image">';
                    img += '<img src="' + row.beneficiaryPhoto + '" class="thumbnail table-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '<img src="' + row.beneficiaryPhoto + '" class="full-map" title="' + row.beneficiaryFullName + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '</div>';
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
                "data": "created",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.created + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    if (row.assignedToAgency) {
                        buttons += '<span class="checkbox">';
                        if (row.autoAllocated) {
                            buttons += '<i class="fas fa-cog fa-spin" title="AUTO ALLOCATION" data-toggle="tooltip"></i>';
                        } else {
                            buttons += '<i class="fas fa-user-tag" title="MANUAL ALLOCATION" data-toggle="tooltip"></i>';
                        }
                        buttons += '</span>';
                    } else {
                        buttons += '<span class="badge badge-light">...</span>';
                    }
                    
                    return buttons;
                }
            },
            { "data": "timePending" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id="details' + row.id + '" href="ReviewDetail?Id=' + row.id + '" class="active-claims btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;'

                    //if (row.withdrawable) {
                    //    buttons += '<a href="withdraw?Id=' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Withdraw</a>&nbsp;'
                    //}
                    return buttons;
                }
            },

            { "data": "timeElapsed", "bVisible": false },
            { "data": "policy", bVisible: false }
        ],
        "drawCallback": function (settings, start, end, max, total, pre) {

            $('#customerTable tbody').on('click', '.btn-info', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('details', ''); // Extract the ID from the button's ID attribute
                getdetails(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });

        }
    });
    $('#caseTypeFilter').on('change', function () {
        table.column('policy:name').search(this.value).draw(); // Column index 9 corresponds to "Case Type"
    });
    $('#refreshTable').click(function () {
        var $icon = $('#refreshIcon');
        if ($icon) {
            $icon.addClass('fa-spin');
        }
        table.ajax.reload(null, false); // false => Retains current page
    });
    table.on('xhr.dt', function () {
        $('#refreshIcon').removeClass('fa-spin');
    });
    table.on('mouseenter', '.map-thumbnail', function () {
            const $this = $(this); // Cache the current element

            // Set a timeout to show the full map after 1 second
            hoverTimeout = setTimeout(function () {
                $this.find('.full-map').show(); // Show full map
            }, 1000); // Delay of 1 second
        })
        .on('mouseleave', '.map-thumbnail', function () {
            const $this = $(this); // Cache the current element

            // Clear the timeout to cancel showing the map
            clearTimeout(hoverTimeout);

            // Immediately hide the full map
            $this.find('.full-map').hide();
        });
});

function getdetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    
    $('a#details' + id + '.btn.btn-xs.btn-info').html("<i class='fas fa-sync fa-spin'></i> Detail");
    disableAllInteractiveElements();

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
    
    $('a#edit' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");
    disableAllInteractiveElements();

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}

