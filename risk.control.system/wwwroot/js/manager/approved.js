$(document).ready(function () {
    var table = $("#dataTable").DataTable({
        ajax: {
            url: '/api/Manager/GetApprovedCases',
            type: 'GET',
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
        columnDefs: [
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 0                      // Index of the column to style
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
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 8                      // Index of the column to style
            },
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 10                      // Index of the column to style
            },
            {
                className: 'max-width-column-number', // Apply the CSS class,
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
                "data": "agency",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span class="badge badge-light" title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
                ///<button type="button" class="btn btn-lg btn-danger" data-bs-toggle="popover" title="Popover title" data-content="And here's some amazing content. It's very engaging. Right?">Click to toggle popover</button>
            },
            {
                "data": "pincode",
                "mRender": function (data, type, row) {
                    if (row.pincodeName != '...') {
                        return `
            <div class="map-thumbnail profile-image doc-profile-image">
                <img src="${row.personMapAddressUrl}" title='${row.pincodeName}'
                     class="thumbnail profile-image doc-profile-image preview-map-image"
                     data-toggle="modal"
                     data-target="#mapModal"
                     data-img='${row.personMapAddressUrl}'
                     data-title='${row.pincodeName}' />
            </div>`;
                    } else {
                        return '<img src="/img/no-map.jpeg" class="profile-image doc-profile-image" title="No address" data-bs-toggle="tooltip" />';
                    }
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
                    img += '<img src="' + row.document + '" class="profile-image doc-profile-image" title="' + row.policyId + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
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
                    img += '<img src="' + row.customer + '" class="table-profile-image" title="' + row.name + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "name",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.name + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail table-profile-image">';
                    img += '<img src="' + row.beneficiaryPhoto + '" class="table-profile-image" title="' + row.beneficiaryName + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
                    img += '<img src="' + row.beneficiaryPhoto + '" class="full-map" title="' + row.beneficiaryName + '" data-bs-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "beneficiaryName",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.beneficiaryName + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "serviceType",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.serviceType + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "created",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.created + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<span class="checkbox">';
                    if (row.autoAllocated) {
                        buttons += '<i class="fas fa-cog fa-spin" title="AUTO ALLOCATION" data-bs-toggle="tooltip"></i>';
                    } else {
                        buttons += '<i class="fas fa-user-tag" title="MANUAL ALLOCATION" data-bs-toggle="tooltip"></i>';
                    }
                    buttons += '</span>';

                    return buttons;
                }
            },
            {
                "data": "timePending",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.timePending + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += `<a data-id="${row.id}" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;`
                    //buttons += (row.canDownload)
                    //    ? '<a href="/Report/PrintPdfReport?Id=' + row.id + '" class="btn btn-xs btn-danger"><i class="far fa-file-pdf"></i> PDF</a>'
                    //    : '<button class="btn btn-xs btn-secondary" disabled><i class="far fa-file-pdf"></i> limit Reached</button>';
                    return buttons;
                }
            },
            { "data": "timeElapsed", "bVisible": false },
            { "data": "policy", bVisible: false }
        ],
        rowCallback: function (row, data, index) {
            $('.btn-info', row).addClass('btn-white-color');
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
    $('body').on('click', 'a.btn-info', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        showdetail(id, this);
    });
    function showdetail(id, element) {
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        showSpinnerOnButton(element, "Detail");

        const editUrl = `/Manager/ApprovedDetail?Id=${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = editUrl;
        }, 1000);
    }
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }
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
    $(document).on('show.bs.modal', '#mapModal', function (event) {
        var trigger = $(event.relatedTarget); // The <img> clicked
        var imageUrl = trigger.data('img');
        var title = trigger.data('title');

        var modal = $(this);
        modal.find('#modalMapImage').attr('src', imageUrl);
        modal.find('.modal-title').text(title || 'Map Preview');
    });
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });
});