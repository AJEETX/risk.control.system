$(document).ready(function () {
    var table = $("#customerTable").DataTable({
        ajax: {
            url: '/api/ClaimsInvestigation/GetAssign',
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
                    var img = '<input class="vendors" name="claims" type="checkbox" id="' + row.id + '"  value="' + row.id + '"  />';
                    return img;
                }
            },
            {
                "data": "id", "name": "Id", "bVisible": false
            },
            { "data": "policyNum" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img src="' + row.document + '" src height="30" width="30" />';
                    return img;
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img src="' + row.customer + '" src height="30" width="30" />';
                    return img;
                }
            },
            { "data": "name" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var img = '<img src="' + row.beneficiaryPhoto + '" src height="30" width="30" />';
                    return img;
                }
            },
            { "data": "beneficiaryName" },
            { "data": "serviceType" },
            { "data": "status" },
            { "data": "location" },
            { "data": "created" },
            { "data": "timePending" },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += '<a href="ReadyDetail?Id=' + row.id + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Details</a>&nbsp;'
                    return buttons;
                }
            }
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
                title: "Claim Assignment !!!",
                content: "Please select claim to assign?",
                icon: 'fas fa-exclamation-triangle',
                columnClass: 'medium',
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
        //else if (anyChecked) {
        //    $.confirm({
        //        title: "Confirm Claim Assignment",
        //        content: "Are you sure?",
        //        icon: 'fas fa-pen-alt',
        //        type: 'green',
        //        closeIcon: true,
        //        buttons: {
        //            confirm: {
        //                text: "Assign",
        //                btnClass: 'btn-success',
        //                action: function () {
        //                    form.submit();
        //                }
        //            },
        //            cancel: {
        //                text: "Cancel",
        //                btnClass: 'btn-default'
        //            }
        //        }
        //    });
        //}
    });
});
function Delete(userId, status) {
    $.confirm({
        title: 'Change Status!',
        content: 'Do you want to Change Status!',
        buttons: {
            confirm: function () {
                $.ajax({
                    url: "/Administration/User/ChangeUserStatus",
                    type: "POST",
                    data: { UserId: userId, Status: status },
                    success: function (data, textStatus, xhr) {
                        if (data.Result == "success") {
                            location.reload();
                        }
                        if (data.Result == "failed") {
                            $.alert('Something Went Wrong');
                        }
                    },
                    error: function (xhr, status, err) {
                        if (xhr.status == 401) {
                            alert('Error');
                            window.location.href = "/Portal/Logout";
                        }
                        if (xhr.status == 500) {
                            alert('Error');
                            window.location.href = "/Portal/Logout";
                        }
                    }
                });
            },
            cancel: function () {
                $.alert('Canceled!');
            }
        }
    });
}