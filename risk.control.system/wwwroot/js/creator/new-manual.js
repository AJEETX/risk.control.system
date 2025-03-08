$(document).ready(function () {
    $('a.create-policy').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        $('a.create-policy').html("<i class='fas fa-sync fa-spin'></i> Add New");

        // Disable all buttons, submit inputs, and anchors
        $('button, input[type="submit"], a').prop('disabled', true);

        // Add a class to visually indicate disabled state for anchors
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });
        $('a').attr('disabled', 'disabled');
        $('button').attr('disabled', 'disabled');
        $('html button').css('pointer-events', 'none');
        $('html a').css({ 'pointer-events': 'none' }, { 'cursor': 'none' });
        $('.text').css({ 'pointer-events': 'none' }, { 'cursor': 'none' });

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });
    
    var table = $("#customerTableManual").DataTable({
        ajax: {
            url: '/api/Creator/GetManual',
            dataSrc: ''
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
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 1                      // Index of the column to style
            },
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 2                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 6                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 8                      // Index of the column to style
            }],
        order: [[15, 'asc']],
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
                "sDefaultContent": "<i class='far fa-edit' data-toggle='tooltip' title='Incomplete claim !!!'></i>",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    if (row.ready2Assign) {
                        var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '" data-toggle="tooltip" title="Ready to allocate(manual)" />';
                        return img;
                    }
                }
            }, {
                "data": "policyNum", "bSortable": false,
                "mRender": function (data, type, row) {
                    var popup = row.policyId;
                    if (row.agencyDeclineComment != '') {
                        popup += '\n' + row.agencyDeclineComment;
                    }
                    return '<span title="' + popup + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "amount",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.amount + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "pincode",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    if (row.pincodeName != '...') {
                        var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                        img += '<img src="' + row.personMapAddressUrl + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                        img += '<img src="' + row.personMapAddressUrl + '" class="full-map" title="' + row.pincodeName + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                        img += '</div>';
                        return img;
                    }
                    else {

                        return '<img src="/img/no-map.jpeg" class="profile-image doc-profile-image" title="No address" data-toggle="tooltip" />'
                    }
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
                    if (row.customerFullName == "?") {
                        var img = '<img alt="' + row.customerFullName + '" title="' + row.customerFullName + '" src="' + row.customer + '" class="table-profile-image-no-user" data-toggle="tooltip"/>';
                        return img;
                    }
                    else {
                        var img = '<div class="map-thumbnail table-profile-image">';
                        img += '<img src="' + row.customer + '" class="full-map" title="' + row.customerFullName + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                        img += '<img src="' + row.customer + '" class="thumbnail table-profile-image" />'; // Thumbnail image with class 'thumbnail'
                        img += '</div>';
                        return img;
                    }
                }
            },
            {
                "data": "name",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.customerFullName + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    if (row.beneficiaryFullName == "?") {
                        var img = '<img alt="' + row.beneficiaryFullName + '" title="' + row.beneficiaryFullName + '" src="' + row.beneficiaryPhoto + '" class="table-profile-image-no-user" data-toggle="tooltip"/>';
                        return img;
                    }
                    else {
                        var img = '<div class="map-thumbnail table-profile-image">';
                        img += '<img src="' + row.beneficiaryPhoto + '" class="thumbnail table-profile-image" />'; // Thumbnail image with class 'thumbnail'
                        img += '<img src="' + row.beneficiaryPhoto + '" class="full-map" title="' + row.beneficiaryFullName + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                        img += '</div>';
                        return img;
                    }
                }
            },
            {
                "data": "beneficiaryName",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.beneficiaryFullName + '" data-toggle="tooltip">' + data + '</span>'
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
                "data": "location",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.location + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "created",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.created + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "timePending"
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id="edit' + row.id + '" href="/CreatorManual/Details?Id=' + row.id + '" class="btn btn-xs btn-warning"><i class="fas fa-pencil-alt"></i> Edit</a>&nbsp;'
                    buttons += '<a id="details' + row.id + '" href="/CreatorManual/Delete?Id=' + row.id + '" class="btn btn-xs btn-danger"><i class="fa fa-trash"></i> Delete </a>'
                    return buttons;
                }
            },
            { "data": "timeElapsed", bVisible: false }
        ],
        "drawCallback": function (settings, start, end, max, total, pre) {
            
            $('#customerTableManual tbody').on('click', '.btn-danger', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('details', ''); // Extract the ID from the button's ID attribute
                getManualDetails(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
            $('#customerTableManual tbody').on('click', '.btn-warning', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('edit', ''); // Extract the ID from the button's ID attribute
                showManualEdit(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the edit page
            });
        },
        "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {
            if (aData.isNewAssigned) {
                $('td', nRow).addClass('isNewAssigned');
            }
        },
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    $('#customerTableManual')
        .on('mouseenter', '.map-thumbnail', function () {
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

    $('#customerTableManual').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });

    $('#customerTableManual tbody').hide();
    $('#customerTableManual tbody').fadeIn(2000);

    if ($("input[type='radio'].selected-case:checked").length) {
        $("#allocate-manual").prop('disabled', false);
    }
    else {
        $("#allocate-manual").prop('disabled', true);
    }

    // When user checks a radio button, Enable submit button
    $("input[type='radio'].selected-case").change(function (e) {
        if ($(this).is(":checked")) {
            $("#allocate-manual").prop('disabled', false);
        }
        else {
            $("#allocate-manual").prop('disabled', true);
        }
    });

    // Handle click on checkbox to set state of "Select all" control
    $('#customerTableManual tbody').on('change', 'input[type="radio"]', function () {
        // If checkbox is not checked
        if (this.checked) {
            $("#allocate-manual").prop('disabled', false);
        } else {
            $("#allocate-manual").prop('disabled', true);
        }
    });

    $('#allocate-manual').on('click', function (event) {
        $("body").addClass("submit-progress-bg");

        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        
        $('#allocate-manual').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Assign <b> <sub>manual</sub></b>");
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
});


function showManualEdit(id) {
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
function getManualDetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    
    $('a#details' + id + '.btn.btn-xs.btn-danger').html("<i class='fas fa-sync fa-spin'></i> Delete");
    disableAllInteractiveElements();

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    } 
}

