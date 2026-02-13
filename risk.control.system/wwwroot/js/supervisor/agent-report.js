$(document).ready(function () {
    $('#allocatedcase').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#allocatedcase').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Submit <sub>report</sub>");

        $('#allocatedcase').css('pointer-events', 'none')

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    var table = $("#dataTable").DataTable({
        ajax: {
            url: '/api/agency/VendorInvestigation/GetReport',
            type: 'GET',
            dataType: 'json',
            dataSrc: function (json) {
                return json.data; // Return table data
            },
            data: function (d) {
                console.log("Data before sending:", d); // Debugging

                return {
                    draw: d.draw || 1,
                    start: d.start || 0,
                    length: d.length || 10,
                    caseType: $('#caseTypeFilter').val() || "",  // Send selected filter value
                    search: d.search?.value || "", // Instead of empty string, send "all"
                    orderColumn: d.order?.[0]?.column ?? 15,
                    orderDir: d.order?.[0]?.dir || "asc"
                };
            },
            error: DataTableErrorHandler
        },
        columnDefs: [{
            'targets': 0,
            'searchable': false,
            'orderable': false,
            'className': 'dt-body-center',
            'render': function (data, type, full, meta) {
                return '<input type="checkbox" name="selectedcase[]" value="' + $('<div/>').text(data).html() + '">';
            }
        },
        {
            className: 'max-width-column-number', // Apply the CSS class,
            targets: 1                      // Index of the column to style
        },
        {
            className: 'max-width-column-number', // Apply the CSS class,
            targets: 2                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 9                      // Index of the column to style
        }],
        order: [[14, 'asc']],
        responsive: true,
        fixedHeader: true,
        processing: true,
        autoWidth: false,
        serverSide: true,
        deferRender: true,
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
                    var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '" data-bs-toggle="tooltip" title="Select Case to submit (report)" />';
                    return img;
                }
            },
            {
                "data": "policyNum",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.policyId + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "amount",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.amount + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "company",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.ownerDetail + '" class="thumbnail profile-image doc-profile-image" title="' + row.company + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
                    img += '<img src="' + row.ownerDetail + '" class="full-map" title="' + row.company + '" data-bs-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "pincode",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.personMapAddressUrl + '" class="thumbnail profile-image doc-profile-image" title="' + row.pincodeName + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
                    img += '<img src="' + row.personMapAddressUrl + '" class="full-map" title="' + row.pincodeName + '" data-bs-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '</div>';
                    return img;
                }
            },

            {
                "data": "distance",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.distance + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },

            {
                "data": "duration",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.duration + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.document + '" class="full-map" title="' + row.policyId + '" data-bs-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '<img src="' + row.document + '" class="thumbnail profile-image doc-profile-image" title="' + row.policyId + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
                    return img;
                }
            },

            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail table-profile-image">';
                    img += '<img src="' + row.customer + '" class="full-map" title="' + row.name + '" data-bs-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '<img src="' + row.customer + '" class="thumbnail table-profile-image" title="' + row.name + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "name",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.name + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "serviceType",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.serviceType + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "service",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.service + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "created",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.created + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "timePending",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.timePending + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            { "data": "timeElapsed", "bVisible": false }
        ],
        "rowCallback": function (row, data, index) {
            if (data.isNewAssigned) {
                $('td', row).addClass('isNewAssigned');
            }
        },
        "drawCallback": function (settings, start, end, max, total, pre) {
            // Reinitialize Bootstrap 5 tooltips
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (el) {
                return new bootstrap.Tooltip(el, {
                    html: true,
                    sanitize: false   // ⬅⬅⬅ THIS IS THE FIX
                });
            });
        }
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
        $("#allocatedcase").prop('disabled', true);
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
    $('#dataTable tbody').hide();
    $('#dataTable tbody').fadeIn(2000);
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });
    if ($("input[type='radio'].selected-case:checked").length) {
        $("#allocatedcase").prop('disabled', false);
    }
    else {
        $("#allocatedcase").prop('disabled', true);
    }

    // When user checks a radio button, Enable submit button
    $("input[type='radio'].selected-case").change(function (e) {
        if ($(this).is(":checked")) {
            $("#allocatedcase").prop('disabled', false);
        }
        else {
            $("#allocatedcase").prop('disabled', true);
        }
    });

    // Handle click on checkbox to set state of "Select all" control
    $('#dataTable tbody').on('change', 'input[type="radio"]', function () {
        // If checkbox is not checked
        if (this.checked) {
            $("#allocatedcase").prop('disabled', false);
        } else {
            $("#allocatedcase").prop('disabled', true);
        }
    });

    // Handle form submission event
    $('#checkboxes').on('submit', function (e) {
        var form = this;

        // Iterate over all checkboxes in the table
        table.$('input[type="checkbox"]').each(function () {
            // If checkbox doesn't exist in DOM
            if (!$.contains(document, this)) {
                // If checkbox is checked
                if (this.checked) {
                    // Create a hidden element
                    $(form).append(
                        $('<input>')
                            .attr('type', 'hidden')
                            .attr('name', this.name)
                            .val(this.value)
                    );
                }
            }
        });
    });
});