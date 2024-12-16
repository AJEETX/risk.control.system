$(document).ready(function () {
    $('#allocatedcase').on('click', function (event) {
        $("body").addClass("submit-progress-bg");

        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('#allocatedcase').attr('disabled', 'disabled');
        $('#allocatedcase').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Allocate <sub>new</sub>");

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
        $('#investigatecase').attr('disabled', 'disabled');
        $('#investigatecase').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Investigate");

        $('#radioButtons').submit();
        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    $('#view-type a').on('click', function () {
        var id = this.id;
        if (this.id == 'map-type') {
            $('#radioButtons').css('display', 'none');
            $('#maps').css('display', 'block');
            $('#map-type').css('display', 'none');
            $('#list-type').css('display', 'block');
        }
        else {
            $('#radioButtons').css('display', 'block');
            $('#maps').css('display', 'none');
            $('#map-type').css('display', 'block');
            $('#list-type').css('display', 'none');
        }
    });
    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/agency/supervisor/GetNew',
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
                targets: 2                      // Index of the column to style
            },
            {
                className: 'max-width-column-number', // Apply the CSS class,
                targets: 3                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 5                      // Index of the column to style
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

                        var img = '<input name="selectedcase" class="selected-case" type="radio" id="' + row.id + '"  value="' + row.id + '"  data-toggle="tooltip" title="Allocate" />';
                        return img;
                    }
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.policyId + '" title="' + row.policyId + '" src="' + row.document + '" class="profile-image doc-profile-image" data-toggle="tooltip"/>';
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
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.customer + '"class="table-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },
            { "data": "name" },
            
            { "data": "serviceType" },
            { "data": "service" },
            {
                "data": "pincode",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.pincodeName + '" data-toggle="tooltip">' + data + '</span>'
                }
            },
            { "data": "location" },
            { "data": "created" },
            { "data": "timePending" },
            {
                "data": "company",
                "mRender": function (data, type, row) {
                    var img = '<img alt="' + row.company + '" title="' + row.company + '" src="' + row.ownerDetail + '" class="profile-image doc-profile-image" data-toggle="tooltip"/>';
                    return img;
                }
            },

            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    if (row.isQueryCase) {
                        buttons += '<a id="details' + row.id + '" onclick="showenquiry(`' + row.id + '`)" href="/SuperVisor/ReplyEnquiry?Id=' + row.id + '"  class="btn btn-xs btn-warning"><i class="fas fa-question" aria-hidden="true"></i> ENQUIRY </a>'
                    }
                    else {
                        buttons += '<a id="details' + row.id + '" onclick="showdetails(`' + row.id + '`)" href="/SuperVisor/CaseDetail?Id=' + row.id + '"  class="btn btn-xs btn-danger"><i class="fas fa-undo"></i> Decline</a>'
                    }
                    return buttons;
                }
            },
            { "data":"timeElapsed","bVisible":false}
        ],
        "fnRowCallback": function (nRow, aData, iDisplayIndex, iDisplayIndexFull) {
            if (aData.isNewAssigned) {
                $('td', nRow).css('background-color', '#ffa');
            }
        },
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    $('#customerTable tbody').hide();
    $('#customerTable tbody').fadeIn(2000);
    $('#customerTable').on('draw.dt', function () {
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
    //initMap("/api/ClaimsVendor/GetNewMap");

});

function showdetails(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    $('a.btn *').attr('disabled', 'disabled');
    $('a#details' + id + '.btn.btn-xs.btn-danger').html("<i class='fas fa-sync fa-spin'></i> Decline");

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
    $('a.btn *').attr('disabled', 'disabled');
    $('a#details' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> ENQUIRY");

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}