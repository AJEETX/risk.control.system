$(document).ready(function () {
    $('#Name').focus();

    $("#Code").on("input", function () {
        this.value = this.value.toUpperCase();
    });
    var isChecking = false; // Flag to prevent recursion

    $("#Code").on("blur", function () {
        var code = $(this).val().trim();
        if (!code || isChecking) {
            $("#Code").removeClass('is-invalid').addClass('is-valid');
            return;
        }

        isChecking = true; // Block further triggers

        var id = $('input[name="CaseEnablerId"]').val() || null;

        $.ajax({
            url: '/CaseEnabler/CheckDuplicateCode',
            type: 'POST',
            data: {
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                code: code,
                id: id
            },
            success: function (exists) {
                if (exists) {
                    $.alert({
                        title: '<i class="fas fa-exclamation-triangle text-danger"></i> Duplicate Reason Code!',
                        content: 'The Reason Code <b>' + code + '</b> already exists. Please choose another.',
                        type: 'red',
                        buttons: {
                            ok: {
                                action: function () {
                                    $("#Code").val("").focus();
                                    $("#Code").addClass('is-invalid').removeClass('is-valid');
                                }
                            }
                        },
                        // 2. THIS IS THE KEY: Wait for the modal to fully close
                        onClose: function () {
                            setTimeout(function () {
                                $("#Code").focus();
                            }, 400); // 100ms delay ensures the modal is gone
                            $("#Code").addClass('is-invalid').removeClass('is-valid');
                        }
                    });
                } else {
                    $("#Code").removeClass('is-invalid').addClass('is-valid');
                }
            },
            error: function () {
                isChecking = false;
                $.alert({
                    title: 'Error',
                    content: 'Error occurred checking if Reason Code already exist.',
                    type: 'red',
                });
            }
        });
    });
    $('#dataTable').DataTable({
        ajax: {
            url: '/CaseEnabler/GetCaseEnablers',
            type: 'GET',
            datatype: 'json',
            error: DataTableErrorHandler
        },
        responsive: true,
        fixedHeader: true,
        processing: true,
        paging: true,
        language: {
            loadingRecords: '&nbsp;',
            processing: '<i class="fas fa-sync fa-spin fa-4x fa-fw"></i><span class="sr-only">Loading...</span>'
        },
        columns: [
            { data: 'name' },
            { data: 'code' },
            { data: 'updateBy' },
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
                    return `<span title="Updated time: ${localDate}" data-bs-toggle="tooltip"><small><strong>${localDate}</strong></small></span>`;
                }
            },
            {
                data: 'caseEnablerId',
                bSortable: false,
                render: function (data) {
                    return `
                        <a data-id="${data}" class="btn btn-xs btn-warning">
                            <i class="fas fa-puzzle-piece"></i> Edit
                        </a>
                        <button type="button" class="btn btn-xs btn-danger delete-item" data-id="${data}">
                            <i class="fa fa-trash"></i> Delete
                        </button>`;
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
    $('body').on('click', 'a.btn-xs.btn-warning', function (e) {
        e.preventDefault();
        const id = $(this).data('id');
        showdetail(id, this);
    });
    function showdetail(id, element) {
        id = String(id).replace(/[^a-zA-Z0-9_-]/g, "");
        $("body").addClass("submit-progress-bg");
        setTimeout(() => $(".submit-progress").removeClass("hidden"), 1);

        showSpinnerOnButton(element, "Edit");

        const url = `/CaseEnabler/Edit/${encodeURIComponent(id)}`;

        setTimeout(() => {
            window.location.href = url;
        }, 1000);
    }
    function showSpinnerOnButton(selector, spinnerText) {
        $(selector).html(`<i class='fas fa-sync fa-spin'></i> ${spinnerText}`);
    }
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
            if ($('#create-form').valid()) {
                e.preventDefault();
                $.confirm({
                    title: "Confirm Add New",

                    content: "Are you sure to add?",
                    icon: 'fas fa-puzzle-piece',
                    type: 'green',
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: "Add New",
                            btnClass: 'btn-success',
                            action: function () {
                                askConfirmation = false;
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
                                $('button#create').html("<i class='fas fa-sync fa-spin'></i> Add New");

                                $('#create-form').submit();
                                var form = document.getElementById("create-form");
                                if (form) {
                                    const formElements = form.getElementsByTagName("*");
                                    for (const element of formElements) {
                                        element.disabled = true;
                                        if (element.hasAttribute("readonly")) {
                                            element.classList.remove("valid", "is-valid", "valid-border");
                                            element.removeAttribute("aria-invalid");
                                        }
                                    }
                                }
                            }
                        },
                        cancel: {
                            text: "Cancel",
                            btnClass: 'btn-default'
                        }
                    }
                });
            } else {
                $.alert({
                    title: 'Incomplete detail',
                    content: 'Complete the required fields',
                    type: 'red'
                })
            }
        }
    });

    var askEditConfirmation = true;
    $('#edit-form').submit(function (e) {
        if (askEditConfirmation) {
            if ($('#edit-form').valid()) {
                e.preventDefault();
                $.confirm({
                    title: "Confirm Edit",
                    content: "Are you sure to edit?",

                    icon: 'fas fa-puzzle-piece',
                    type: 'orange',
                    closeIcon: true,
                    buttons: {
                        confirm: {
                            text: "Edit",
                            btnClass: 'btn-warning ',
                            action: function () {
                                askEditConfirmation = false;
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
                                $('button#edit').html("<i class='fas fa-sync fa-spin'></i> Edit");

                                $('#edit-form').submit();
                                var form = document.getElementById("edit-form");
                                if (form) {
                                    const formElements = form.getElementsByTagName("*");
                                    for (const element of formElements) {
                                        element.disabled = true;
                                        if (element.hasAttribute("readonly")) {
                                            element.classList.remove("valid", "is-valid", "valid-border");
                                            element.removeAttribute("aria-invalid");
                                        }
                                    }
                                }
                            }
                        },
                        cancel: {
                            text: "Cancel",
                            btnClass: 'btn-default'
                        }
                    }
                });
            } else {
                $.alert({
                    title: 'Incomplete detail',
                    content: 'Complete the required fields',
                    type: 'red'
                })
            }
        }
    })

    $(document).on("click", ".delete-item", function () {
        var $btn = $(this);
        var $spinner = $(".submit-progress"); // global spinner (you already have this)
        var id = $(this).data("id");
        var row = $(this).closest("tr");
        var table = $('#dataTable').DataTable();

        $.confirm({
            title: 'Confirm Deletion',
            content: 'Are you sure you want to delete ?',
            type: 'red',
            buttons: {
                confirm: {
                    text: 'Yes, Delete',
                    btnClass: 'btn-red',
                    action: function () {
                        $spinner.removeClass("hidden");
                        $btn.prop("disabled", true).html('<i class="fas fa-sync fa-spin"></i> Delete');
                        $.ajax({
                            url: '/CaseEnabler/Delete',
                            type: 'POST',
                            data: {
                                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val(),
                                id: id
                            },
                            success: function (response) {
                                if (response.success) {
                                    table.row(row).remove().draw(false); // correct DataTable removal
                                    $.alert({
                                        title: 'Deleted',
                                        content: response.message,
                                        type: 'red'
                                    });
                                } else {
                                    $.alert(response.message);
                                }
                            },
                            error: function (e) {
                                $.alert('Error while deleting.');
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

    $('a.create').on('click', function () {
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

        $('a.create').html("<i class='fas fa-sync fa-spin'></i> Add New");

        var article = document.getElementById("article");
        if (article) {
            var nodes = article.getElementsByTagName('*');
            for (var i = 0; i < nodes.length; i++) {
                nodes[i].disabled = true;
            }
        }
    });
});
function showedit(id) {
    $("body").addClass("submit-progress-bg");
    // Wrap in setTimeout so the UI
    // can update the spinners
    setTimeout(function () {
        $(".submit-progress").removeClass("hidden");
    }, 1);

    $('a#edit' + id + '.btn.btn-xs.btn-warning').html("<i class='fas fa-sync fa-spin'></i> Edit");

    disableAllInteractiveElements();

    var article = document.getElementById("article");
    if (article) {
        var nodes = article.getElementsByTagName('*');
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].disabled = true;
        }
    }
}