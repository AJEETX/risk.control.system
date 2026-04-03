$(document).ready(function () {
    $('#dataTable').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/Audit/GetAudit', // works if [HttpGet] without custom route
            type: 'GET',
            data: function (d) {
                d.search = d.search.value; // Pass the search term
                d.orderColumn = d.order[0].column; // Column index
                d.orderDirection = d.order[0].dir; // "asc" or "desc"
            },
            error: DataTableErrorHandler
        },
        order: [[3, "desc"]],
        fixedHeader: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columnDefs: [
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 0                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 2                      // Index of the column to style
            },
            {
                className: 'max-width-column-name', // Apply the CSS class,
                targets: 3                      // Index of the column to style
            }],
        columns: [
            {
                data: 'userId',
                render: function (data) {
                    return `<span title=${data} data-bs-toggle="tooltip">${data} </span>`
                }
            },
            {
                data: 'type',
                render: function (data) {
                    return `<span title=${data} data-bs-toggle="tooltip">${data} </span>`
                }
            },
            {
                data: 'tableName',
                render: function (data) {
                    return `<span title=${data} data-bs-toggle="tooltip">${data} </span>`
                }
            },
            {
                data: 'dateTime',
                render: function (data) {
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
                    return `<span title="Updated time: ${localDate}" data-bs-toggle="tooltip"><small><strong>${localDate}</strong></small></span>`;
                }
            },
            {
                data: 'oldValues', // or newValues
                orderable: false,
                render: function (data, type, row) {
                    if (!data) return '';

                    let display = '';

                    try {
                        const obj = typeof data === 'string' ? JSON.parse(data) : data;

                        display = Object.entries(obj)
                            .map(([k, v]) => `${k}: ${v}`)
                            .join(', ');
                    } catch {
                        display = data.toString();
                    }

                    const encoded = $('<div/>').text(display).html();

                    return display.length > 50
                        ? `<div title="${encoded}"><small>${encoded.substring(0, 50)}...</small></div>`
                        : `<div data-bs-toggle="tooltip" title="${encoded}"><small>${encoded}</small></div>`;
                }
            },
            {
                data: 'newValues',
                orderable: false,
                render: function (data) {
                    if (!data || data == undefined) return '';

                    // try parse JSON
                    let display = '';
                    try {
                        let obj = JSON.parse(data);
                        // convert to "key: value" pairs without {}
                        display = Object.entries(obj)
                            .map(([k, v]) => `${k}: ${v}`)
                            .join(', ');
                    } catch {
                        display = data;
                    }

                    let encoded = $('<div/>').text(display).html();

                    return data.length > 50
                        ? `<div title="${encoded}"><small>${data.substring(0, 50)}...</small></div>`
                        : `<div data-bs-toggle="tooltip" title="${encoded}"><small>${encoded}</small></div>`;
                }
            },
            {
                data: 'id',
                orderable: false,
                searchable: false,
                render: function (data) {
                    return `<a data-id="${data}" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>&nbsp;`
                }
            }
        ],
        "drawCallback": function (settings, start, end, max, total, pre) {
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (el) {
                return new bootstrap.Tooltip(el, {
                    html: true,
                    sanitize: false   // ⬅⬅⬅ THIS IS THE FIX
                });
            });
        }
    });
    $('body').on('click', 'a.btn-info', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        showdetail(id, this);
    });
    function showdetail(id, element) {
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        showSpinnerOnButton(element, "Detail");

        const editUrl = `/Audit/Detail/${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = editUrl;
        }, 1000);
    }
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }
});
function getaudit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);
    // Disable all buttons, submit inputs, and anchors
    $('button, input[type="submit"], a').prop('disabled', true);

    // Add a class to visually indicate disabled state for anchors
    $('a').addClass('disabled-anchor').on('click', function (e) {
        e.preventDefault(); // Prevent default action for anchor clicks
    });

    $('a#details' + id + '.btn.btn-xs.btn-info').html("<i class='fas fa-sync fa-spin'></i> Detail");
    var article = document.getElementById("datatable");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}