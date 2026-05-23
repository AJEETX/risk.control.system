$(document).ready(function () {
    var table = $('#dataTable').DataTable({
        ajax: {
            url: '/api/Agency/GetAgentWithCases/' + $('#caseId').val(),
            dataSrc: '',
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    $.alert({
                        title: 'Session Expired! Login again',
                    });
                    window.location.href = '/Account/Login'; // Or session timeout handler
                }
                if (xhr.status === 500) {
                    $.alert({
                        title: 'Server Error Occurred! Try again.',
                    });
                    window.location.href = '/VendorInvestigation/Allocate'; // // Refresh page
                }
                if (xhr.status === 400) {
                    $.alert({
                        title: 'Bad Request occurred!',
                    });
                    window.location.href = '/VendorInvestigation/Allocate'; // // Refresh page
                }
            }
        },
        columnDefs: [{
            'targets': 0,
            'searchable': false,
            'orderable': false,
            'className': 'dt-body-center',
            'render': function (data, type, full, meta) {
                return '<input type="checkbox" name="id[]" value="' + $('<div/>').text(data).html() + '">';
            }
        },
        {
            orderable: false, targets: [0, 1, 3, 4]
        }, // Disable ordering for specific columns
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 2                      // Index of the column to style
        },
        {
            className: 'max-width-column-number', // Apply the CSS class,
            targets: 3                      // Index of the column to style
        },
        {
            className: 'max-width-column', // Apply the CSS class,
            targets: 5                      // Index of the column to style
        },
        {
            className: 'max-width-column', // Apply the CSS class,
            targets: 8                      // Index of the column to style
            },
            {
                className: 'max-width-column', // Apply the CSS class,
                targets: 9                      // Index of the column to style
            }
        ],
        order: [[11, 'asc']],
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;', processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            /* Name of the keys from     
            data file source */
            {
                "data": "id", // "id" is the key from the data file
                "sDefaultContent": '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>',
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '" data-bs-toggle="tooltip" title="Select Agent" />';
                    return img;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img data-title="Agent: ' + row.name + '" data-img="' + row.photo + '" src="' + row.photo + '" class="thumbnail table-profile-image open-image-modal" title="' + row.name + '" data-bs-toggle="tooltip">'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "rawEmail",
                "mRender": function (data, type, row) {
                    return '<span class="blue" title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "phone",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "personMapAddressUrl",
                "mRender": function (data, type, row) {
                    const formattedUrl = row.personMapAddressUrl
                        .replace("{0}", "400")
                        .replace("{1}", "400");

                    return `
                        <div class="map-thumbnail profile-image doc-profile-image">
                            <img src="${formattedUrl}"
                                 title="${row.mapDetails}"
                                 class="thumbnail profile-image doc-profile-image preview-map-image open-map-modal"
                                 data-bs-toggle="tooltip"
                                 data-bs-placement="top"
                                 data-img='${formattedUrl}'
                                 data-agent-address='${row.agentAddress}'
                                 data-person-address='${row.personAddress}'
                                 data-person-label='${row.personAddressLabel}'
                                 data-distance='${row.distance}'
                                 data-duration='${row.duration}'
                                 data-title='${row.mapDetails}' />
                        </div>`;
                }
            },
            {
                "data": "distance",
                "mRender": function (data, type, row) {
                    return '<span class="distance" title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "duration",
                "mRender": function (data, type, row) {
                    return '<span class="duration" title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "addressline",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "state",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "pinCode",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "count",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "durationInSeconds",
                "bVisible": false
            },
            {
                "data": "distanceInMetres",
                "bVisible": false
            }
        ],
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
        table.ajax.reload(null, false);
        $("#allocatedcase").prop('disabled', true);
    });
    $('#dataTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });

    $('#dataTable tbody').hide();
    $('#dataTable tbody').fadeIn(2000);

    $(document).on("click", ".open-image-modal", function () {
        $("#imageModal").modal("show");
        const imageUrl = $(this).data("img");
        const title = $(this).data("title");

        $("#modalImage").attr("src", imageUrl);
        $("#mapImageLabel").text(title || "Map Preview");
    });

    $(document).on("click", ".open-map-modal", function () {
        $("#mapModal").modal("show");

        const imageUrl = $(this).data("img");

        $("#modalMapImage").attr("src", imageUrl);

        const agentAddress = $(this).data("agentAddress");
        $("#mapModalAgentAddress").text(agentAddress || "Agent Address (S)");

        const personAddressLabel = $(this).data("personLabel");
        $("#mapModalPersonAddressLabel").text(personAddressLabel || "Person Address (S)");

        const personAddress = $(this).data("personAddress");
        $("#mapModalPersonAddress").text(personAddress);

        const distance = $(this).data("distance");
        $("#mapModalDistance").text(distance);

        const duration = $(this).data("duration");
        $("#mapModalDuration").text(duration);
    });
    // Handle click on checkbox to set state of "Select all" control   
    $('#dataTable tbody').on('change', 'input[type="radio"]', function () {
        // If checkbox is not checked       
        if (this.checked) {
            // Get the selected row
            var selectedRow = $(this).closest('tr');

            var rowIndex = $(this).closest('tr').index();
            var rowData = $('#dataTable').DataTable().row(rowIndex).data();

            // Assuming the data object has `duration` and `distance` keys
            var duration = rowData.duration; // "15 mins"
            var distance = rowData.distance; // "10 km"

            var distanceInMeters = rowData.distanceInMetres;
            var durationInSeconds = rowData.durationInSeconds;

            // Retrieve the map URL from the specific column (e.g., 5th column)
            var mapUrl = selectedRow.find('.full-driving-map').attr('src');

            // Assign the map URL to the hidden input field
            $('#drivingMap').val(mapUrl);
            $('#drivingDistance').val(distance);
            $('#durationInSeconds').val(durationInSeconds);
            $('#distanceInMeters').val(distanceInMeters);
            $('#drivingDuration').val(duration);

            $("#allocatedcase").prop('disabled', false);
        } else {
            $("#allocatedcase").prop('disabled', true);
        }
    });
    var askConfirmation = true;
    $('#allocate-agent').submit(function (e) {
        if (askConfirmation) {
            var content = $('#ReSelect').val() == 'True' ? 'Re-Allocate' :'Allocate'
            e.preventDefault();
            $.confirm({
                title: `Confirm ${content}`,
                content: `Are you sure to ${content}?`,

                icon: 'fas fa-external-link-alt',
                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: `${content} <sub>agent</sub>`,
                        btnClass: 'btn-warning',
                        action: function () {
                            askConfirmation = false;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#allocatedcase').attr('disabled', 'disabled');
                            $('#allocatedcase').html(`<i class='fas fa-sync fa-spin' aria-hidden='true'></i> ${content} <sub>agent</sub>`);

                            $('#allocate-agent').submit();
                            var article = document.getElementById("article");
                            if (article) {
                                var nodes = article.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
                                }
                            }
                        }
                    },
                    cancel: {
                        text: "Cancel",
                        btnClass: 'btn-default'
                    }
                }
            });
        }
    })
});