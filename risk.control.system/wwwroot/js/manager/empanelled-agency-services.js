var ALL_DISTRICTS = "All Districts";
$(document).ready(function () {
    $('a.create-agency-service').on('click', function () {
        $("body").addClass("submit-progress-bg");
        // Wrap in setTimeout so the UI
        // can update the spinners
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        $('a.create-agency-service').attr('disabled', 'disabled');
        $('a.create-agency-service').html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Service");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });

    var table = $("#dataTable").DataTable({
        ajax: {
            url: '/api/Company/AllServices?id=' + $('#Id').val(),
            dataSrc: '',
            error: DataTableErrorHandler
        },
        order: [[10, 'desc'], [11, 'desc']], // Sort by `isUpdated` and `lastModified`,
        columnDefs: [
            { className: 'max-width-column-number', targets: 1 },
            { className: 'max-width-column-number', targets: 2 },
            { className: 'max-width-column-picodes', targets: 4 },
            { className: 'max-width-column-number', targets: 7 },
            { className: 'max-width-column-name', targets: 8 }],
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            /* Name of the keys from
            data file source */
            {
                "data": "id", "name": "Id", "bVisible": false
            },
            {
                "data": "caseType",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.caseType + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "serviceType",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.serviceType + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "rate",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.rate + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                data: "district",
                mRender: (data, type, row) => {
                    const fullText = row.district || '';
                    if (fullText == ALL_DISTRICTS) {
                        return `<span title="${fullText}" data-toggle="tooltip"> ${fullText} </span>`;
                    } else {
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
                }
            },
            {
                "data": "stateCode",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.state + '" data-bs-toggle="tooltip">' + data + '</span>'
                }
            },
            {
                "data": "countryCode",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.country + '" data-bs-toggle="tooltip"> <img alt="' + data + '" title="' + data + '" src="' + row.flag + '" class="flag-icon" data-bs-toggle="tooltip"/>' + data + '</span>';
                }
            },
            {
                "data": "updatedBy",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.updatedBy + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "data": "updated",
                "mRender": function (data, type, row) {
                    return '<span title="' + row.updated + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                "sDefaultContent": "",
                "bSortable": false,
                "mRender": function (data, type, row) {
                    var buttons = "";
                    buttons += `<a data-id="${row.id}" class="btn btn-xs btn-warning"><i class="fas fa-edit"></i> Edit</a> &nbsp;`;
                    buttons += `
                        <button
                           class="btn btn-xs btn-danger js-delete"
                           data-id="${row.id}">
                           <i class="fa fa-trash"></i> Delete
                        </button>`;
                    return buttons;
                }
            },
            {
                "data": "isUpdated",
                bVisible: false
            },
            {
                "data": "lastModified",
                bVisible: false
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
    $('body').on('click', 'a.btn-warning', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        showedit(id, this);
    });
    function showedit(id, element) {
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        showSpinnerOnButton(element, "Edit");

        const url = `/EmpanelledAgencyService/Edit?id=${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = url;
        }, 1000);
    }
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }

    $('#dataTable').on('click', '.js-delete', function (e) {
        e.preventDefault();
        var $spinner = $(".submit-progress"); // global spinner (you already have this)
        var $btn = $(this);
        const id = $btn.data('id');
        const token = $('input[name="__RequestVerificationToken"]').val();

        $.confirm({
            title: 'Confirm Delete',
            content: 'Are you sure you want to delete this service?',
            type: 'red',
            icon: 'fas fa-trash',
            buttons: {
                confirm: {
                    text: 'Yes, Delete',
                    btnClass: 'btn-red',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Delete');
                        $.ajax({
                            url: '/EmpanelledAgencyService/DeleteService',
                            type: 'POST',
                            data: {
                                __RequestVerificationToken: token,
                                id: id
                            },
                            success: function (response) {
                                if (response.success) {
                                    $.alert({
                                        title: 'Deleted!',
                                        content: response.message,
                                        closeIcon: true,
                                        type: 'red',
                                        icon: 'fas fa-trash',
                                        buttons: {
                                            ok: {
                                                text: 'Close',
                                                btnClass: 'btn-default',
                                            }
                                        }
                                    });
                                    $('#dataTable').DataTable().ajax.reload(null, false);
                                } else {
                                    toastr.error(response.message || 'Delete failed');
                                }
                            },
                            error: function (xhr) {
                                $.alert({
                                    title: 'Error!',
                                    content: 'Failed to delete the service.',
                                    type: 'red'
                                });
                                if (xhr.status === 401 || xhr.status === 403) {
                                    window.location.href = '/Account/Login';
                                } else {
                                    toastr.error('Unexpected error occurred');
                                }
                            },
                            complete: function () {
                                $spinner.addClass("hidden");
                                // ✅ Re-enable button and restore text
                                $btn.prop("disabled", false).html('<i class="fas fa-trash"></i> Delete');
                            }
                        });
                    }
                },
                cancel: {
                    text: 'Cancel',
                }
            }
        });
    });
    table.on('draw', function () {
        table.rows().every(function () {
            const data = this.data();
            const rowNode = this.node();

            // Convert to lowercase for case-insensitive comparison
            const district = data.district ? data.district : '';

            if (district === ALL_DISTRICTS) {
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

    $('#dataTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'bottom',
            html: true
        });
    });
});