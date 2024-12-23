$(document).ready(function () {
    var table = $('#customerTable').DataTable({
        ajax: {
            url: '/api/Agency/GetAgentLoad',
            dataSrc: ''
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
                orderable: false, targets: [0, 2, 3, 4]
            }, // Disable ordering for specific columns
        ],
        order: [[1, 'asc']],
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
                "sDefaultContent": '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>',
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '" data-toggle="tooltip" title="Select Agent" />';
                    return img;
                }
            },
            {
                "data": "email",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.rawEmail + '" data-toggle="tooltip">' + row.rawEmail + '</span>';
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.photo + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '<img src="' + row.photo + '" class="full-map" title="' + row.name + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                    img += '</div>';
                    return img;
                }
            },
            {
                "data": "phone",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.phone + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "personMapAddressUrl",
                "mRender": function (data, type, row) {
                    if (row.pincodeName != '...') {
                        var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                        img += '<img src="' + row.personMapAddressUrl + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                        img += '<img src="' + row.personMapAddressUrl + '" class="full-map" title="' + row.addressline + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
                        img += '</div>';
                        return img;
                    }
                    else {

                        return '<img src="/img/no-user.png" class="profile-image doc-profile-image" title="No Photo" data-toggle="tooltip" />'
                    }


                    return '<span title="' + row.mapUrl + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "addressline",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.addressline + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "count",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.count + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
        ]
    });

    $('#customerTable').on('mouseenter', '.map-thumbnail', function () {
        $(this).find('.full-map').show(); // Show full map
    }).on('mouseleave', '.map-thumbnail', function () {
        $(this).find('.full-map').hide(); // Hide full map
    });

    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'bottom',
            html: true
        });
    });

    $('#customerTable tbody').hide();
    $('#customerTable tbody').fadeIn(2000);

    $('#customerTable').on('mouseenter', '.map-thumbnail', function () {
        $(this).find('.full-map-title').show(); // Show full map
        $(this).find('.full-map').show(); // Show full map
    }).on('mouseleave', '.map-thumbnail', function () {
        $(this).find('.full-map-title').hide(); // Hide full map
        $(this).find('.full-map').hide(); // Hide full map
    });
    // Handle click on checkbox to set state of "Select all" control    
    $('#customerTable tbody').on('change', 'input[type="radio"]', function () {
        // If checkbox is not checked        
        if (this.checked) {
            $("#allocatedcase").prop('disabled', false);
        } else {
            $("#allocatedcase").prop('disabled', true);
        }
    });
    var askConfirmation = true;
    $('#radioButtons').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Allocate",
                content: "Are you sure to allocate?",
    
                icon: 'fas fa-external-link-alt',
                type: 'red',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Allocate <sub>agent</sub>",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = false;

                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            $('#allocate-case').attr('disabled', 'disabled');
                            $('#allocate-case').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Allocate <sub>agent</sub>");

                            $('#radioButtons').submit();
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