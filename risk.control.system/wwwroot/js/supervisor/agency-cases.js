$(document).ready(function () {
    $('#allocatedcase').on('click', function (event) {
        $("body").addClass("submit-progress-bg");

        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('#allocatedcase').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Allocate");
        disableAllInteractiveElements();

        $('#radioButtons').submit();
        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#investigatecase').on('click', function (event) {
        $("body").addClass("submit-progress-bg");

        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        // Disable all buttons, submit inputs, and anchors

        $('#investigatecase').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Investigate");
        disableAllInteractiveElements();

        $('#radioButtons').submit();
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
            url: '/api/agency/VendorInvestigation/GetNewCases',
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
                    search: d.search?.value || "", // Instead of empty string, send "all"
                    orderColumn: d.order?.[0]?.column ?? 12, // Default to column 15
                    orderDir: d.order?.[0]?.dir || "desc"
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
                else if (xhr.status === 400) {
                    $.confirm({
                        title: 'Bad Request!',
                        content: 'Try with valid data.You will be redirected to Dashboard page',
                        type: 'orange',
                        typeAnimated: true,
                        buttons: {
                            Ok: function () {
                                window.location.href = '/DashBoard/Index';
                            }
                        },
                        onClose: function () {
                            window.location.href = '/DashBoard/Index';
                        }
                    });
                }
                else {
                    $.confirm({
                        title: 'Server Error!',
                        content: 'An unexpected server error occurred. You will be redirected to Dashboard page.',
                        type: 'orange',
                        typeAnimated: true,
                        buttons: {
                            Ok: function () {
                                window.location.href = '/DashBoard/Index';
                            }
                        },
                        onClose: function () {
                            window.location.href = '/DashBoard/Index';
                        }
                    });
                }
            }
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
            targets: 3                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 7                      // Index of the column to style
        },
        {
            className: 'max-width-column-name', // Apply the CSS class,
            targets: 8                      // Index of the column to style
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
                "sDefaultContent": "<i class='fas fa-question'  data-bs-toggle='tooltip' title='Enquiry'></i> ",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    if (!row.isQueryCase) {
                        var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '"  data-bs-toggle="tooltip" title="Select Case to Allocate" />';
                        return img;
                    }
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
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "pincode",
                "mRender": function (data, type, row) {
                    if (row.pincodeName != '...') {
                        const formattedUrl = row.personMapAddressUrl
                            .replace("{0}", "500")
                            .replace("{1}", "500");

                        return `
                        <div class="map-thumbnail profile-image doc-profile-image">
                            <img src="${formattedUrl}"
                                 title="${row.pincodeName}"
                                 class="thumbnail profile-image doc-profile-image preview-map-image open-map-modal"
                                 data-bs-toggle="tooltip"
                                 data-bs-placement="top"
                                 data-img='${formattedUrl}'
                                 data-title='Addresss: ${row.pincodeName}' />
                        </div>`;
                    } else {
                        return '<img src="/img/no-map.jpeg" class="profile-image doc-profile-image" title="No address" data-toggle="tooltip" />';
                    }
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img data-title="Case Document: ' + row.policyId + '" data-img="' + row.document + '" src="' + row.document + '" class="thumbnail profile-image doc-profile-image open-map-modal" title="' + row.policyId + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
                    img += '</div>';
                    return img;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail table-profile-image">';
                    img += '<img data-title="Name: ' + row.name + '" data-img="' + row.customer + '" src="' + row.customer + '" class="thumbnail table-profile-image open-map-modal" title="' + row.name + '" data-bs-toggle="tooltip"/>'; // Thumbnail image with class 'thumbnail'
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
                "data": "addressLocationInfo",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + row.addressLocationInfo + '" data-bs-toggle="tooltip">' + data + '</span>'
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
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    if (row.isQueryCase) {
                        buttons += `<a data-id="${row.id}" class="btn btn-xs btn-warning"><i class="fas fa-question" aria-hidden="true"></i> Enquiry </a> &nbsp;`;
                    }
                    else {
                        buttons += `<a data-id="${row.id}" class="btn btn-xs btn-info"><i class="fas fa-search"></i> Detail</a>`
                    }
                    return buttons;
                }
            },
            { "data": "timeElapsed", "bVisible": false }
            ,
            { "data": "isNewAssigned", "bVisible": false }
        ],

        "rowCallback": function (row, data, index) {
            if (data.isNewAssigned) {
                $('td', row).addClass('isNewAssigned');
                // Remove the class after 3 seconds
                setTimeout(function () {
                    $('td', row).removeClass('isNewAssigned');
                }, 3000);
            }
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

        const url = `/VendorInvestigation/CaseDetail?Id=${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = url;
        }, 1000);
    }
    $('body').on('click', 'a.btn-warning', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        showenquiry(id, this);
    });
    function showenquiry(id, element) {
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        showSpinnerOnButton(element, "Enquiry");

        const editUrl = `/VendorInvestigation/ReplyEnquiry?Id=${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = editUrl;
        }, 1000);
    }
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }

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
    $(document).on("click", ".open-map-modal", function () {
        $("#mapModal").modal("show");

        const imageUrl = $(this).data("img");
        const title = $(this).data("title");

        $("#modalMapImage").attr("src", imageUrl);
        $("#mapModalLabel").text(title || "Map Preview");
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
        $("#investigatecase").prop('disabled', false);
    }
    else {
        $("#allocatedcase").prop('disabled', true);
        $("#investigatecase").prop('disabled', true);
    }

    // When user checks a radio button, Enable submit button
    $("input[type='radio'].selected-case").change(function (e) {
        if ($(this).is(":checked")) {
            $("#allocatedcase").prop('disabled', false);
            $("#investigatecase").prop('disabled', false);
        }
        else {
            $("#allocatedcase").prop('disabled', true);
            $("#investigatecase").prop('disabled', true);
        }
    });

    // Handle click on checkbox to set state of "Select all" control
    $('#dataTable tbody').on('change', 'input[type="radio"]', function () {
        // If checkbox is not checked
        if (this.checked) {
            $("#allocatedcase").prop('disabled', false);
            $("#investigatecase").prop('disabled', false);
        } else {
            $("#allocatedcase").prop('disabled', true);
            $("#investigatecase").prop('disabled', true);
        }
    });
    let askConfirmation = false;

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