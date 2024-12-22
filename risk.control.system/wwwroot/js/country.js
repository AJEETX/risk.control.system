$(document).ready(function () {
    $('#customerTable').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/Country/GetCountries',
            type: 'GET',
            dataType: 'json',
            data: function (d) {
                d.search = d.search.value; // Pass search term to the server
                d.orderColumn = d.order[0]?.column; // Pass the column index being sorted
                d.orderDirection = d.order[0]?.dir; // Pass the sorting directio
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
            { data: 'code', orderable: true }, // Make sortable
            { data: 'name', orderable: true }, // Make sortable
            {
                data: 'countryId',
                orderable: false, // Disable sorting for actions column
                render: function (data, type, row) {
                    return `
                                <a class="btn btn-xs btn-warning" href="/Country/Edit/${data}">
                                    <i class="fas fa-map-marker-alt"></i> Edit
                                </a>
                                &nbsp;
                                <a class="btn btn-xs btn-danger" href="/Country/Delete/${data}">
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
                title: "Confirm Add New",
                content: "Are you sure to add?",
    
                icon: 'fas fa-map-marked-alt',
                type: 'green',
                closeIcon: true,
                buttons: {
                    confirm: {
                        text: "Add New",
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
    })
});