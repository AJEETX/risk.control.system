$(document).ready(function () {
    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/Company/GetAvailableVendors',
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
        }],
        order: [[1, 'asc']],
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
                    var img = '<input class="vendors" name="vendors" type="checkbox" id="' + row.id + '"  value="' + row.id + '"  />';
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
                    var img = '<img alt="' + row.name + '" title="' + row.name + '" src="' + row.document + '" class="doc-profile-image" />';
                    return img;
                }
            },
            { "data": "domain" },
            { "data": "name" },
            { "data": "code" },
            { "data": "phone" },
            { "data": "address" },
            { "data": "district" },
            { "data": "state" },
            { "data": "country" }
        ],
        error: function (xhr, status, error) { alert('err ' + error) }
    });
    // Handle click on "Select all" control
    $('#checkall').on('click', function () {
        // Get all rows with search applied
        var rows = table.rows({ 'search': 'applied' }).nodes();
        // Check/uncheck checkboxes for all rows in the table
        $('input[type="checkbox"]', rows).prop('checked', this.checked);
    });
    $('input.vendors').on('click', function () {
        var checkboxes = $("input[type='checkbox'].vendors");
        var anyChecked = checkIfAnyChecked(checkboxes);
        var allChecked = checkIfAllChecked(checkboxes);
        $('#checkall').prop('checked', allChecked);
        $('#manage-vendors').prop('disabled', !anyChecked)
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

        var checkboxes = $("input[type='checkbox'].vendors");
        var anyChecked = checkIfAnyChecked(checkboxes);
        if (!anyChecked) {
            e.preventDefault();
            $.alert({
                title: "Agency Empanelment !!!",
                content: "Please select agency to empanel?",
                icon: 'fas fa-exclamation-triangle',
    
                type: 'green',
                closeIcon: true,
                buttons: {
                    cancel: {
                        text: "SELECT",
                        btnClass: 'btn-success'
                    }
                }
            });
        }
        else if (anyChecked && !askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Agency Empanel",
                content: "Are you sure?",
                icon: 'fas fa-handshake',
    
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Submit",
                        btnClass: 'btn-success',
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
                            $('#manage-vendors').attr('disabled', 'disabled');
                            $('#manage-vendors').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Empanel");

                            var nodes = document.getElementById("article").getElementsByTagName('*');
                            for (var i = 0; i < nodes.length; i++) {
                                nodes[i].disabled = true;
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