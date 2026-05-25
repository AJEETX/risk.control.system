$(document).ready(function () {
    var table = $("#invoiceTable").DataTable({
        "processing": true,
        "serverSide": true,
        "filter": true,
        "ajax": {
            "url": "VendorInvoice/GetInvoicesJson",
            "type": "POST",
            "datatype": "json",
            "data": function (d) {
                // Read the selection choice live and bind it to the POST payload
                d.selectedVendorId = $("#VendorFilterDropdown").val();
            }
        },
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        "columns": [
            { "data": "invoiceNumber", "name": "InvoiceNumber" },
            { "data": "invoiceDate", "name": "InvoiceDate" },
            { "data": "dueDate", "name": "DueDate" },
            { "data": "vendorName", "name": "VendorName" }, // Enabled sorting for vendors
            { "data": "clientName", "name": "ClientName" },  // Enabled sorting for clients
            {
                "data": "grandTotal",
                "name": "GrandTotal",
                "render": function (data, type, row) {
                    let currency = row.currency ? row.currency : '$';
                    return currency + ' ' + parseFloat(data).toFixed(2);
                }
            },
            {
                "data": "vendorInvoiceId",
                "orderable": false,
                "render": function (data) {
                    return `
                        <a data-id="${data}" class="btn btn-xs btn-info">
                            <i class="fas fa-search"></i> Detail
                        </a>
                        `;
                }
            }
        ],
        "order": [[0, "desc"]]
    });

    // Watch the dropdown component for client choices and redraw table accordingly
    $("#VendorFilterDropdown").on("change", function () {
        table.draw();
    });

    $('body').on('click', 'a.btn-xs.btn-info', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        showdetail(id, this);
    });

    function showdetail(id, element) {
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        showSpinnerOnButton(element, "Detail");

        const url = `/VendorInvoice/Details/${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = url;
        }, 1000);
    }

    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }
});