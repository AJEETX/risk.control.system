$(function () {
    var claimId = $('#claimId').val();
    var vendorId = $('#vendorId').val();
    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/Company/GetEmpanelledAgency?caseId=' + claimId,
            dataSrc: '',
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Account/Login'; // Or session timeout handler
                }
            }
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
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 1                      // Index of the column to style
            },
            {
                className: 'max-width-column-claim', // Apply the CSS class,
                targets: 3                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 4                      // Index of the column to style
            },
            {
                className: 'max-width-column', // Apply the CSS class,
                targets: 5                      // Index of the column to style
            },        ],
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
                    var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '" data-bs-toggle="tooltip" title="Select Agency" />';
                    return img;
                }
            },
            {
                "data": "domain",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '';
                    for (var i = 1; i <= 5; i++) {
                        img += '<img id="' + i + '" src="/img/StarFade.gif" class="rating" vendorId="' + row.id + '"/>';
                    }

                    // Add the rate count badge
                    img +=
                        ' <span class="badge badge-light" ' +
                        'data-bs-toggle="tooltip" data-bs-html="true" ' +
                        'title="(Total users rated)<sup>star ratings</sup>"> (' + row.rateCount + ')</span>';

                    // Calculate and display the average rating if available
                    if (row.rateCount && row.rateCount > 0) {
                        var averageRating = row.rateTotal / row.rateCount;
                        img += '<span class="avr"><sup>' + averageRating + '</sup></span>';
                    }
                    img += '</span>';
                    // Add the result span
                    img += '<br /> <span class="result" data-toggle="tooltip"></span>';

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
                    img += '<img src="' + row.document + '" class="full-map" title="' + row.name + '" data-bs-toggle="tooltip"/>'; // Full map image with class 'full-map'
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
                "data": "phone",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-toggle="tooltip"/>' + data + '</span>'
                }
            },
            {
                "data": "address",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.address + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "district",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.district + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "state",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.state + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "caseCount",
                "mRender": function (data, type, row) {
                    let statusText = row.hasService
                        ? '<span class="text-success fw-bold small"> <i class="fas fa-check-circle i-green"></i></span>'
                        : '<span class="i-red fw-bold small"> <i class="fa fa-times i-grey"></i></span>';

                    let tooltipText = row.hasService
                        ? 'SERVICE AVAILABLE.\n\n  Total number of current cases = ' + row.caseCount
                        : ' NO SERVICE AVAILABLE.\n\n Total number of current cases = ' + row.caseCount;
                    return `
            <span data-bs-toggle="tooltip" title="${tooltipText}">
                ${statusText}
                <span>(${data})</span>
            </span>
        `;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a id="details' + row.id + '" href="/Investigation/VendorDetail?Id=' + row.id + '&selectedcase=' + claimId + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Agency Info</a>&nbsp;'
                    return buttons;
                }
            }],
        rowCallback: function (row, data, index) {
            if (data.hasService) {
                $(row).addClass('highlight-new-user');
                setTimeout(function () {
                    $(row).removeClass('highlight-new-user');
                }, 3000);
            }
        },
        "drawCallback": function (settings, start, end, max, total, pre) {
            // Preselect the radio button matching vendorId
            var selectedVendorId = $('#vendorId').val();
            if (selectedVendorId) {
                $("input[type='radio'][name='selectedcase'][value='" + selectedVendorId + "']").prop('checked', true);
                $('#allocatedcase').prop("disabled", false);
            }
            $('#customerTable tbody').on('click', '.btn-info', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('details', ''); // Extract the ID from the button's ID attribute
                getdetails(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
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
    $('#refreshTable').click(function () {
        var $icon = $('#refreshIcon');
        if ($icon) {
            $icon.addClass('fa-spin');
        }
        table.ajax.reload(null, false);
        $("#allocatedcase").prop('disabled', true);
    });
    table.on('xhr.dt', function () {
        $('#refreshIcon').removeClass('fa-spin');
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
                        star.attr("src", "/img/FilledStar.jpeg");
                    } else if (index === Math.floor(av) && av % 1 !== 0) {  // Handle half-filled stars
                        star.attr("src", "/imgHalfStar.jpeg");  // You need a half-star image
                    } else {
                        star.attr("src", "/img/StarFade.gif");  // Faded stars
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
            var $rowResult = $(e.currentTarget).closest('tr').find('span.result');

            // Set text
            $rowResult.text(data).css({
                'color': 'red',
                'font-size': 'small'
            });

            // Set title (Fix)
            $rowResult.attr("title", data);
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
            img.attr("src", "/img/FilledStar.jpeg").prevAll("img.rating").attr("src", "/img/FilledStar.jpeg");
        }
    });

    var askConfirmation = true;
    $('#radioButtons').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault(); $.confirm({
                title: "Confirm Assign<sub>manual</sub>",
                content: "Are you sure ?",
                icon: 'fas fa-external-link-alt',
                type: 'blue',
                closeIcon: true, buttons: {
                    confirm: {
                        text: "Assign<sub>manual</sub>",
                        btnClass: 'btn-info', action: function () {
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

    $("#loadTemplate").on("click", function (e) {
        e.preventDefault();

        const $btn = $(this);
        const $icon = $btn.find("i");
        const $container = $("#reportTemplateContainer");
        const caseId = $("#caseIdHidden").val();

        // Toggle icon
        if ($container.hasClass("show")) {
            // Collapse
            $icon.removeClass("fa-minus").addClass("fa-plus");
        } else {
            // Expand
            $icon.removeClass("fa-plus").addClass("fa-minus");

            // ✅ Only load if empty
            if ($container.children().length === 0) {
                $container.html("<div class='text-center p-3'><i class='fas fa-sync fa-spin fa-2x'></i></div>");

                $.get("/Investigation/GetReportTemplate", { caseId: caseId })
                    .done(function (html) {
                        const safe = DOMPurify.sanitize(html, { RETURN_TRUSTED_TYPE: false });
                        $container.html(safe);
                    })
                    .fail(function () {
                        $container.html("<div class='alert alert-danger'>Failed to load report template.</div>");
                    });
            }
        }
    });

});
function giveRating(img, image) {
    img.attr("src", "/img/" + image).prevAll("img.rating").attr("src", "/img/" + image);
} function refilRating(img1) {
    var rt = $(img1).closest('tr').find("span.avr").text();
    var img = $(img1).closest('tr').find("img[id='" + parseInt(rt) + "']");
    img.attr("src", "/img/FilledStar.jpeg").prevAll("img.rating").attr("src", "/img/FilledStar.jpeg");
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
if (window.location.search.includes("vendorId")) {
    const url = new URL(window.location);
    url.searchParams.delete("vendorId");
    window.history.replaceState({}, document.title, url.pathname);
}
