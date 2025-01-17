$(document).ready(function () {
    // Utility to disable all buttons, links, and inputs
    function disableAllElements() {
        $('button, input[type="submit"], a').prop('disabled', true);
        $('a').addClass('disabled-anchor').on('click', function (e) {
            e.preventDefault(); // Prevent default action for anchor clicks
        });
    }

    // Utility to show a spinner on a specific button
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }

    // Function to handle the Edit button
    function showedit(id) {
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);
        disableAllElements();
        showSpinnerOnButton(`a#edit${id}.btn.btn-warning`, "Edit");

        // Navigate after showing spinner
        const editUrl = `/Agency/EditService?id=${id}`;
        setTimeout(() => {
            window.location.href = editUrl;
        }, 1000);
    }

    // Function to handle the Delete button
    function getdetails(id) {
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);
        disableAllElements();
        showSpinnerOnButton(`a#delete${id}.btn.btn-danger`, "Delete");

        // Navigate after showing spinner
        const editUrl = `/Agency/DeleteService?id=${id}`;
        setTimeout(() => {
            window.location.href = editUrl;
        }, 1000);
    }

    // Event delegation for dynamically generated Edit and Delete buttons
    $('body').on('click', 'a.btn-warning', function (e) {
        e.preventDefault();
        const editUrl = $(this).attr('href');
        const id = new URL(editUrl, window.location.origin).searchParams.get("id");
        showedit(id);
    });

    $('body').on('click', 'a.btn-danger', function (e) {
        e.preventDefault();
        const deleteUrl = $(this).attr('href');
        const id = new URL(deleteUrl, window.location.origin).searchParams.get("id");
        getdetails(id);
    });

    // Event handler for Add Service button
    $('body').on('click', 'a.create-agency-service', function () {
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        $(this).attr('disabled', true).html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Service");
        disableAllElements();
    });

    // Initialize DataTable with enhanced configurations
    const table = $("#customerTable").DataTable({
        ajax: { url: '/api/Agency/AllServices', dataSrc: '' },
        order: [[11, 'desc'], [12, 'desc']],
        columnDefs: [
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
            { data: "id", name: "Id", bVisible: false },
            { data: "caseType", mRender: (data, type, row) => `<span title="${row.caseType}" data-toggle="tooltip">${data}</span>` },
            { data: "serviceType", mRender: (data, type, row) => `<span title="${row.serviceType}" data-toggle="tooltip">${data}</span>` },
            { data: "rate", mRender: (data, type, row) => `<span title="${row.rate}" data-toggle="tooltip">${data}</span>` },
            { data: "district", mRender: (data, type, row) => `<span title="${row.district}" data-toggle="tooltip">${data}</span>` },
            { data: "state", mRender: (data, type, row) => `<span title="${row.state}" data-toggle="tooltip">${data}</span>` },
            { data: "country", mRender: (data, type, row) => `<span title="${row.country}" data-toggle="tooltip"> <img alt="${data}" title="${data}" src="${row.flag}" class="flag-icon" />(${row.country})</span>` },
            { data: "pincodes", mRender: (data, type, row) => `<span title="${row.rawPincodes}" data-toggle="tooltip">${data}</span>` },
            { data: "updatedBy", mRender: (data, type, row) => `<span title="${row.updatedBy}" data-toggle="tooltip">${data}</span>` },
            { data: "updated", mRender: (data, type, row) => `<span title="${row.updated}" data-toggle="tooltip">${data}</span>` },
            {
                sDefaultContent: "",
                bSortable: false,
                mRender: function (data, type, row) {
                    return `
                        <a id="edit${row.id}" href="/Agency/EditService?id=${row.id}" class="btn btn-xs btn-warning"><i class="fas fa-pen"></i> Edit</a>&nbsp;
                        <a id="delete${row.id}" href="/Agency/DeleteService?id=${row.id}" class="btn btn-xs btn-danger"><i class="fas fa-trash"></i> Delete</a>`;
                }
            },
            { data: "isUpdated", bVisible: false },
            { data: "lastModified", bVisible: false }
        ],
        error: function (xhr, status, error) {
            alert(`Error: ${error}`);
        }
    });

    // Highlight rows based on `isUpdated` flag
    table.on('draw', function () {
        table.rows().every(function () {
            const data = this.data();
            const rowNode = this.node();

            // Convert to lowercase for case-insensitive comparison
            const district = data.district ? data.district.toLowerCase() : '';
            const pincodes = data.pincodes ? data.pincodes.toLowerCase() : '';

            if (district === 'all districts' && pincodes === 'all pincodes') {
                $(rowNode).find('td:nth-child(4)').addClass('text-light-green'); // Column index starts from 1
                $(rowNode).find('td:nth-child(5)').addClass('text-light-green'); // Column index starts from 1
                $(rowNode).find('td:nth-child(6)').addClass('text-light-green'); // Column index starts from 1
                $(rowNode).find('td:nth-child(7)').addClass('text-light-green'); // Column index starts from 1
            } else {
                $(rowNode).find('td:nth-child(4)').removeClass('text-light-green');
                $(rowNode).find('td:nth-child(5)').removeClass('text-light-green');
                $(rowNode).find('td:nth-child(6)').removeClass('text-light-green');
                $(rowNode).find('td:nth-child(7)').removeClass('text-light-green');
            }

            // Apply text color for `pincodes` column
            if (district != 'all districts' && pincodes === 'all pincodes') {
                $(rowNode).find('td:nth-child(4)').addClass('text-light-blue');
                $(rowNode).find('td:nth-child(5)').addClass('text-light-blue');
                $(rowNode).find('td:nth-child(6)').addClass('text-light-blue');
                $(rowNode).find('td:nth-child(7)').addClass('text-light-blue');
            } else {
                $(rowNode).find('td:nth-child(4)').removeClass('text-light-blue');
                $(rowNode).find('td:nth-child(5)').removeClass('text-light-blue');
                $(rowNode).find('td:nth-child(6)').removeClass('text-light-blue');
                $(rowNode).find('td:nth-child(7)').removeClass('text-light-blue');
            }

            if (data.isUpdated) {

                $(rowNode).addClass('highlight-new-user');
                rowNode.scrollIntoView({ behavior: 'smooth', block: 'center' });

                setTimeout(() => {
                    $(rowNode).removeClass('highlight-new-user');
                }, 3000);
            }
        });
    });

    // Initialize tooltips after each DataTable draw
    $('#customerTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({ animated: 'fade', placement: 'bottom', html: true });
    });
});
