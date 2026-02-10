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
            error: function (xhr, status, error) {
                console.error("AJAX Error:", status, error);
                console.error("Response:", xhr.responseText);
                if (xhr.status === 401 || xhr.status === 403) {
                    $.confirm({
                        title: 'Session Expired!',
                        content: 'Your session has expired or you are unauthorized. You will be redirected to the login page.',
                        type: 'red',
                        typeAnimated: true,
                        buttons: {
                            Ok: {
                                text: 'Login',
                                btnClass: 'btn-red',
                                action: function () {
                                    window.location.href = '/Account/Login';
                                }
                            }
                        },
                        onClose: function () {
                            window.location.href = '/Account/Login';
                        }
                    });
                }
                else if (xhr.status === 400) {
                    $.confirm({
                        title: 'Bad Request!',
                        content: 'Try with valid data.You will be redirected to Dashboard page',
                        type: 'orange',
                        typeAnimated: true,
                        buttons: {
                            Ok: function () {
                                window.location.href = '/DashBoard/Index';
                            }
                        },
                        onClose: function () {
                            window.location.href = '/DashBoard/Index';
                        }
                    });
                }
                else {
                    $.confirm({
                        title: 'Server Error!',
                        content: 'An unexpected server error occurred. You will be redirected to Dashboard page.',
                        type: 'orange',
                        typeAnimated: true,
                        buttons: {
                            Ok: function () {
                                window.location.href = '/DashBoard/Index';
                            }
                        },
                        onClose: function () {
                            window.location.href = '/DashBoard/Index';
                        }
                    });
                }
            }
        },
        order: [[3, "desc"]],
        fixedHeader: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            { data: 'userId' },
            { data: 'type' },
            { data: 'tableName' },
            {
                data: 'dateTime',
                render: function (data) {
                    return data ? new Date(data).toLocaleString() : '';
                }
            },
            {
                data: 'oldValues', // or newValues
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
                        : `<small>${encoded}</small>`;
                }
            },
            {
                data: 'newValues',
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
                        : `<small>${encoded}</small>`;
                }
            },
            {
                data: 'id',
                orderable: false,
                searchable: false,
                render: function (data) {
                    return '<a id=details' + data + ' href="/Audit/Details?Id=' + data + '" class="btn btn-xs btn-info"><i class="fa fa-search"></i> Detail</a>'
                }
            }
        ],
        "drawCallback": function (settings, start, end, max, total, pre) {
            $('#dataTable tbody').on('click', '.btn-info', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('details', ''); // Extract the ID from the button's ID attribute
                getaudit(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
        }
    });
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