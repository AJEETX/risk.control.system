$(document).ready(function () {
    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/Company/GetEmpanelledVendors',
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
                return '<input type="checkbox" name="id[]" value="' + $('<div/>').text(data).html() + '">';
            }
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
                className: 'max-width-column', // Apply the CSS class,
                targets: 4                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 9                      // Index of the column to style
            }],
        order: [[11, 'desc'], [12, 'desc']], // Sort by `isUpdated` and `lastModified`,
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
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<input title="select to depanel" data-bs-toggle="tooltip" class="vendors" name="vendors" type="checkbox" id="' + row.id + '"  value="' + row.id + '"  />';
                    return img;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.document + '" class="profile-image doc-profile-image" data-bs-toggle="tooltip"/>';
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
                    img += '<br /> <span class="result"></span>';

                    // Return the domain with the appended rating images and information
                    return '<span title="' + row.name + '" data-bs-toggle="tooltip"><a id="edit' + row.id + '" href="/Company/AgencyDetail?Id=' + row.id + '"> ' + data +'</a></span>' + '<br /> ' + img;
                }
            },
            //{
            //    "data": "name",
            //    "mRender": function (data, type, row) {
            //        return '<span title="' + row.name + '" data-bs-toggle="tooltip">' + data + '</span>'
            //    }
            //},
            {
                "data": "phone",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-bs-toggle="tooltip"/>' + data + '</span>'
                }
            },
            {
                "data": "address",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "district",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "stateCode",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.state + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "countryCode",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.country + '" data-bs-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-bs-toggle="tooltip"/>' + data + '</span>';
                }
            },
            {
                "data": "caseCount",
                "mRender": function (data, type, row) {
                    return '<span title="Total number of current cases = ' + row.caseCount + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "updated",
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "updateBy",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.updateBy + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "isUpdated",
                "bVisible": false
            },
            {
                "data": "lastModified",
                bVisible: false
            }
        ],
        "drawCallback": function (settings, start, end, max, total, pre) {
            var rowCount = (this.fnSettings().fnRecordsTotal()); // total number of rows
            if (rowCount > 0) {
                $('#empanel-vendors').prop('disabled', false);
            }
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

    table.on('draw', function () {
        table.rows().every(function () {
            var data = this.data(); // Get row data
            console.log(data); // Debug row data

            if (data.isUpdated) { // Check if the row should be highlighted
                var rowNode = this.node();

                // Highlight the row
                $(rowNode).addClass('highlight-new-user');

                // Optionally, remove the highlight after a delay
                setTimeout(function () {
                    $(rowNode).removeClass('highlight-new-user');
                }, 3000);
            }
        });
    });
    table.on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'top',
            html: true
        });
    });
    
    // Handle click on "Select all" control
    $('#checkall').on('click', function () {
        // Get all rows with search applied
        var rows = table.rows({ 'search': 'applied' }).nodes();
        // Check/uncheck checkboxes for all rows in the table
        $('input[type="checkbox"]', rows).prop('checked', this.checked);
    });

    // Handle click on checkbox to set state of "Select all" control
    $('#customerTable tbody').on('change', 'input[type="checkbox"]', function () {
        // If checkbox is not checked
        if (!this.checked) {
            var el = $('#checkall').get(0);
            // If "Select all" control is checked and has 'indeterminate' property
            if (el && el.checked && ('indeterminate' in el)) {
                // Set visual state of "Select all" control
                // as 'indeterminate'
                el.indeterminate = true;
            }
        }
    });

    // Handle form submission event
    let askConfirmation = false;
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

        var checkboxes = $("input[type='checkbox'].vendors");
        var anyChecked = checkIfAnyChecked(checkboxes);

        if (!anyChecked) {
            e.preventDefault();
            $.alert({
                title: "Depanel Agency !!!",
                content: "Please select agency to depanel?",
                icon: 'fas fa-exclamation-triangle',
    
                type: 'red',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "SELECT",
                        btnClass: 'btn-danger'
                    }
                }
            });
        }
        else if (anyChecked && !askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Agency Depanel",
                content: "Are you sure?",
                icon: 'fas fa-hand-pointer',
    
                type: 'red',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Submit",
                        btnClass: 'btn-danger',
                        action: function () {
                            askConfirmation = true;
                            $("body").addClass("submit-progress-bg");
                            setTimeout(function () {
                                $(".submit-progress").removeClass("hidden");
                            }, 1);

                            $(this).attr('disabled', 'disabled');
                            $(this).html("<i class='fas fa-sync fa-spin'></i> Submit");

                            $('#checkboxes').submit();
                            $('html *').css('cursor', 'not-allowed');
                            $('html a *, html button *').attr('disabled', 'disabled');
                            $('html a *, html button *').css('pointer-events', 'none')
                            $('#empanel-vendors').attr('disabled', 'disabled');
                            $('#empanel-vendors').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Depanel");

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
    });
});
function checkIfAllChecked(elements) {
    var totalElmentCount = elements.length;
    var totalCheckedElements = elements.filter(":checked").length;
    return (totalElmentCount == totalCheckedElements)
}

function checkIfAnyChecked(elements) {
    var hasAnyCheckboxChecked = false;

    $.each(elements, function (index, element) {
        if (element.checked === true) {
            hasAnyCheckboxChecked = true;
        }
    });
    return hasAnyCheckboxChecked;
}