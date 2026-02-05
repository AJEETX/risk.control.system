$(document).ready(function () {
    $('#Name').focus();

    $("#Name").on("input", function () {
        this.value = this.value.toUpperCase();
    });
    $("#Code").on("input", function () {
        this.value = this.value.toUpperCase();
    });
    $("#Code").on("blur", function () {
        var code = $(this).val().trim().toUpperCase();
        if (!code) return;

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
                        title: '<i class="fas fa-exclamation-triangle text-danger"></i> Duplicate Code!',
                        content: 'The Reason Code <b>' + code + '</b> already exists. Please choose another.',
                        type: 'red',
                    });
                    $("#Code").val("").focus();
                }
            },
            error: function () {
                $.alert({
                    title: 'Error',
                    content: 'Reason Code already exist.',
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
            }
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
            { data: 'updated' },
            { data: 'updateBy' },
            {
                data: 'caseEnablerId',
                render: function (data) {
                    return `
                        <a id="edit${data}" class="btn btn-xs btn-warning" href="/CaseEnabler/Edit/${data}">
                            <i class="fas fa-puzzle-piece"></i> Edit
                        </a>
                        <button type="button" class="btn btn-xs btn-danger delete-item" data-id="${data}">
                            <i class="fas fa-trash"></i> Delete
                        </button>`;
                }
            }
        ],
        "drawCallback": function (setting) {
            $('#dataTable tbody').on('click', '.btn-warning', function (e) {
                e.preventDefault(); // Prevent the default anchor behavior
                var id = $(this).attr('id').replace('edit', ''); // Extract the ID from the button's ID attribute
                showedit(id); // Call the getdetails function with the ID
                window.location.href = $(this).attr('href'); // Navigate to the delete page
            });
        }
    });
    var askConfirmation = true;
    $('#create-form').submit(function (e) {
        if (askConfirmation) {
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
        }
    });

    var askEditConfirmation = true;
    $('#edit-form').submit(function (e) {
        if (askEditConfirmation) {
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