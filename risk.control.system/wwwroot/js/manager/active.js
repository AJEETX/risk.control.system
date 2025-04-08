
$(document).ready(function () {


    var table  = $("#customerTable").DataTable({
        ajax: {
            url: '/api/Manager/GetActive',
            type: 'GET',
            dataType: 'json',
            data: function (d) {
                console.log("Data before sending:", d); // Debugging

                return {
                    draw: d.draw || 1,
                    start: d.start || 0,
                    length: d.length || 10,
                    caseType: $('#caseTypeFilter').val() || "",  // Send selected filter value
                    search: d.search?.value || "", // Instead of empty string, send "all"
                    orderColumn: d.order?.[13]?.column ?? 0,
                    orderDir: d.order?.[0]?.dir || "asc"
                };
            },
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
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
                targets: 6                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 8                      // Index of the column to style
            },
            {
                'targets': 15, // Index for the "Case Type" column
                'name': 'policy' // Name for the "Case Type" column
            }],
        order: [[13, 'asc']],
        responsive: true,
        fixedHeader: true,
        processing: true,
        serverSide: true,
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
                    if (row.caseWithPerson) {
                        var img = '<div class="map-thumbnail table-profile-image">';
                        img += '<img src="' + row.ownerDetail + '" class="thumbnail table-profile-image" />'; // Thumbnail image with class 'thumbnail'
                        img += '<img src="' + row.ownerDetail + '" class="full-map" title="' + row.agent + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
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
                "data": "location",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.status + '" data-toggle="tooltip">' + data + '</span>'
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
            { "data": "timePending" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id="details' + row.id + '" href="ActiveDetail?Id=' + row.id + '" class="active-claims btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;';
                    return buttons;
                }
            },
            { "data": "policy", bVisible: false }
        ],
        "drawCallback": function (settings, start, end, max, total, pre) {

            $('#customerTable tbody').on('click', '.btn-info', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('details', ''); // Extract the ID from the button's ID attribute
                getdetails(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });

        },
        "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {
            if (aData.isNewAssigned) {
                $('td', nRow).addClass('isNewAssigned');
            }
        },
        error: function (xhr, status, error) { alert('err ' + error) }
    });

    $('#caseTypeFilter').on('change', function () {
        table.ajax.reload(); // Reload the table when the filter is changed
    });
    table.on('xhr.dt', function () {
        $('#refreshIcon').removeClass('fa-spin');
    });

    $('#refreshTable').click(function () {
        var $icon = $('#refreshIcon');
        if ($icon) {
            $icon.addClass('fa-spin');
        }
        table.ajax.reload(null, false); // false => Retains current page
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
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });
    $('#customerTable tbody').hide();
    $('#customerTable tbody').fadeIn(2000);
    
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

