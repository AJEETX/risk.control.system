$(function () {
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
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 1                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 2                      // Index of the column to style
            },
            //{
            //    className: 'max-width-column-name', // Apply the CSS class,
            //    targets: 3                      // Index of the column to style
            //},
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 4                      // Index of the column to style
            },
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 5                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 6                      // Index of the column to style
            }
            ,
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 7                      // Index of the column to style
            },
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 8                      // Index of the column to style
            },
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 9                      // Index of the column to style
            }],
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
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '" data-toggle="tooltip" title="Select Agency" />';
                    return img;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.document + '" class="profile-image doc-profile-image" data-toggle="tooltip"/>'; return img;
                }
            },
            {
                "data": "domain"
                , "mRender": function (data, type, row) {
                    var img = '';
                    for (var i = 1; i <= 5; i++) {
                        img += '<img id="' + i + '" src="/images/StarFade.gif" class="rating" vendorId="' + row.id + '"/>';
                    }

                    // Add the rate count badge
                    img += ' <span class="badge badge-light"> (' + row.rateCount + ') </span>';

                    // Calculate and display the average rating if available
                    if (row.rateCount && row.rateCount > 0) {
                        var averageRating = row.rateTotal / row.rateCount;
                        img += '<span class="avr">' + averageRating + '</span>';  // Ensure the rating is shown with two decimals
                    }

                    // Add the result span
                    img += '<br /> <span class="result"></span>';

                    // Return the domain with the appended rating images and information
                    return '<span title="' + row.name + '" data-toggle="tooltip">' + data + '</span>' + '<br /> ' + img;
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
                    return '<span title="' + row.phone + '" data-toggle="tooltip">' + data + '</span>';
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
                    return '<span title="' + row.country + '" data-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "caseCount",
                "mRender": function (data, type, row) {
                    return '<span title="Total number of current cases = ' + row.caseCount + '" data-toggle="tooltip">' + data + '</span>';
                }
            }],
        "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {
            if (aData.caseCount > 1) {
                $('td', nRow).css('background-color', '#ffa');
            }
        }, error: function (xhr, status, error) { alert('err ' + error) }
    });
    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });
    $('#customerTable tbody').hide();
    $('#customerTable tbody').fadeIn(2000);
    $('#customerTable tbody').on('mouseover', 'img.rating', function () {
        var starId = $(this).attr('id'); // Get the ID of the hovered star        
        var vendorId = $(this).attr('vendorId'); // Get the vendorId for additional logic
        giveRating($(this), "FilledStar.jpeg"); $(this).css("cursor", "pointer");
    });
    $('#customerTable tbody').on('mouseleave', 'img.rating', function () {
        giveRating($(this), "StarFade.gif"); refilRating($(this));
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
    }); var askConfirmation = true; $('#radioButtons').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault(); $.confirm({
                title: "Confirm Assign", content: "Are you sure ?",
                icon: 'fas fa-external-link-alt', type: 'red', closeIcon: true, buttons: {
                    confirm: {
                        text: "Assign <sub>manual</sub>", btnClass: 'btn-danger', action: function () {
                            askConfirmation = false;
                            $("body").addClass("submit-progress-bg");
                            // Wrap in setTimeout so the UI
                            // can update the spinners                            
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1); $('#allocatedcase').attr('disabled', 'disabled');
                            $('#allocatedcase').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Assign <sub>manual</sub>");
                            $('#radioButtons').submit(); var article = document.getElementById("article");
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
});
function giveRating(img, image) {
    img.attr("src", "/Images/" + image).prevAll("img.rating").attr("src", "/Images/" + image);
} function refilRating(img1) {
    var rt = $(img1).closest('tr').find("span.avr").text();
    var img = $(img1).closest('tr').find("img[id='" + parseInt(rt) + "']");
    img.attr("src", "/images/FilledStar.jpeg").prevAll("img.rating").attr("src", "/images/FilledStar.jpeg");
}
function showVendor(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners    
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    var editbtn = $('a#' + id + '.btn.btn-xs.btn-info')
    $('.btn.btn-xs.btn-info').attr('disabled', 'disabled');
    editbtn.html("<i class='fas fa-sync fa-spin'></i> Details");
    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}