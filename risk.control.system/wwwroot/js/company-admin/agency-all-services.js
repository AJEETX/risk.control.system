$(document).ready(function () {

    function disableAllElements() {
        $('button, input[type="submit"], a').prop('disabled', true);
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });
    }

    $('body').on('click', 'a.create-agency-service', function () {
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        $(this).attr('disabled', true).html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Service");
        disableAllElements();
    });

    // Utility function to show loading progress
    function showLoadingState(element, message, spinnerClass = 'fas fa-sync fa-spin') {
        $("body").addClass("submit-progress-bg");
        setTimeout(() => {
            $(".submit-progress").removeClass("hidden");
        }, 1);

        // Disable all buttons, inputs, and links
        $('button, input[type="submit"], a').prop('disabled', true).addClass('disabled-anchor');
        $('a.disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default actions
        });

        if (element) {
            $(element).html(`<i class="${spinnerClass}"></i> ${message}`);
        }

        // Disable all elements inside an article section
        const article = document.getElementById("article");
        if (article) {
            const nodes = article.getElementsByTagName('*');
            Array.from(nodes).forEach(node => node.disabled = true);
        }
    }

    // Event delegation for actions (edit, delete, detail)
    $(document).on('click', 'a.action-btn', function (e) {
        e.preventDefault();
        const actionType = $(this).data('action');
        const id = $(this).data('id');
        let targetUrl = '';

        switch (actionType) {
            case 'details':
                showLoadingState(this, 'Detail');
                targetUrl = `/VendorService/Details?id=${id}`; // Redirect to details page
                break;

            case 'edit':
                showLoadingState(this, 'Edit');
                targetUrl = `/VendorService/Edit?id=${id}`; // Redirect to edit page
                break;

            case 'delete':
                showLoadingState(this, 'Delete');
                // Perform your delete logic here, then redirect if necessary
                targetUrl = `/VendorService/Delete?id=${id}`; // For deleting, you may want to confirm before navigating
                break;

            default:
                console.warn(`Unknown action: ${actionType}`);
        }

        // Navigate to the respective URL (edit or detail page)
        if (targetUrl) {
            window.location.href = targetUrl; // Navigate to the page
        }
    });

    // Initialize DataTable
    const table = $("#customerTable").DataTable({
        ajax: {
            url: `/api/Company/AllServices?id=${$('#vendorId').val()}`,
            dataSrc: ''
        },
        order: [[11, 'desc'], [12, 'desc']],
        columnDefs: [
            { className: 'max-width-column-name', targets: 1 },
            { className: 'max-width-column', targets: 7 },
            { className: 'max-width-column-name', targets: 8 }
        ],
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            { data: "id", visible: false },
            {
                data: "caseType",
                render: (data, type, row) => `<span title="${row.caseType}" data-toggle="tooltip">${data}</span>`
            },
            {
                data: "serviceType",
                render: (data, type, row) => `<span title="${row.serviceType}" data-toggle="tooltip">${data}</span>`
            },
            {
                data: "rate",
                render: (data, type, row) => `<span title="${row.rate}" data-toggle="tooltip">${data}</span>`
            },
            {
                data: "district",
                render: (data, type, row) => `<span title="${row.district}" data-toggle="tooltip">${data}</span>`
            },
            {
                data: "state",
                render: (data, type, row) => `<span title="${row.state}" data-toggle="tooltip">${data}</span>`
            },
            {
                data: "country",
                render: (data, type, row) => `
                    <span title="${row.country}" data-toggle="tooltip">
                        <img alt="${row.country}" title="${row.country}" src="${row.flag}" class="flag-icon" data-toggle="tooltip"/>
                        ${row.country}
                    </span>`
            },
            {
                data: "pincodes",
                render: (data, type, row) => `<span title="${row.rawPincodes}" data-toggle="tooltip">${data}</span>`
            },
            {
                data: "updatedBy",
                render: (data, type, row) => `<span title="${row.updatedBy}" data-toggle="tooltip">${data}</span>`
            },
            {
                data: "updated",
                render: (data, type, row) => `<span title="${row.updated}" data-toggle="tooltip">${data}</span>`
            },
            {
                defaultContent: '',
                orderable: false,
                render: (data, type, row) => `
                    <a href="#" data-id="${row.id}" data-action="edit" class="action-btn btn btn-xs btn-warning">
                        <i class="fas fa-pen"></i> Edit
                    </a>&nbsp;
                    <a href="#" data-id="${row.id}" data-action="delete" class="action-btn btn btn-xs btn-danger">
                        <i class="fas fa-trash"></i> Delete
                    </a>`
            },
            { data: "isUpdated", visible: false },
            { data: "lastModified", visible: false }
        ],
        drawCallback: function () {
            $('[data-toggle="tooltip"]').tooltip({
                animated: 'fade',
                placement: 'bottom',
                html: true
            });
        }
    });

    // Highlight updated rows and optionally scroll into view
    table.on('draw', function () {
        table.rows().every(function () {
            const data = this.data();
            if (data.isUpdated) {
                const rowNode = this.node();
                $(rowNode).addClass('highlight-new-user');
                rowNode.scrollIntoView({ behavior: 'smooth', block: 'center' });

                // Remove highlight after a delay
                setTimeout(() => {
                    $(rowNode).removeClass('highlight-new-user');
                }, 3000);
            }
        });
    });
});
