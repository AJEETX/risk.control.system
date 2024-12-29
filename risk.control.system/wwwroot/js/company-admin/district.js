$(document).ready(function () {
    $('#customerTable').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/District/GetDistricts',
            type: 'GET',
            dataType: 'json',
            data: function (d) {
                d.search = d.search.value; // Pass the search term
                d.orderColumn = d.order[0].column; // Column index
                d.orderDirection = d.order[0].dir; // "asc" or "desc"
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
            { data: 'state' },
            { data: 'country' },
            {
                data: 'districtId',
                render: function (data, type, row) {
                    return `
                                        <a class="btn btn-xs btn-warning" href="/District/Edit/${data}">
                                            <i class="fas fa-pen"></i> Edit
                                        </a> &nbsp;
                                        <a class="btn btn-xs btn-danger" href="/District/Delete/${data}">
                                            <i class="fas fa-trash"></i> Delete
                                        </a>`;
                }
            }
        ]
    });

    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm  Add New",

                content: "Are you sure to add?",
                icon: 'fas fa-city',
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
        if (askEditConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit",
                content: "Are you sure to edit?",

                icon: 'fas fa-city',
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