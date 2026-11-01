$(document).ready(function () {
    var table = $('#reportTemplatesTable').DataTable({
        processing: true,
        serverSide: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        ajax: {
            url: '/ReportTemplate/GetReportTemplates',
            type: 'POST',
            data: function (d) {
                d.__RequestVerificationToken= $('input[name="__RequestVerificationToken"]').val(),
                d.insuranceType = $('#caseTypeFilter').val(); // <--- sends filter value
            },
            error: DataTableErrorHandler
        },
        columnDefs: [
            { targets: 1, width: '25%' } // 1 = 'name' column
        ],
        columns: [
            { data: 'id', "bVisible": false },
            {
                data: 'name',
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                data: 'insuranceType',
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                data: 'serviceType',
                "mRender": function (data, type, row) {
                    return '<span title="' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                data: 'isActive',
                render: function (data) {
                    return data
                        ? '<span class="badge bg-success"><i class="fas fa-check-circle"></i>  Active</span>'
                        : '<span class="badge bg-secondary"><i class="fas fa-times-circle"></i> Inactive</span>';
                }
            },
            {
                data: 'createdOn',
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
                    return `<span title="${localDate}" data-bs-toggle="tooltip"><small><strong>${localDate}</strong></small></span>`;
                }
            },
            {
                data: 'locations',
                "mRender": function (data, type, row) {
                    return '<span title="Number of locations: ' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                data: 'faceCount',
                "mRender": function (data, type, row) {
                    return '<span title="Number of face-capture(s): ' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                data: 'docCount',
                "mRender": function (data, type, row) {
                    return '<span title="Number of document capture(s): ' + row.policyId + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                data: 'mediaCount',
                "mRender": function (data, type, row) {
                    return '<span title="Number of media capture(s): : ' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                data: 'questionCount',
                "mRender": function (data, type, row) {
                    return '<span title="Number of question(s): ' + data + '" data-bs-toggle="tooltip">' + data + '</span>';
                }
            },
            {
                data: null,
                orderable: false,
                searchable: false,
                render: function (data, type, row) {
                    // Button HTML setup
                    let activateBtn = row.isActive
                        ? `<button class="btn btn-xs btn-outline-success" disabled><i class="fas fa-flash"></i> Active</button>`
                        : `<button class="btn btn-xs btn-success activate-btn" data-id="${row.id}" data-insurancetype="${row.insuranceType}"><i class="fas fa-flash"></i> Activate</button>`;

                    let editBtn = `<button class="btn btn-xs btn-warning edit-template" data-id="${row.id}"><i class="fas fa-edit"></i> Edit</button>`;
                    let cloneBtn = `<button class="btn btn-xs btn-secondary  clone-templare" data-id="${row.id}"><i class="fas fa-copy"></i> Clone</button>`;
                    let deleteBtn = row.isActive ?
                        `<button class="btn btn-xs btn-danger" disabled><i class="fa fa-trash"></i> Delete</button>` : `<button class="btn btn-xs btn-danger delete-template" data-id="${row.id}"><i class="fas fa-trash"></i> Delete</button>`;

                    return `${activateBtn} ${cloneBtn} ${editBtn} ${deleteBtn}`;
                }
            }
        ],
        "drawCallback": function (settings) {
            // Reinitialize Bootstrap 5 tooltips
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            tooltipTriggerList.map(function (el) {
                return new bootstrap.Tooltip(el, {
                    html: true,
                    sanitize: false   // ⬅⬅⬅ THIS IS THE FIX
                });
            });
        },
        order: []
    });

    $('#caseTypeFilter').on('change', function () {
        table.ajax.reload(); // Reload the table when the filter is changed
    });
    $('#refreshTable').click(function () {
        var $icon = $('#refreshIcon');
        if ($icon) {
            $icon.addClass('fa-spin');
        }
        table.ajax.reload(null, false); // false => Retains current page
    });

    table.on('xhr.dt', function () {
        $('#refreshIcon').removeClass('fa-spin');
    });

    //Activate the report
    $(document).on('click', '.activate-btn', function (e) {
        e.preventDefault();
        var id = $(this).data('id');
        var $btn = $(this);

        if (!id) {
            $.alert({
                title: "Error",
                content: "Missing templateId ID.",
                type: "red"
            });
            return;
        }
        var $spinner = $(".submit-progress"); // global spinner (you already have this)

        $.confirm({
            title: 'Confirm Activation',
            icon: 'fas fa-flash',
            content: 'Are you sure you want to activate this report?',
            type: 'green',
            buttons: {
                confirm: {
                    text: 'Yes, Activate',
                    btnClass: 'btn-green',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Activate');
                        $.ajax({
                            url: '/ReportTemplate/Activate',
                            type: 'POST',
                            data: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                id: id
                            },
                            success: function (response) {
                                if (response.success) {
                                    $.alert({
                                        title: '<span class="i-green"> <i class="fas fas fa-flash"></i> </span> Activated!',
                                        content: response.message,
                                        type: 'green',
                                        buttons: {
                                            OK: {
                                                btnClass: 'btn-green',
                                                icon: 'fa-flash',
                                                action: function () {
                                                    $('#reportTemplatesTable').DataTable().ajax.reload();
                                                }
                                            }
                                        }
                                    });
                                } else {
                                    $.alert({
                                        title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                        content: response.message,
                                        type: 'red'
                                    });
                                }
                            },
                            error: function () {
                                $.alert({
                                    title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                    content: 'Something went wrong while activating the report.',
                                    type: 'red'
                                });
                            },
                            complete: function () {
                                // ✅ Re-enable button and restore text
                                $spinner.addClass("hidden");
                                $btn.prop("disabled", false).html('<i class="fas fa-flash"></i> Activate');
                            }
                        });
                    }
                },
                cancel: {
                    text: 'Cancel',
                    btnClass: 'btn-default'
                }
            }
        });
    });

    //edit template
    $(document).on('click', '.edit-template', function (e) {
        var id = $(this).data('id');
        $("body").addClass("submit-progress-bg");
        setTimeout(function () {
            $(".submit-progress").removeClass("hidden");
        }, 1);
        disableAllInteractiveElements();
        var $btn = $(this);
        $btn.prop('disabled', true);         // disable button
        $btn.addClass('disabled');           // add visual Bootstrap disabled style
        $btn.html('<i class="fas fa-sync fa-spin"></i> Edit'); // show spinner feedback
        location.href = "/ReportTemplate/Details/" + id;
    });

    var hasClone = true;
    $(document).on('click', '.clone-templare', function (e) {
        e.preventDefault();
        var id = $(this).data('id');
        var url = $(this).attr("href");
        if (hasClone) {
            var $btn = $(this);
            var $spinner = $(".submit-progress"); // global spinner (you already have this)

            $.confirm({
                title: 'Confirm Clone',
                content: 'Do you want to clone this template?',
                icon: 'fas fa-copy',
                type: 'dark',
                typeAnimated: true,
                buttons: {
                    confirm: {
                        text: 'Yes, Clone',
                        btnClass: 'btn-dark',
                        action: function () {
                            $spinner.removeClass("hidden");
                            $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Clone');
                            $.ajax({
                                url: '/ReportTemplate/CloneDetails',
                                type: 'POST',
                                data: {
                                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                    templateId: id
                                },
                                success: function (response) {
                                    if (response.success) {
                                        $.alert({
                                            title: '<span class="i-gray"> <i class="fas fa-copy"></i> </span> Cloned!',
                                            content: response.message,
                                            type: 'dark',
                                            buttons: {
                                                OK: {
                                                    btnClass: 'btn-dark',
                                                    icon: 'fa-copy',
                                                    action: function () {
                                                        $('#reportTemplatesTable').DataTable().ajax.reload();
                                                    }
                                                }
                                            }
                                        });
                                    } else {
                                        $.alert({
                                            title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                            content: response.message,
                                            type: 'red'
                                        });
                                    }
                                },
                                error: function () {
                                    $.alert({
                                        title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                        content: 'Something went wrong while cloning the report.',
                                        type: 'red'
                                    });
                                },
                                complete: function () {
                                    // ✅ Re-enable button and restore text
                                    $spinner.addClass("hidden");
                                    $btn.prop("disabled", false).html('<i class="fas fa-copy"></i> Clone');
                                }
                            });
                        }
                    },
                    cancel: {
                        text: 'Cancel',
                        btnClass: 'btn-default'
                    }
                }
            });
        }
    });

    //delete template
    $(document).on('click', '.delete-template', function () {
        var id = $(this).data("id");
        var row = $(this).closest("tr");
        var $btn = $(this);

        if (!id) {
            $.alert({
                title: "Error",
                content: "Missing templateId ID.",
                type: "red"
            });
            return;
        }
        var $spinner = $(".submit-progress"); // global spinner (you already have this)
        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete this template?',
            type: 'red',
            buttons: {
                confirm: {
                    text: 'Yes, Delete',
                    btnClass: 'btn-red',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Delete');
                        $.ajax({
                            url: '/ReportTemplate/DeleteTemplate',
                            type: 'POST',
                            data: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                id: id
                            },
                            success: function (response) {
                                if (response.success) {
                                    $.alert({
                                        title: '<span class="i-orangered"> <i class="fas fa-trash"></i> </span> Deleted!',
                                        content: response.message,
                                        type: 'red',
                                        buttons: {
                                            OK: {
                                                btnClass: 'btn-red',
                                            }
                                        }
                                    });
                                    row.fadeOut(500, function () {
                                        $(this).remove();
                                    });
                                } else {
                                    $.alert({
                                        title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                        content: response.message,
                                        type: 'red'
                                    });
                                }
                            },
                            error: function () {
                                $.alert({
                                    title: '<span class="i-orangered"> <i class="fas fa-exclamation-triangle"></i> </span> Error!',
                                    content: 'Something went wrong. Please try again.',
                                    type: 'red'
                                });
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
                    btnClass: 'btn-default'
                }
            }
        });
    });
});