$(document).ready(function () {
    var table = $('#customerTable').DataTable({
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
        order: [[4, 'desc'], [2, 'desc']],
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
                data: 'lastModified',
                orderable: true, // Enable sorting for this column
                render: function (data, type, row) {
                    if (data) {
                        const date = new Date(data);
                        return date.toLocaleDateString('en-AU');
                    }
                    return 'N/A';
                }
            },
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
            },
            {
                "data": "isUpdated",
                bVisible: false
            }
        ],
        rowCallback: function (row, data, index) {
            // Highlight rows updated in the last 24 hours
            if (data.lastModified) {
                const lastModified = new Date(data.lastModified);
                const now = new Date();
                const timeDifference = now - lastModified;

                // Check if the difference is less than 24 hours
                if (timeDifference < 24 * 60 * 60 * 1000) {
                    //$(row).addClass('highlighted-row'); // Apply a custom CSS class
                }
            }
        }
    });

    table.on('draw', function () {
        table.rows().every(function () {
            var data = this.data(); // Get row data
            console.log(data); // Debug row data

            if (data.lastModified) { // Check if the row should be highlighted
                var rowNode = this.node();
                const lastModified = new Date(data.lastModified);
                const now = new Date();
                const timeDifference = now - lastModified;
                if (timeDifference < 24 * 60 * 60 * 1000) {
                    // Highlight the row
                    $(rowNode).addClass('highlight-new-user');

                    // Scroll the row into view
                    rowNode.scrollIntoView({ behavior: 'smooth', block: 'center' });

                    // Optionally, remove the highlight after a delay
                    setTimeout(function () {
                        $(rowNode).removeClass('highlight-new-user');
                    }, 3000);
                }
            }
        });
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
                            // Disable all buttons, submit inputs, and anchors
                            $('button, input[type="submit"], a').prop('disabled', true);

                            // Add a class to visually indicate disabled state for anchors
                            $('a').addClass('disabled-anchor').on('click', function (e) {
                                e.preventDefault(); // Prevent default action for anchor clicks
                            });
                            $('#create-form').submit();
                            var form = document.getElementById("create-form");
                            if (form) {
                                const formElements = form.getElementsByTagName("*");
                                for (const element of formElements) {
                                    element.disabled = true;
                                    if (element.hasAttribute("readonly")) {
                                        element.classList.remove("valid", "is-valid", "valid-border");
                                        element.removeAttribute("aria-invalid");
                                    }
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
    })

    var askEditConfirmation = true;
    $('#edit-form').submit(function (e) {
        if (askEditConfirmation) {
            e.preventDefault();
            $.confirm({
                title: "Confirm Edit",
                content: "Are you sure to edit?",
                icon: 'fas fa-map-marked-alt',
                type: 'orange',
                closeIcon: true,

                buttons: {
                    confirm: {
                        text: "Edit",
                        btnClass: 'btn-warning',
                        action: function () {
                            askEditConfirmation = false;
                            // Disable all buttons, submit inputs, and anchors
                            $('button, input[type="submit"], a').prop('disabled', true);

                            // Add a class to visually indicate disabled state for anchors
                            $('a').addClass('disabled-anchor').on('click', function (e) {
                                e.preventDefault(); // Prevent default action for anchor clicks
                            });
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
$('#Name').focus();