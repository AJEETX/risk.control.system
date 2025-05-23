﻿$(document).ready(function () {
    $('#allocatedcase').on('click', function (event) {
        $("body").addClass("submit-progress-bg");

        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        
        $('#allocatedcase').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Allocate <sub>new</sub>");
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

    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/agency/supervisor/GetNew',
            dataSrc: '',
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Account/Login'; // Or session timeout handler
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
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 2                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 7                      // Index of the column to style
            },
            {
                className: 'max-width-column', // Apply the CSS class,
                targets: 10                      // Index of the column to style
            }],
        order: [[14, 'asc']],
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
                "sDefaultContent": "<i class='fas fa-question'  data-toggle='tooltip' title='Enquiry'></i> ",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    if (!row.isQueryCase) {

                        var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '"  data-toggle="tooltip" title="Select Case to Allocate" />';
                        return img;
                    }
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
                "data": "company",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.ownerDetail + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '<img src="' + row.ownerDetail + '" class="full-map" title="' + row.company + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
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
                    img += '<img src="' + row.customer + '" class="full-map" title="' + row.name + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
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
                "data": "addressLocationInfo",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.addressLocationInfo + '" data-toggle="tooltip">' + data + '</span>'
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
                    if (row.isQueryCase) {
                        buttons += '<a id="details' + row.id + '" href="/SuperVisor/ReplyEnquiry?Id=' + row.id + '"  class="btn btn-xs btn-warning"><i class="fas fa-question" aria-hidden="true"></i> ENQUIRY </a>'
                    }
                    else {
                        buttons += '<a id="details' + row.id + '" href="/SuperVisor/CaseDetail?Id=' + row.id + '"  class="btn btn-xs btn-info"><i class="fas fa-search"></i> Details</a>'
                    }
                    return buttons;
                }
            },
            { "data":"timeElapsed","bVisible":false}
        ],
        "drawCallback": function (settings) {
            $('#customerTable tbody').off('click', '.btn-info').on('click', '.btn-info', function (e) {
                e.preventDefault();
                var id = $(this).attr('id').replace('details', '');
                showdetails(id);
                window.location.href = $(this).attr('href');
            });

            $('#customerTable tbody').off('click', '.btn-warning').on('click', '.btn-warning', function (e) {
                e.preventDefault();
                var id = $(this).attr('id').replace('details', '');
                showenquiry(id);
                window.location.href = $(this).attr('href');
            });
        },
        "rowCallback": function (row, data, index) {
            if (data.isNewAssigned) {
                $('td', row).addClass('isNewAssigned');
            }
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
        $("#investigatecase").prop('disabled', true);
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

    $('#customerTable tbody').hide();
    $('#customerTable tbody').fadeIn(2000);
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
    $('#customerTable tbody').on('change', 'input[type="radio"]', function () {
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

function showdetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    
    $('a#details' + id + '.btn.btn-xs.btn-info').html("<i class='fas fa-sync fa-spin'></i> Details");
    disableAllInteractiveElements();

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}


function showenquiry(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    
    $('a#details' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> ENQUIRY");
    disableAllInteractiveElements();

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}