$(document).ready(function () {
    var table = $("#dataTable").DataTable({
        ajax: {
            url: '/api/agency/VendorInvestigation/GetOpenCases',
            type: 'GET',
            dataSrc: function (json) {
                console.log("Data after receiving:", json.data); // Debugging
                return json.data; // Return table data
            },
            data: function (d) {
                console.log("Data before sending:", d); // Debugging

                return {
                    draw: d.draw || 1,
                    start: d.start || 0,
                    length: d.length || 10,
                    search: d.search?.value || "", // Instead of empty string, send "all"
                    orderColumn: d.order?.[0]?.column ?? 12, // Default to column 15
                    orderDir: d.order?.[0]?.dir || "asc"
                };
            },
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    $.confirm({
                        title: 'Session Expired!',
                        content: 'Your session has expired or you are unauthorized. You will be redirected to the login page.',
                        type: 'red',
                        typeAnimated: true,
                        buttons: {
                            Ok: {
                                text: 'Login',
                                btnClass: 'btn-red',
                                action: function () {
                                    window.location.href = '/Account/Login';
                                }
                            }
                        },
                        onClose: function () {
                            window.location.href = '/Account/Login';
                        }
                    });
                }
                else if (xhr.status === 500) {
                    $.confirm({
                        title: 'Server Error!',
                        content: 'An unexpected server error occurred. You will be redirected to the Active page.',
                        type: 'orange',
                        typeAnimated: true,
                        buttons: {
                            Ok: function () {
                                window.location.href = '/VendorInvestigation/Open';
                            }
                        },
                        onClose: function () {
                            window.location.href = '/VendorInvestigation/Open';
                        }
                    });
                }
                else if (xhr.status === 400) {
                    $.confirm({
                        title: 'Bad Request!',
                        content: 'Try with valid data.You will be redirected to the Active page',
                        type: 'orange',
                        typeAnimated: true,
                        buttons: {
                            Ok: function () {
                                window.location.href = '/VendorInvestigation/Open';
                            }
                        },
                        onClose: function () {
                            window.location.href = '/VendorInvestigation/Open';
                        }
                    });
                }
            }
        },
        columnDefs: [

            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 0                      // Index of the column to style
            }, {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 1                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 8                      // Index of the column to style
            }],
        order: [[12, 'asc']],
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
                "data": "agent",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    if (row.caseWithPerson) {
                        var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                        img += '<img src="' + row.ownerDetail + '" class="thumbnail profile-image doc-profile-image" title="' + row.agent + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
                        img += '<img src="' + row.ownerDetail + '" class="full-map" title="' + row.agent + '" data-bs-toggle="tooltip"/>'; // Full map image with class 'full-map'
                        img += '</div>';
                        return img;
                    }
                    else {
                        var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                        img += '<img src="' + row.ownerDetail + '" class="full-map" title="' + row.agent + '" data-bs-toggle="tooltip"/>'; // Full map image with class 'full-map'
                        img += '<img src="' + row.ownerDetail + '" class="thumbnail profile-image doc-profile-image" title="' + row.agent + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
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
                    img += '<img src="' + row.personMapAddressUrl + '" class="thumbnail profile-image doc-profile-image"  title="' + row.pincodeName + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
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
                    return '<span title="' + row.name + '" data-bs-toggle="tooltip">' + data + '</span>';
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
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.timePending + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                        buttons += `<a data-id="${row.id}" class="btn btn-xs btn-info"><i class="fas fa-search"></i> Detail</a>`
                    return buttons;
                }
            },
            {
                "data": "timeElapsed",
                "bVisible": false
            }
        ],
        "rowCallback": function (row, data, index) {
            
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

        const url = `/VendorInvestigation/Detail?Id=${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = url;
        }, 1000);
    }
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }
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
});

function showdetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);

    $('a#details' + id + '.btn.btn-xs.btn-info').html("<i class='fas fa-sync fa-spin'></i> Detail");
    disableAllInteractiveElements();

    var nodes = document.getElementById("article").getElementsByTagName('*');
    for (var i = 0; i < nodes.length; i++) {
        nodes[i].disabled = true;
    }
}