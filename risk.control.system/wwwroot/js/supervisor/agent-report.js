$(document).ready(function () {
    $('#allocatedcase').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#allocatedcase').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Agent <sub>report</sub>");

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
            url: '/api/agency/VendorInvestigation/GetAgentReports',
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
                    orderColumn: d.order?.[0]?.column ?? 12,
                    orderDir: d.order?.[0]?.dir || "desc"
                };
            },
            error: DataTableErrorHandler
        },
        columnDefs: [
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
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 12                      // Index of the column to style
            }],
        order: [[12, 'desc']],
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
                    var img = '<input name="id" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '" data-bs-toggle="tooltip" title="Select Case to submit (report)" />';
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
                    img += '<img src="' + row.ownerDetail + '" class="profile-image doc-profile-image" title="' + row.company + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "pincode",
                "bSortable": false,
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
                    return '<span title="' + row.distance + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },

            {
                "data": "duration",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.duration + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img data-title="Case Document: ' + row.policyId + '" data-img="' + row.document + '" src="' + row.document + '" class="thumbnail profile-image doc-profile-image open-image-modal" title="' + row.policyId + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
                    return img;
                }
            },

            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail table-profile-image">';
                    img += '<img data-title="Person: ' + row.name + '" data-img="' + row.customer + '" src="' + row.customer + '" class="thumbnail table-profile-image open-image-modal" title="' + row.name + '" data-bs-toggle="tooltip" />'; // Thumbnail image with class 'thumbnail'
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
                "render": function (data, type, row) {
                    if (!data) return '';
                    let date = new Date(data);
                    var localDate = date.toLocaleString('en-IN', {
                        day: '2-digit',
                        month: 'short',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                        second: '2-digit',
                        hour12: true
                    });
                    return `<span title="${localDate}" data-bs-toggle="tooltip"><small><strong>${localDate}</strong></small></span>`;
                }
            },
            {
                "data": "timePending",
                "mRender": function (data, type, row) {
                    return `<small><strong>${data} </strong> </small>`;
                }
            },
            { "data": "timeElapsed", "bVisible": false }
        ],
        "rowCallback": function (row, data, index) {
            if (data.isNewAssigned) {
                $('td', row).addClass('isNewAssigned');
                setTimeout(function () {
                    $('td', row).removeClass('isNewAssigned');
                }, 3000);
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
        const title = $(this).data("title");

        $("#modalMapImage").attr("src", imageUrl);
        $("#mapModalLabel").text(title || "Map Preview");

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
    $('#dataTable tbody').hide();
    $('#dataTable tbody').fadeIn(2000);
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });

    $('#dataTable').on('change', 'input[name="id"]', function () {
        $('#allocatedcase').prop('disabled', false);
    });

    $('#allocatedcase').on('click', function () {
        // Find the checked radio button
        var id = $("input[name='id']:checked").val();

        if (id) {
            // Redirect to the clean URL
            window.location.href = 'ReportDetail/' + id;
        } else {
            $.alert("Please select a case.");
        }
    });
});