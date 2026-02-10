var ALL_DISTRICTS = "All Districts";
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
    function showedit(id, element) {
        // Sanitize the ID to prevent scanner warnings
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");

        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);
        disableAllElements();

        showSpinnerOnButton(element, "Edit");

        const editUrl = `/AgencyService/Edit?id=${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = editUrl;
        }, 1000);
    }

    // Event delegation for dynamically generated Edit and Delete buttons
    $('body').on('click', 'a.btn-warning', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        showedit(id, this);
    });

    // Event handler for Add Service button
    $('body').on('click', 'a.create-agency-service', function () {
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        $(this).attr('disabled', true).html("<i class='fas fa-sync fa-spin' aria-hidden='true'></i> Add Service");
        disableAllElements();
    });

    // Initialize DataTable with enhanced configurations
    const table = $("#dataTable").DataTable({
        ajax: {
            url: '/api/Agency/AllServices',
            dataSrc: '',
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
                } else {
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
        order: [[10, 'desc'], [11, 'desc']],
        columnDefs: [
            { className: 'max-width-column-number', targets: 1 },
            { className: 'max-width-column-number', targets: 2 },
            { className: 'max-width-column-picodes', targets: 4 },
            { className: 'max-width-column-number', targets: 7 },
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
            { data: "caseType", mRender: (data, type, row) => `<span title="${row.caseType}" data-bs-toggle="tooltip">${data}</span>` },
            { data: "serviceType", mRender: (data, type, row) => `<span title="${row.serviceType}" data-bs-toggle="tooltip">${data}</span>` },
            { data: "rate", mRender: (data, type, row) => `<span title="${row.rate}" data-bs-toggle="tooltip">${data}</span>` },
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
            { data: "stateCode", mRender: (data, type, row) => `<span title="${row.state}" data-bs-toggle="tooltip">${data}</span>` },
            { data: "countryCode", mRender: (data, type, row) => `<span title="${row.country}" data-bs-toggle="tooltip"> <img alt="${data}" title="${data}" src="${row.flag}" class="flag-icon" />(${data})</span>` },
            { data: "updatedBy", mRender: (data, type, row) => `<span title="${row.updatedBy}" data-bs-toggle="tooltip">${data}</span>` },
            { data: "updated", mRender: (data, type, row) => `<span title="${row.updated}" data-bs-toggle="tooltip">${data}</span>` },
            {
                sDefaultContent: "",
                bSortable: false,
                mRender: function (data, type, row) {
                    var buttons = "";
                    buttons += `<a data-id="${row.id}" class="btn btn-xs btn-warning"><i class="fas fa-edit"></i> Edit</a>`;
                    buttons += `
                        <button
                           class="btn btn-xs btn-danger js-delete"
                           data-id="${row.id}">
                           <i class="fa fa-trash"></i> Delete
                        </button>`;

                    return buttons;
                }
            },
            { data: "isUpdated", bVisible: false },
            { data: "lastModified", bVisible: false }
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
                            url: '/AgencyService/Delete',
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
    // Highlight rows based on `isUpdated` flag
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

    // Initialize tooltips after each DataTable draw
    $('#dataTable').on('draw.dt', function () {
        $('[data-toggle="tooltip"]').tooltip({
            animated: 'fade',
            placement: 'bottom',
            html: true
        });
    });
});