$(document).ready(function () {
    $('#dataTable').DataTable({
        ajax: {
            url: '/SmsForCase/GetSms',
            type: 'GET',
            datatype: 'json',
            error: DataTableErrorHandler
        },
        responsive: true,
        fixedHeader: true,
        processing: true,
        paging: true,
        order: [],
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            { data: 'senderEmail' },
            { data: 'recepientEmail' },
            { data: 'status' },
            {
                data: 'message',
                className: 'text-break',
                render: function (data, type, row) {
                    if (!data) return '';
                    return `<div class="sms-message-body">${data}</div>`;
                }
            },
            {
                "data": "updated",
                "render": function (data, type, row) {
                    if (!data) return '';
                    let date = new Date(data);
                    var localDate = date.toLocaleString('en-IN', {
                        day: '2-digit',
                        month: 'short',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                        second: '2-digit',
                        hour12: true
                    });
                    return `<span title="${localDate}" data-bs-toggle="tooltip"><small><strong>${localDate}</strong></small></span>`;
                }
            }
        ],
        "drawCallback": function (settings, start, end, max, total, pre) {
            // Reinitialize Bootstrap 5 tooltips
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (el) {
                return new bootstrap.Tooltip(el, {
                    html: true,
                    sanitize: false   // ⬅⬅⬅ THIS IS THE FIX
                });
            });
        }
    });
});