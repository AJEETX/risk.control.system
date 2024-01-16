$(document).ready(function () {
    $('#view-type a').on('click', function () {
        var id = this.id;
        if (this.id == 'map-type') {
            $('#checkboxes').css('display', 'none');
            $('#maps').css('display', 'block');
            $('#map-type').css('display', 'none');
            $('#list-type').css('display', 'block');
        }
        else {
            $('#checkboxes').css('display', 'block');
            $('#maps').css('display', 'none');
            $('#map-type').css('display', 'block');
            $('#list-type').css('display', 'none');
        }
    });

    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/ClaimsVendor/GetNew',
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
        }],
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
                    var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '"  />';
                    return img;
                }
            },
            {
                "data": "id", "name": "Id", "bVisible": false
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.policyId + '" title="' + row.policyId + '" src="' + row.document + '" class="doc-profile-image" />';
                    return img;
                }
            },
            { "data": "policyNum", "bSortable": false },
            {
                "data": "amount"
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.customer + '" class="table-profile-image" />';
                    return img;
                }
            },
            { "data": "name" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.beneficiaryName + '" title="' + row.beneficiaryName + '" src="' + row.beneficiaryPhoto + '" class="table-profile-image" />';
                    return img;
                }
            },
            { "data": "beneficiaryName" },
            { "data": "serviceType" },
            { "data": "service" },
            { "data": "pincode" },
            { "data": "location" },
            { "data": "created" },
            { "data": "timePending" },
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });

    if ($("input[type='radio'].selected-case:checked").length) {
        $("#allocatedcase").prop('disabled', false);
    }
    else {
        $("#allocatedcase").prop('disabled', true);
    }

    // When user checks a radio button, Enable submit button
    $("input[type='radio'].selected-case").change(function (e) {
        if ($(this).is(":checked")) {
            $("#allocatedcase").prop('disabled', false);
        }
        else {
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
    initMap("/api/ClaimsVendor/GetNewMap");
});