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
            dataSrc: '',
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    window.location.href = '/Account/Login'; // Or session timeout handler
                }
            }
        },
        order: [[10, 'desc'], [11, 'desc']],
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
        "drawCallback": function (settings, start, end, max, total, pre) {
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
            const rowNode = this.node();

            // Convert to lowercase for case-insensitive comparison
            const district = data.district ? data.district.toLowerCase() : '';
            const pincodes = data.pincodes ? data.pincodes.toLowerCase() : '';

            if (district === 'all districts') {
                $(rowNode).find('td:nth-child(4)').addClass('text-light-green'); // Column index starts from 1
                $(rowNode).find('td:nth-child(5)').addClass('text-light-green'); // Column index starts from 1
                $(rowNode).find('td:nth-child(6)').addClass('text-light-green'); // Column index starts from 1
            } else {
                $(rowNode).find('td:nth-child(4)').removeClass('text-light-green');
                $(rowNode).find('td:nth-child(5)').removeClass('text-light-green');
                $(rowNode).find('td:nth-child(6)').removeClass('text-light-green');
            }

            if (data.isUpdated) {

                $(rowNode).addClass('highlight-new-user');

                setTimeout(() => {
                    $(rowNode).removeClass('highlight-new-user');
                }, 3000);
            }
        });
    });
});
