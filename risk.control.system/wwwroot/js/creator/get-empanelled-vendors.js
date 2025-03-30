$(function () {
    var claimId = $('#claimId').val();
    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/Company/GetEmpanelledVendors',
            dataSrc: ''
        },
        columnDefs: [
            {
                'targets': 0,
                'searchable': false,
                'orderable': false,
                'className': 'dt-body-center',
                'render': function (data, type, full, meta) {
                    return '<input type="checkbox" name="id[]" value="' + $('<div/>').text(data).html() + '">';
                }
            },
            {
                className: 'max-width-column', // Apply the CSS class,
                targets: 5                      // Index of the column to style
            },],
        order: [[1, 'asc']],
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;', processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            /* Name of the keys from            data file source */
            {
                "sDefaultContent": '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>',
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '" data-toggle="tooltip" title="Select Agency" />';
                    return img;
                }
            },
            {
                "data": "domain",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '';
                    for (var i = 1; i <= 5; i++) {
                        img += '<img id="' + i + '" src="/images/StarFade.gif" class="rating" vendorId="' + row.id + '"/>';
                    }

                    // Add the rate count badge
                    img += ' <span title="(Total count of user rated) star ratings" class="badge badge-light" data-toggle="tooltip"> (' + row.rateCount + ')';

                    // Calculate and display the average rating if available
                    if (row.rateCount && row.rateCount > 0) {
                        var averageRating = row.rateTotal / row.rateCount;
                        img += '<span class="avr"><sup>' + averageRating + '</sup></span>';
                    }
                    img += '</span>';
                    // Add the result span
                    img += '<br /> <span class="result"></span>';

                    // Return the domain with the appended rating images and information
                    return '<span title="' + row.name + '" data-toggle="tooltip">' + data + '</span>' + '<br /> ' + img;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<div class="map-thumbnail profile-image doc-profile-image">';
                    img += '<img src="' + row.document + '" class="thumbnail profile-image doc-profile-image" />'; // Thumbnail image with class 'thumbnail'
                    img += '<img src="' + row.document + '" class="full-map" title="' + row.name + '" data-toggle="tooltip"/>'; // Full map image with class 'full-map'
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
                "data": "phone",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-toggle="tooltip"/>' + data + '</span>'
                }
            },
            {
                "data": "address",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.address + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "district",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.district + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "state",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.state + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "country",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.country + '" data-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-toggle="tooltip"/>' + data + '</span>';
                }
            },
            {
                "data": "caseCount",
                "mRender": function (data, type, row) {
                    return '<span title="Total number of current cases = ' + row.caseCount + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id="details' + row.id + '" href="/CreatorAuto/VendorDetail?Id=' + row.id + '&selectedcase=' + claimId + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Agency Info</a>&nbsp;'
                    return buttons;
                }
            }],
        "drawCallback": function (settings, start, end, max, total, pre) {

            $('#customerTable tbody').on('click', '.btn-info', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('details', ''); // Extract the ID from the button's ID attribute
                getdetails(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });

        },
        "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {
            if (aData.caseCount > 10) {
                $('td', nRow).css('background-color', '#ffa');
            }
        }, error: function (xhr, status, error) { alert('err ' + error) }
    });
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'bottom',
            html: true
        });
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
    table.on('draw', function () {
        // Loop through each row of the table after it has been redrawn
        $("#customerTable > tbody > tr").each(function () {
            var av = parseFloat($(this).find("span.avr").text()); // Get the average rating
            if (!isNaN(av) && av > 0) {  // Ensure it's a valid rating
                var stars = $(this).find("img.rating");  // Get all star images in the row

                // Loop through each star and highlight them based on the average rating
                stars.each(function (index) {
                    var star = $(this);
                    if (index < Math.floor(av)) {  // Fully filled stars
                        star.attr("src", "/images/FilledStar.jpeg");
                    } else if (index === Math.floor(av) && av % 1 !== 0) {  // Handle half-filled stars
                        star.attr("src", "/images/HalfStar.jpeg");  // You need a half-star image
                    } else {
                        star.attr("src", "/images/StarFade.gif");  // Faded stars
                    }
                });
            }
        });
    });

    // Initial draw to set the stars when the table first loads
    table.draw();
 
    $('#customerTable tbody').hide();
    $('#customerTable tbody').fadeIn(2000);
    $('#customerTable tbody').on('mouseover', 'img.rating', function () {
        var starImage = $(this);

        if (!starImage.data('bs.tooltip')) {
            // Set the title attribute for the tooltip text
            starImage.attr("title", "Rate this agency");

            // Set data-toggle to enable the tooltip
            starImage.attr("data-toggle", "tooltip");
            new bootstrap.Tooltip(starImage[0]);
        }
        // Get the ID and vendorId for additional logic
        var starId = starImage.attr('id'); // Get the ID of the hovered star
        var vendorId = starImage.attr('vendorId'); // Get the vendorId for additional logic

        // Call a function to update the star image (this function should be defined elsewhere)
        giveRating(starImage, "FilledStar.jpeg");

        // Change the cursor to pointer when hovering over the star
        starImage.css("cursor", "pointer");

        // Initialize the tooltip (this should be called after setting the attributes)
        var tooltip = bootstrap.Tooltip.getInstance ? bootstrap.Tooltip.getInstance(starImage[0]) : null;

        if (tooltip) {
            tooltip.show();
        }
    });
    $('#customerTable tbody').on('mouseleave', 'img.rating', function () {
        var starImage = $(this);

        // Dispose of the tooltip only if it has been initialized
        var tooltip = bootstrap.Tooltip.getInstance ? bootstrap.Tooltip.getInstance(starImage[0]) : null;

        if (tooltip) {
            tooltip.dispose();
        }

        // Call functions to reset the star rating
        giveRating(starImage, "StarFade.gif");
        refilRating(starImage);
    });
    $('#customerTable tbody').on('click', 'img.rating', function (e) {
        var starId = $(this).attr('id');
        var vendorId = $(this).attr('vendorId');
        console.log('Rated ' + starId + ' stars for vendor ' + vendorId);
        $(this).css('color', 'red');
        var url = "/Vendors/PostRating?rating=" + parseInt($(this).attr("id")) + "&mid=" + $(this).attr("vendorId");
        $.post(url, null, function (data) {
            $(e.currentTarget).closest('tr').find('span.result').text(data).css({
                'color': 'red',
                'font-size': 'small'
            });
            $("#result").text(data);
        });
    });
    if ($("input[type='radio'].selected-case:checked").length) {
        $("#allocatedcase").prop('disabled', false);
    } else {
        $("#allocatedcase").prop('disabled', true);
    }
    // When user checks a radio button, Enable submit button    
    $("input[type='radio'].selected-case").change(function (e) {
        if ($(this).is(":checked")) {
            $("#allocatedcase").prop('disabled', false);
        } else {
            $("#allocatedcase").prop('disabled', true);
        }
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
    $("#customerTable > tbody  > tr").each(function () {
        var av = $(this).find("span.avr").text();
        if (av != "" || av != null) {
            var img = $(this).find("img[id='" + parseInt(av) + "']");
            img.attr("src", "/images/FilledStar.jpeg").prevAll("img.rating").attr("src", "/images/FilledStar.jpeg");
        }
    });

    var askConfirmation = true;
    $('#radioButtons').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault(); $.confirm({
                title: "Confirm Assign", content: "Are you sure ?",
                icon: 'fas fa-external-link-alt', type: 'blue', closeIcon: true, buttons: {
                    confirm: {
                        text: "Assign <sub>manual</sub>", btnClass: 'btn-info', action: function () {
                            askConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners                            
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);
                            
                            $('#allocatedcase').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Assign <sub>manual</sub>");
                            disableAllInteractiveElements();

                            $('#radioButtons').submit();
                            var article = document.getElementById("article");
                            if (article) {
                                var nodes = article.getElementsByTagName('*');
                                for (var i = 0; i < nodes.length; i++) {
                                    nodes[i].disabled = true;
                                }
                            }
                        }
                    }, cancel: { text: "Cancel", btnClass: 'btn-default' }
                }
            });
        }
    })

    $('#refreshTable').click(function () {
        table.ajax.reload(null, false); // false => Retains current page
    });
});
function giveRating(img, image) {
    img.attr("src", "/Images/" + image).prevAll("img.rating").attr("src", "/Images/" + image);
} function refilRating(img1) {
    var rt = $(img1).closest('tr').find("span.avr").text();
    var img = $(img1).closest('tr').find("img[id='" + parseInt(rt) + "']");
    img.attr("src", "/images/FilledStar.jpeg").prevAll("img.rating").attr("src", "/images/FilledStar.jpeg");
}

function getdetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);

    $('a#details' + id + '.btn.btn-xs.btn-info').html("<i class='fas fa-sync fa-spin'></i> Agency Info");
    disableAllInteractiveElements()
    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}