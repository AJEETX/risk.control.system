$(document).ready(function () {

    $('#customerTable').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/Pincodes/GetPincodes',
            type: 'GET',
            dataType: 'json',
            data: function (d) {
                d.search = d.search.value; // Pass the search term
                d.orderColumn = d.order[0].column; // Column index (0, 1, 2, etc.)
                d.orderDirection = d.order[0].dir; // Sorting direction ("asc" or "desc")
            }
        },
        order: [[0, 'asc']],
        fixedHeader: true,
        processing: true,
        paging: true,

        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            { data: 'code' },
            { data: 'name' },
            { data: 'district' },
            { data: 'state' },
            { data: 'country' },
            {
                data: 'pinCodeId',
                render: function (data, type, row) {
                    return `
                                <a class="btn btn-xs btn-warning" href="/Pincodes/Edit/${data}">
                                    <i class="fas fa-pen"></i> Edit
                                </a> &nbsp;
                                <a class="btn btn-xs btn-danger" href="/Pincodes/Delete/${data}">
                                    <i class="fas fa-trash"></i> Delete
                                </a>`;
                }
            }
        ]
    });

    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if ($('#create-form').valid() && askConfirmation) { // Ensure `valid` is called as a method
            e.preventDefault();
            $.confirm({
                title: "Confirm  Add New",
                content: "Are you sure to add?",

                icon: 'fas fa-map-pin',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: " Add New",
                        btnClass: 'btn-success',
                        action: function () {
                            askConfirmation = false;
                            $('#create-form').submit();
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

    var askEditConfirmation = true;
    $('#edit-form').submit(function (e) {
        if ($('#edit-form').valid() && askEditConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit",
                content: "Are you sure to edit?",
                icon: 'fas fa-map-pin',

                type: 'orange',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Edit ",
                        btnClass: 'btn-warning',
                        action: function () {
                            askEditConfirmation = false;
                            $('#edit-form').submit();
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

var country = $('#CountryId');
if (country) {
    country.focus();
}